#!/bin/bash
set -ev

if [[ $TRAVIS_PULL_REQUEST != "false" ]]; then
    NAME="pr/"$TRAVIS_PULL_REQUEST
else
    NAME=$TRAVIS_BRANCH
fi

JOB_NUMBER=$(echo $TRAVIS_JOB_NUMBER | cut -d. -f1)
DEPLOYPATH=s3-deploy/deploy/$NAME/$JOB_NUMBER
VERSION=`tools/krpc-version.sh`
echo $VERSION

ASIO_VERSION=1.12.1
PROTOBUF_VERSION=3.9.1

# ASIO
rm -rf asio
mkdir asio
pushd asio
# Download and extract source
wget https://krpc.s3.amazonaws.com/lib/asio/asio-${ASIO_VERSION}.tar.gz
tar -xf asio-${ASIO_VERSION}.tar.gz --exclude doc
rm asio-${ASIO_VERSION}.tar.gz
mv asio-${ASIO_VERSION} src
# Copy header files
mkdir install
cp -r src/include install/include
# Clean up
rm -rf src
popd

# ProtoBuf
PROTOBUF_URL=https://krpc.s3.amazonaws.com/lib/protobuf/protobuf-cpp-${PROTOBUF_VERSION}-win64.tar.gz
if curl --output /dev/null --silent --head --fail "$PROTOBUF_URL"; then
  # From pre-built binaries cached on s3
  wget "$PROTOBUF_URL"
  tar -xf protobuf-cpp-${PROTOBUF_VERSION}-win64.tar.gz
else
  # From source, and cache the result on s3 for next time
  rm -rf protobuf
  mkdir protobuf
  pushd protobuf
  # Download and extract source
  wget https://krpc.s3.amazonaws.com/lib/protobuf/protobuf-cpp-${PROTOBUF_VERSION}.tar.gz
  tar -xf protobuf-cpp-${PROTOBUF_VERSION}.tar.gz
  rm protobuf-cpp-${PROTOBUF_VERSION}.tar.gz
  mv protobuf-${PROTOBUF_VERSION} src
  # Build and install
  mkdir build
  pushd build
  cmake ../src/cmake \
    -G "Visual Studio 15 2017 Win64" \
    -DCMAKE_INSTALL_PREFIX=../install \
    -Dprotobuf_BUILD_TESTS=OFF
  cmake --build . --config Release
  cmake --build . --target install --config Release
  popd
  popd
  # Remove build and source files
  rm -rf protobuf/build protobuf/src
  # Create release archive
  tar -cf protobuf-cpp-${PROTOBUF_VERSION}-win64.tar.gz protobuf
  mkdir -p s3-deploy/lib/protobuf
  mv protobuf-cpp-${PROTOBUF_VERSION}-win64.tar.gz s3-deploy/lib/protobuf/
fi

# kRPC C++ client
rm -rf krpc
mkdir krpc
pushd krpc
# Download and extract source (from previous build step)
wget https://krpc.s3.amazonaws.com/deploy/$NAME/$JOB_NUMBER/client/krpc-cpp-$VERSION.zip
unzip krpc-cpp-$VERSION.zip
mv krpc-cpp-$VERSION src
rm krpc-cpp-$VERSION.zip
# Build and install
mkdir build
pushd build
cmake ../src \
  -G "Visual Studio 15 2017 Win64" \
  -DCMAKE_INSTALL_PREFIX=../install \
  -DPROTOBUF_LIBRARY=../../protobuf/install/lib/libprotobuf \
  -DPROTOBUF_INCLUDE_DIR=../../protobuf/install/include \
  -DPROTOBUF_PROTOC_EXECUTABLE=../../protobuf/install/bin/protoc.exe \
  -DCMAKE_INCLUDE_PATH=../../asio/install/include
cmake --build . --config Release
cmake --build . --target install --config Release
popd
popd
# Remove build and source files
rm -r krpc/build krpc/src
# Create release archive
cp -r protobuf/install/lib/libprotobuf.lib krpc/install/lib/
cp -r protobuf/install/include/* krpc/install/include/
(cd krpc/install; 7z a -r krpc-cpp-$VERSION-win64.zip ./)
# Prepare release to be deployed
mkdir -p $DEPLOYPATH/client
cp krpc/install/krpc-cpp-$VERSION-win64.zip $DEPLOYPATH/client/
