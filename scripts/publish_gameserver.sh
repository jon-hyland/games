#!/bin/bash

# ensure run as root
if [ "$EUID" -ne 0 ]
then
    echo "Must be run as root"
    exit
fi

# break on error
set -e

# clone (or pull) 'games' repo
if [ ! -d "/home/pi/git/games" ]
then
    echo "Cloning 'games' repository.."
    sudo -u pi git clone "https://github.com/jon-hyland/games.git" "/home/pi/git/games/"
else
    echo "Pulling 'games' repository.."
    sudo -u pi git -C "/home/pi/git/games" pull
fi

# stop 'gameserver' service
if systemctl list-units | grep -Fq 'gameserver.service'
then
    echo "Stopping Game Server.."
    systemctl stop gameserver.service
fi

# build and publish 'gameserver' project
echo "Building and publishing Game Server.."
dotnet publish --output /usr/share/gameserver/ /home/pi/git/games/csharp/GameServer/GameServer.csproj

# create symbolic links
echo "Creating symbolic links.."
sudo -u pi ln -sf /usr/share/gameserver /home/pi/gameserver
sudo -u pi ln -sf /usr/share/gameserver/LogFile.txt /home/pi/log_file.txt

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
