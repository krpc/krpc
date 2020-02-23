#!/bin/bash

set -ev

bazel build //:LICENSE  # Build something to create symlinks
mkdir -p bazel-bin/lib
pushd bazel-bin/lib

read -d '' versions << EOM || true
1.8.0.2686
1.8.1.2694
1.9.0.2781
EOM

# Save lib/ksp symlink
popd
rm -f lib/ksp-orig
mv lib/ksp lib/ksp-orig
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
rm -f lib/ksp
mv lib/ksp-orig lib/ksp
