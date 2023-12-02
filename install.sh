#!/bin/bash

# run with bash install.sh (not sh install.sh)

set -o errexit

# Create the destination directory if it doesn't exist
sudo mkdir -p /usr/local/bin/dlsync
# and copy the standalone binarty to it
sudo cp ./publish/standalone/linux-x64/DlMirrorSync /usr/local/bin/dlsync/
# don't overwrite the settings file if present
sudo cp -n ./publish/standalone/linux-x64/appsettings.json /usr/local/bin/dlsync/

# Get the directory of the current script
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

# Get the username of the logged in user
# the service will run as the installing user
# pass the username as the first argument to the script
# default to logged in user if not supplied
USERNAME=${1:-$(logname)}

# Replace all instances of 'USERNAME' with the runas user's name
# in 'dlsync.service' and save as '/etc/systemd/system/dlsync.service'
sudo sed "s/USERNAME/$USERNAME/g" "$DIR/dlsync.service" > "/etc/systemd/system/dlsync.service"

# start the service and set to start on boot
sudo systemctl start dlsync
sudo systemctl enable dlsync
sudo systemctl status dlsync