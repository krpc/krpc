#!/bin/bash
# Test building the C++ client using autotools and CMake

set -ev

bazel build //client/cpp

out=bazel-bin/client/cpp/test-build
version=`tools/krpc-version.sh`

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
cmake .. -DCMAKE_INSTALL_PREFIX=`pwd`/../local-cmake
make -j`nproc`
make install -j`nproc`

popd
