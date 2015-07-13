#!/bin/bash

# Update the version, e.g. ./tools/set-version.sh 0.1.5

VERSION=$1
echo $VERSION >VERSION.txt
sed -i -E 's/AssemblyVersion \(".+"\)/AssemblyVersion \("'$VERSION'\")/' \
       src/kRPC/Properties/AssemblyInfo.cs
sed -i -E 's/AssemblyVersion \(".+"\)/AssemblyVersion \("'$VERSION'\")/' \
       src/kRPCSpaceCenter/Properties/AssemblyInfo.cs
sed -i -E 's/AssemblyVersion \(".+"\)/AssemblyVersion \("'$VERSION'\")/' \
       src/kRPCInfernalRobotics/Properties/AssemblyInfo.cs
sed -i -E 's/AssemblyVersion \(".+"\)/AssemblyVersion \("'$VERSION'\")/' \
       src/kRPCKerbalAlarmClock/Properties/AssemblyInfo.cs
sed -i -E 's/AssemblyVersion \(".+"\)/AssemblyVersion \("'$VERSION'\")/' \
       test/TestServer/Properties/AssemblyInfo.cs
sed -i -E "s/version='.+'/version='"$VERSION"'/" python/setup.py
sed -i -E "s/release = '.+'/release = '"$VERSION"'/" doc/src/conf.py
