#!/bin/bash

set -e

mkdir -p bazel-bin/lib
pushd bazel-bin/lib

read -d '' versions << EOM || true
1.2.2.1622
1.3.0.1804
1.3.1.1891
1.4.0.2077
1.4.1.2089
1.4.2.2110
1.4.3.2152
1.4.4.2215
1.4.5.2243
1.5.0.2332
1.5.1.2335
EOM

for version in $versions; do
    if [ ! -f ksp-$version.tar.gz ]; then
        wget -O ksp-$version.tar.gz https://krpc.s3.amazonaws.com/lib/ksp-$version.tar.gz
    fi
    if [ ! -d ksp-$version ]; then
        mkdir ksp-$version
        pushd ksp-$version
        tar -xf ../ksp-$version.tar.gz
        popd
    fi

    popd
    rm -f lib/ksp
    ln -s -r bazel-bin/lib/ksp-$version lib/ksp
    bazel build //:krpc
    pushd bazel-bin/lib
done

popd
