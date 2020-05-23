#!/bin/bash

if [ "$EUID" -ne 0 ]
  then echo "Must be run as root"
  exit
fi

set -e


echo "Downloading .NET Core 3.1 SDK.."
wget -nv https://download.visualstudio.microsoft.com/download/pr/f2e1cb4a-0c70-49b6-871c-ebdea5ebf09d/acb1ea0c0dbaface9e19796083fe1a6b/dotnet-sdk-3.1.300-linux-arm.tar.gz -O dotnet.tar.gz

echo "Creating folder '/usr/share/dotnet'.."
sudo mkdir -p /usr/share/dotnet

echo "Unpacking binaries.."
sudo tar -zxf dotnet.tar.gz -C /usr/share/dotnet

echo "Creating symbolic link '/usr/bin/dotnet'.."
sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet

echo 'Removing download file..'
rm dotnet.tar.gz

echo "Installation complete"
