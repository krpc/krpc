#!/bin/bash
# Test building the C-nano client using autotools and CMake

set -ev

bazel build //client/cnano

out=bazel-bin/client/cnano/test-build
version=`tools/krpc-version.sh`

# Extract
rm -rf $out
mkdir -p $out
unzip -q bazel-bin/client/cnano/krpc-cnano-$version.zip -d $out
mv $out/krpc-cnano-$version/* $out/
rm -r $out/krpc-cnano-$version
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
