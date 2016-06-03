#!/bin/bash

set -e

GAMEDATA=${KSP_DIR:-lib/ksp}/GameData/kRPC
VERSION=`tools/krpc-version.sh`

bazel build \
    //:ksp-avc-version \
    //server \
    //service/SpaceCenter \
    //service/Drawing \
    //service/InfernalRobotics \
    //service/KerbalAlarmClock \
    //service/RemoteTech \
    //service/UI \
    //tools/TestingTools

rm -f $GAMEDATA/KRPC.dll
rm -rf $GAMEDATA
mkdir -p $GAMEDATA
cp -R -L \
    bazel-genfiles/kRPC.version \
    bazel-bin/server/KRPC.dll \
    bazel-bin/server/KRPC.xml \
    bazel-bin/server/src/icons \
    bazel-bin/service/**/*.dll \
    bazel-bin/service/**/*.xml \
    bazel-krpc/external/csharp_protobuf_net35/file/Google.Protobuf.dll \
    service/**/*.ksp \
    bazel-bin/tools/TestingTools/TestingTools.dll \
    bazel-bin/tools/TestingTools/TestingTools.xml \
    $GAMEDATA/

mkdir -p $GAMEDATA/PluginData
cp tools/settings.cfg $GAMEDATA/PluginData/

find $GAMEDATA -type f -exec chmod 644 {} \;
find $GAMEDATA -type d -exec chmod 755 {} \;

ls -lR $GAMEDATA
