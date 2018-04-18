#!/bin/bash
# Test building the C++ client using autotools and CMake

set -ev

bazel build //client/cpp

root=`pwd`
out=bazel-bin/client/cpp/test-build
version=`tools/krpc-version.sh`

# Build dependencies
bazel build @com_google_protobuf//:protobuf @com_google_protobuf//:protobuf_lite @cpp_asio//:asio
protobuf_include=$root/bazel-krpc/external/com_google_protobuf/src
protobuf_library=$root/bazel-bin/external/com_google_protobuf
asio_include=$root/bazel-krpc/external/cpp_asio/include

export LDFLAGS=-L$protobuf_library
export CPPFLAGS="-I$protobuf_include -I$asio_include"
export CPLUS_INCLUDE_PATH="$protobuf_include:$asio_include"
export protobuf_CFLAGS="-I$protobuf_include"
export protobuf_LIBS=$protobuf_library

# Extract
rm -rf $out
mkdir -p $out
unzip -q bazel-bin/client/cpp/krpc-cpp-$version.zip -d $out
mv $out/krpc-cpp-$version/* $out/
rm -r $out/krpc-cpp-$version
pushd $out

# Autotools build
./configure --prefix=`pwd`/local-configure
make -j`nproc`
make install -j`nproc`

# CMake build
mkdir build-cmake
cd build-cmake
cmake .. \
  -DCMAKE_INSTALL_PREFIX=`pwd`/../local-cmake \
  -DPROTOBUF_LIBRARY=$protobuf_library/libprotobuf.so \
  -DPROTOBUF_INCLUDE_DIR=$protobuf_include \
  -DCMAKE_INCLUDE_PATH=$asio_include
make -j`nproc`
make install -j`nproc`
cd ..

# Build test app
echo "#include <iostream>
#include <krpc.hpp>
#include <krpc/services/krpc.hpp>
int main() {
  auto conn = krpc::connect();
  krpc::services::KRPC krpc(&conn);
  std::cout << krpc.get_status().version() << std::endl;
}" > main.cpp
mkdir -p test-configure test-cmake
# Note: libprotobuf is NOT built to contains libprotobuf_lite by the bazel build files, so both must be included
args="-L$protobuf_library -Wall -Werror main.cpp -std=c++11 -lkrpc -lprotobuf -lprotobuf_lite"
g++ -Ilocal-configure/include -Llocal-configure/lib $args -o test-configure/main
g++ -Ilocal-cmake/include -Llocal-cmake/lib $args -o test-cmake/main

popd
