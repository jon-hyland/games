#!/bin/bash

if [ "$EUID" -ne 0 ]
then
    echo "Must be run as root"
    exit
fi

set -e


if [ $(dpkg-query -W -f='${Status}' git 2>/dev/null | grep -c "ok installed") -eq 0 ]
then
    echo "Installing Git.."
    apt-get install git
fi

sudo -u pi git config --global credential.helper "cache --timeout=3600"
sudo -u pi git config --global user.name "John Hyland"
sudo -u pi git config --global user.email "jonhyland@hotmail.com"
sudo -u pi mkdir -p /home/pi/git/

if [ ! -d "/home/pi/git/games" ]
then
    echo "Cloning repository.."
    sudo -u pi git clone "https://github.com/jon-hyland/games.git" "/home/pi/git/games/"
else
    echo "Pulling repository.."
    sudo -u pi git -C "/home/pi/git/games" pull
fi

if systemctl list-units | grep -Fq 'gameserver.service'
then
    echo "Stopping Game Server.."
    systemctl stop gameserver.service
fi

echo "Building Game Server.."
dotnet publish --output /usr/share/gameserver/ /home/pi/git/games/csharp/GameServer/GameServer.csproj

echo "Creating symbolic links.."
sudo -u pi ln -sf /usr/share/gameserver /home/pi/gameserver
sudo -u pi ln -sf /usr/share/gameserver/LogFile.txt /home/pi/log_file.txt

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

echo "Refreshing services.."
systemctl daemon-reload

echo "Starting Game Server.."
systemctl start gameserver.service

echo "Publish complete"
