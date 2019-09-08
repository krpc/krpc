#!/bin/bash

set -e

bazel build //:LICENSE  # Build something to create symlinks
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
1.6.1.2401
1.7.0.2483
1.7.1.2539
1.7.2.2556
1.7.3.2594
EOM

# Save lib/ksp symlink
popd
mv lib/ksp lib/krpc-orig
pushd bazel-bin/lib

for version in $versions; do
    # Download and extract ksp libs
    if [ ! -f ksp-$version.tar.gz ]; then
        wget -O ksp-$version.tar.gz https://krpc.s3.amazonaws.com/lib/ksp-$version.tar.gz
    fi
    if [ ! -d ksp-$version ]; then
        mkdir ksp-$version
        pushd ksp-$version
        tar -xf ../ksp-$version.tar.gz
        popd
    fi

    # Build plugin
    popd
    rm -f lib/ksp
    ln -s -r bazel-bin/lib/ksp-$version lib/ksp
    bazel build //:krpc
    pushd bazel-bin/lib
done

# Restore lib/ksp symlink
popd
mv lib/krpc-orig lib/ksp
pushd bazel-bin/lib

popd
