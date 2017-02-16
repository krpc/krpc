#!/bin/bash
set -ev
VERSION=3.2.0
wget https://github.com/google/protobuf/releases/download/v$VERSION/protobuf-cpp-$VERSION.tar.gz
mkdir -p protobuf-cpp-$VERSION
(cd protobuf-cpp-$VERSION; tar -xf ../protobuf-cpp-$VERSION.tar.gz --strip=1)
(cd protobuf-cpp-$VERSION; ./configure && make -j`nproc` && sudo make install)
sudo ldconfig
rm -rf protobuf-cpp-$VERSION protobuf-cpp-$VERSION.tar.gz
