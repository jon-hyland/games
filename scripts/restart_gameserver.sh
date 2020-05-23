#!/bin/bash

if [ "$EUID" -ne 0 ]
  then echo "Must be run as root"
  exit
fi

set -e

systemctl restart gameserver.service
