#!/bin/bash

# Requires the .NET SDK, and a nuget.org API key in the NUGET_API_KEY
# environment variable.

set -e

VERSION=`tools/krpc-version.sh`

bazel build //client/csharp:nuget
bazel test //client/csharp:test --cache_test_results=no

dotnet nuget push bazel-bin/client/csharp/KRPC.Client.$VERSION.nupkg \
  --source https://api.nuget.org/v3/index.json \
  --api-key "$NUGET_API_KEY"
