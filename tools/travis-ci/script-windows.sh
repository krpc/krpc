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

asio() {
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
}

# ProtoBuf
protobuf() {
  PLATFORM=$1
  if [ "${PLATFORM}" == "win32" ]; then
    CMAKE_PLATFORM="Visual Studio 15 2017"
  else
    CMAKE_PLATFORM="Visual Studio 15 2017 ${PLATFORM^}"
  fi

  PROTOBUF_ARCHIVE=protobuf-cpp-${PROTOBUF_VERSION}-${PLATFORM}.tar.gz
  PROTOBUF_URL=https://krpc.s3.amazonaws.com/lib/protobuf/${PROTOBUF_ARCHIVE}
  if curl --output /dev/null --silent --head --fail "$PROTOBUF_URL"; then
    # From pre-built binaries cached on s3
    wget "$PROTOBUF_URL"
    tar -xf ${PROTOBUF_ARCHIVE}
  else
    # From source, and cache the result on s3 for next time
    rm -rf protobuf-${PLATFORM}
    mkdir protobuf-${PLATFORM}
    pushd protobuf-${PLATFORM}
    # Download and extract source
    wget https://krpc.s3.amazonaws.com/lib/protobuf/protobuf-cpp-${PROTOBUF_VERSION}.tar.gz
    tar -xf protobuf-cpp-${PROTOBUF_VERSION}.tar.gz
    rm protobuf-cpp-${PROTOBUF_VERSION}.tar.gz
    mv protobuf-${PROTOBUF_VERSION} src
    # Build and install
    mkdir build
    pushd build
    cmake ../src/cmake \
      -G "${CMAKE_PLATFORM}" \
      -DCMAKE_INSTALL_PREFIX=../install \
      -Dprotobuf_BUILD_TESTS=OFF
    cmake --build . --config Release
    cmake --build . --target install --config Release
    popd
    # Remove build and source files
    rm -rf build src
    popd
    # Create release archive
    tar -cf ${PROTOBUF_ARCHIVE} protobuf-${PLATFORM}
    mkdir -p s3-deploy/lib/protobuf
    mv ${PROTOBUF_ARCHIVE} s3-deploy/lib/protobuf/
  fi
}

# kRPC C++ client
krpc() {
  PLATFORM=$1
  if [ "${PLATFORM}" == "win32" ]; then
    CMAKE_PLATFORM="Visual Studio 15 2017"
  else
    CMAKE_PLATFORM="Visual Studio 15 2017 ${PLATFORM^}"
  fi

  rm -rf krpc-${PLATFORM}
  mkdir krpc-${PLATFORM}
  pushd krpc-${PLATFORM}
  # Download and extract source (from previous build step)
  wget https://krpc.s3.amazonaws.com/deploy/$NAME/$JOB_NUMBER/client/krpc-cpp-$VERSION.zip
  unzip krpc-cpp-$VERSION.zip
  mv krpc-cpp-$VERSION src
  rm krpc-cpp-$VERSION.zip
  # Build and install
  mkdir build
  pushd build
  cmake ../src \
    -G "${CMAKE_PLATFORM}" \
    -DCMAKE_INSTALL_PREFIX=../install \
    -DPROTOBUF_LIBRARY=../../protobuf-${PLATFORM}/install/lib/libprotobuf \
    -DPROTOBUF_INCLUDE_DIR=../../protobuf-${PLATFORM}/install/include \
    -DPROTOBUF_PROTOC_EXECUTABLE=../../protobuf-${PLATFORM}/install/bin/protoc.exe \
    -DCMAKE_INCLUDE_PATH=../../asio/install/include
  cmake --build . --config Release
  cmake --build . --target install --config Release
  popd
  # Remove build and source files
  rm -r build src
  popd
  # Create release archive
  cp -r protobuf-${PLATFORM}/install/lib/libprotobuf.lib krpc-${PLATFORM}/install/lib/
  cp -r protobuf-${PLATFORM}/install/include/* krpc-${PLATFORM}/install/include/
  (cd krpc-${PLATFORM}/install; 7z a -r ../../krpc-cpp-${VERSION}-${PLATFORM}.zip ./)
  # Prepare release to be deployed
  mkdir -p ${DEPLOYPATH}/client
  cp krpc-cpp-${VERSION}-${PLATFORM}.zip ${DEPLOYPATH}/client/
}

asio

protobuf win32
protobuf win64

krpc win32
krpc win64
