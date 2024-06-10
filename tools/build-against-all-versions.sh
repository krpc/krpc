#!/bin/bash

set -ev

bazel build //:LICENSE  # Build something to create symlinks
mkdir -p bazel-bin/lib
pushd bazel-bin/lib

read -d '' versions << EOM || true
1.12.5
1.12.4
1.12.3
EOM

# Save lib/ksp symlink
popd
[[ -e lib/ksp ]] && mv lib/ksp lib/ksp-orig
pushd bazel-bin/lib

for version in $versions; do
    # Download and extract ksp libs
    if [ ! -f ksp-$version.zip ]; then
        wget -O ksp-$version.zip https://github.com/krpc/ksp-lib/raw/main/ksp/ksp-$version.zip
    fi
    if [ ! -d ksp-$version ]; then
        mkdir ksp-$version
        pushd ksp-$version
        unzip ../ksp-$version.zip
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
[[ -e lib/ksp-orig ]] && mv lib/ksp-orig lib/ksp
