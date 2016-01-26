#!/bin/bash

set -e

KSP=lib/ksp
GAMEDATA=$KSP/GameData/kRPC
VERSION=`tools/get-version.sh`

bazel build //:krpc
bazel build //tools/TestingTools

rm -rf $GAMEDATA
mkdir $GAMEDATA
mkdir $GAMEDATA/archive
CWD=`pwd`
(cd $GAMEDATA/archive; unzip $CWD/bazel-bin/krpc-$VERSION.zip)
cp -r $GAMEDATA/archive/GameData/kRPC/* $GAMEDATA/
cp bazel-bin/tools/TestingTools/TestingTools.dll $GAMEDATA/
rm -r $GAMEDATA/archive
