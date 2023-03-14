#!/bin/bash
# Test building the C++ client using autotools and CMake
set -e
set -x
set -o functrace

scriptroot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $scriptroot/../..

root=`pwd`
out=$root/bazel-bin/client/cpp/test-build
version=`tools/krpc-version.sh`

# Build library and dependencies
bazel build \
  //client/cpp \
  @protoc_linux_x86_64//:bin/protoc \
  @com_google_protobuf//:protobuf_lite \
  @cpp_asio//:asio
protobuf_include=$root/bazel-krpc/external/com_google_protobuf/src
protobuf_library=$root/bazel-bin/external/com_google_protobuf
asio_include=$root/bazel-krpc/external/cpp_asio/include
protoc_dir=$root/bazel-krpc/external/protoc_linux_x86_64/bin

# Extract source
rm -rf $out
mkdir -p $out
unzip -q bazel-bin/client/cpp/krpc-cpp-$version.zip -d $out
mv $out/krpc-cpp-$version/* $out/
rm -r $out/krpc-cpp-$version
pushd $out

function autotools_build {
  # Build the library using autotools with given build and install directories
  mkdir -p $out/$1
  pushd $out/$1
  $out/configure --prefix=$out/$2
  make -j`nproc`
  make install -j`nproc`
  popd
}

function cmake_build {
  # Build the library using cmake with given build and install directories
  mkdir -p $out/$1
  pushd $out/$1
  cmake $out \
    -DCMAKE_INSTALL_PREFIX=$out/$2 \
    -DPROTOBUF_LIBRARY=$protobuf_library/libprotobuf.so \
    -DPROTOBUF_INCLUDE_DIR=$protobuf_include \
    -DPROTOBUF_PROTOC_EXECUTABLE=$protoc \
    -DCMAKE_INCLUDE_PATH=$asio_include
  make -j`nproc`
  make install -j`nproc`
  popd
}

function test_build {
  # Compile a test application using the given install path for headers and libraries
  echo "#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(&conn);
  std::cout << krpc.get_status().version() << std::endl;
}" > $out/main.cpp
  # Note: libprotobuf is NOT built to contains libprotobuf_lite by the bazel build files, so both must be included
  g++ \
    -Wall -Werror -std=c++11 \
    -I$out/$1/include \
    -L$out/$1/lib \
    -L$protobuf_library \
    -o $out/main.exe $out/main.cpp \
    -lkrpc -lprotobuf -lprotobuf_lite -lz
}

export LDFLAGS=-L$protobuf_library
export CPPFLAGS="-I$protobuf_include -I$asio_include"
export CPLUS_INCLUDE_PATH="$protobuf_include:$asio_include"
export protobuf_CFLAGS="-I$protobuf_include"
export protobuf_LIBS=$protobuf_library

protoc=

# Run tests with libprotobuf, but without protoc
autotools_build autotools/build autotools/install
test_build autotools/install
cmake_build cmake/build cmake/install
test_build cmake/install

protoc=$protoc_dir/protoc
export PATH=$PATH:$protoc_dir

# Run tests with libprotobuf and protoc
autotools_build autotools-protoc/build autotools-protoc/install
test_build autotools-protoc/install
cmake_build cmake-protoc/build cmake-protoc/install
test_build cmake-protoc/install

popd
