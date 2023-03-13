#!/bin/bash

set -e

GAMEDATA=${KSP_DIR:-lib/ksp}/GameData/kRPC
VERSION=`tools/krpc-version.sh`

bazel build \
    //:ksp-avc-version \
    //server \
    //core \
    //service/SpaceCenter \
    //service/Drawing \
    //service/InfernalRobotics \
    //service/KerbalAlarmClock \
    //service/RemoteTech \
    //service/UI \
    //service/LiDAR \
    //service/DockingCamera \
    //tools/TestingTools

rm -f $GAMEDATA/KRPC.dll
rm -rf $GAMEDATA
mkdir -p $GAMEDATA
cp -R -L \
    bazel-bin/kRPC.version \
    bazel-bin/core/KRPC.Core.dll \
    bazel-bin/server/KRPC.dll \
    bazel-bin/server/KRPC.xml \
    bazel-bin/server/src/icons \
    bazel-bin/service/SpaceCenter/KRPC.SpaceCenter.dll \
    bazel-bin/service/SpaceCenter/KRPC.SpaceCenter.xml \
    bazel-bin/service/Drawing/KRPC.Drawing.dll \
    bazel-bin/service/Drawing/KRPC.Drawing.xml \
    bazel-bin/service/InfernalRobotics/KRPC.InfernalRobotics.dll \
    bazel-bin/service/InfernalRobotics/KRPC.InfernalRobotics.xml \
    bazel-bin/service/KerbalAlarmClock/KRPC.KerbalAlarmClock.dll \
    bazel-bin/service/KerbalAlarmClock/KRPC.KerbalAlarmClock.xml \
    bazel-bin/service/RemoteTech/KRPC.RemoteTech.dll \
    bazel-bin/service/RemoteTech/KRPC.RemoteTech.xml \
    bazel-bin/service/UI/KRPC.UI.dll \
    bazel-bin/service/UI/KRPC.UI.xml \
    bazel-bin/service/LiDAR/KRPC.LiDAR.dll \
    bazel-bin/service/LiDAR/KRPC.LiDAR.xml \
    bazel-bin/service/DockingCamera/KRPC.DockingCamera.dll \
    bazel-bin/service/DockingCamera/KRPC.DockingCamera.xml \
    service/UI/KRPC.UI.ksp \
    bazel-bin/tools/build/ksp/Google.Protobuf.dll \
    bazel-bin/tools/build/ksp/KRPC.IO.Ports.dll \
    bazel-bin/tools/TestingTools/TestingTools.dll \
    bazel-bin/tools/TestingTools/TestingTools.xml \
    service/SpaceCenter/src/module-manager.cfg \
    $GAMEDATA/
cp -L bazel-krpc/external/module_manager/file/ModuleManager.4.2.2.dll $GAMEDATA/../

mkdir -p $GAMEDATA/PluginData
cp tools/settings.cfg $GAMEDATA/PluginData/

find $GAMEDATA -type f -exec chmod 644 {} \;
find $GAMEDATA -type d -exec chmod 755 {} \;
chmod 644 $GAMEDATA/../ModuleManager.4.2.2.dll

ls -lR $GAMEDATA
