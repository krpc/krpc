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
    bazel-krpc/external/csharp_protobuf/Google.Protobuf.nuspec \
    bazel-krpc/external/csharp_protobuf/lib/net45/Google.Protobuf.* \
    bazel-krpc/external/csharp_protobuf_net35/file/Google.Protobuf.* \
    bazel-krpc/external/csharp_nunit/license.txt \
    bazel-krpc/external/csharp_nunit/bin/framework/nunit.framework.* \
    bazel-krpc/external/csharp_moq/Moq.nuspec \
    bazel-krpc/external/csharp_moq/lib/net40/Moq.* \
    bazel-krpc/external/csharp_json/Newtonsoft.Json.nuspec \
    bazel-krpc/external/csharp_json/lib/net45/Newtonsoft.Json.* \
    bazel-krpc/external/csharp_options/NDesk.Options.nuspec \
    bazel-krpc/external/csharp_options/lib/NDesk.Options.*
