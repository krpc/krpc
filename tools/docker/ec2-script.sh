#!/bin/bash
# Copy this script to a new EC2 instance (running Ubuntu server 18.04), make it executable, and run it as root
# to build a new kRPC build-env docker image.
# This script expects a parameter giving the name of the branch to check out and build from the repository.

set -e

BRANCH=$1

if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root"
   exit 1
fi

if [ $# -eq 0 ]; then
   echo "Branch must be specified"
   exit 1
fi

apt-get update
apt-get upgrade -y
apt-get install -y git make
apt-get install -y apt-transport-https ca-certificates curl software-properties-common
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -
add-apt-repository "deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"
apt-get update
apt-get install -y docker-ce
docker run hello-world

if [ ! -d krpc ]; then
  git clone http://github.com/djungelorm/krpc
fi
cd krpc
git fetch origin
git branch -v -a
git checkout $BRANCH

make -C tools/docker build
docker images

echo "Build complete. Run:"
echo "  sudo docker login"
echo "  sudo make -C krpc/tools/docker deploy"
echo "to deploy the new image to Docker Hub"
