#!/bin/bash

set -e

MONO=mono
NUGET=bazel-krpc/external/csharp_nuget/file/nuget.exe
VERSION=`tools/krpc-version.sh`

bazel build //client/csharp:nuget
bazel test //client/csharp:test --cache_test_results=no

$MONO $NUGET push bazel-bin/client/csharp/KRPC.Client.$VERSION-pre.nupkg
