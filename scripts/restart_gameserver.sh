#!/bin/bash

# ensure run as root
if [ "$EUID" -ne 0 ]
then
    echo "Must be run as root"
    exit
fi

# break on error
set -e

# restart 'gameserver' service
echo "Restarting Game Server.."
systemctl restart gameserver.service
