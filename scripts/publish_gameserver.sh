#!/bin/bash

# ensure run as root
if [ "$EUID" -ne 0 ]
then
    echo "Must be run as root"
    exit
fi

# break on error
set -e

# vars
HOME="/home/pi"
USER="pi"

# clone (or pull) 'games' repo
if [ ! -d "$HOME/git/games" ]
then
    echo "Cloning 'games' repository.."
    sudo -u $USER git clone "https://github.com/jon-hyland/games.git" "$HOME/git/games/"
else
    echo "Pulling 'games' repository.."
    sudo -u $USER git -C "$HOME/git/games" pull
fi

# stop 'gameserver' service
if systemctl list-units | grep -Fq 'gameserver.service'
then
    echo "Stopping Game Server.."
    systemctl stop gameserver.service
fi

# build and publish 'gameserver' project
echo "Building and publishing Game Server.."
dotnet publish --output /usr/share/gameserver/ $HOME/git/games/csharp/GameServer/GameServer.csproj

# copy scripts
echo "Copying scripts.."
sudo -u $USER mkdir -p $HOME/scripts/
sudo -u $USER cp $HOME/git/games/scripts/* $HOME/scripts/

# grant execution on scripts
echo "Granting execution on scripts.."
sudo -u $USER chmod +x $HOME/scripts/*.sh

# copy control scripts
echo "Copying home resources.."
rm -f $HOME/publish_gameserver.sh
sudo -u $USER cp $HOME/scripts/publish_gameserver.sh $HOME/publish_gameserver.sh
rm -f $HOME/restart_gameserver.sh
sudo -u $USER cp $HOME/scripts/restart_gameserver.sh $HOME/restart_gameserver.sh
rm -f $HOME/GAMESERVER_README
sudo -u $USER cp $HOME/scripts/GAMESERVER_README $HOME/GAMESERVER_README

# create symbolic links
echo "Creating symbolic links.."
rm -f $HOME/gameserver
sudo -u $USER ln -sf /usr/share/gameserver $HOME/gameserver
rm -f $HOME/log_file.txt
sudo -u $USER ln -sf /usr/share/gameserver/LogFile.txt $HOME/log_file.txt

# create service file
echo "Creating service file.."
rm -f /etc/systemd/system/gameserver.service
cat <<EOF >/etc/systemd/system/gameserver.service
[Unit]
Description=Game Server
After=network-online.target
Wants=network-online.target

[Service]
ExecStart=/usr/share/gameserver/GameServer
Restart=on-abort

[Install]
WantedBy=multi-user.target
EOF

# refresh services
echo "Refreshing services.."
systemctl daemon-reload

# start 'gameserver' service
echo "Starting Game Server.."
systemctl start gameserver.service

# success
echo "Operation success"
