#!/bin/bash

set -e

bazel build //:csproj

VERSION=`tools/krpc-version.sh`
FILE=bazel-bin/krpc-genfiles-$VERSION.zip

rm -f $FILE
zip $FILE \
    bazel-bin/server/AssemblyInfo.cs \
    bazel-genfiles/protobuf/KRPC.cs \
    bazel-bin/client/csharp/AssemblyInfo.cs \
    bazel-genfiles/protobuf/KRPC.cs \
    bazel-genfiles/client/csharp/Services/* \
    bazel-bin/service/*/AssemblyInfo.cs \
    bazel-bin/tools/*/AssemblyInfo.cs \
    bazel-genfiles/tools/cslibs/* \
    bazel-genfiles/tools/cslibs/**/*
