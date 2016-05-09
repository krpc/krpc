#!/bin/bash

set -e

GAMEDATA=${KSP_DIR:-lib/ksp}/GameData/kRPC
VERSION=`tools/krpc-version.sh`

bazel build \
    //:ksp-avc-version \
    //server \
    //service/SpaceCenter \
    //service/InfernalRobotics \
    //service/KerbalAlarmClock \
    //tools/TestingTools

rm -rf $GAMEDATA
mkdir -p $GAMEDATA
cp -R \
    bazel-genfiles/kRPC.version \
    bazel-bin/server/KRPC.dll \
    bazel-bin/server/KRPC.xml \
    bazel-bin/server/src/icons \
    bazel-bin/service/**/*.dll \
    bazel-bin/service/**/*.xml \
    bazel-krpc/external/csharp_protobuf_net35/Google.Protobuf.dll \
    bazel-bin/tools/TestingTools/TestingTools.dll \
    bazel-bin/tools/TestingTools/TestingTools.xml \
    tools/settings.cfg \
    $GAMEDATA/

find $GAMEDATA -type f -exec chmod 644 {} \;
find $GAMEDATA -type d -exec chmod 755 {} \;

ls -lR $GAMEDATA
