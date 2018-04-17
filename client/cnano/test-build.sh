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
cd ..

# Build test app
echo "#include <stdio.h>
#include <krpc_cnano.h>
#include <krpc_cnano/services/krpc.h>
int main() {
  krpc_connection_t conn;
  krpc_open(&conn, \"COM0\");
  krpc_connect(conn, NULL);
  krpc_schema_Status status;
  krpc_KRPC_GetStatus(conn, &status);
  printf(\"%s\n\", status.version);
  return 0;
}" > main.c
mkdir -p test-configure test-cmake
args="main.c -lkrpc_cnano -Wall -Werror"
pwd
gcc -Ilocal-configure/include -Llocal-configure/lib $args -o test-configure/main
gcc -Ilocal-cmake/include -Llocal-cmake/lib $args -o test-cmake/main

popd
