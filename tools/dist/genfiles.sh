#!/bin/bash

set -e

bazel build //:csproj

VERSION=`tools/krpc-version.sh`
FILE=bazel-bin/krpc-genfiles-$VERSION.zip

rm -f $FILE
zip -r -MM $FILE \
    bazel-bin/core/AssemblyInfo.cs \
    bazel-bin/core/TestAssemblyInfo.cs \
    bazel-bin/server/AssemblyInfo.cs \
    bazel-bin/protobuf/KRPC_unity.cs \
    bazel-bin/client/csharp/AssemblyInfo.cs \
    bazel-bin/protobuf/KRPC.cs \
    bazel-bin/client/csharp/Services/ \
    bazel-bin/service/*/AssemblyInfo.cs \
    bazel-bin/tools/*/AssemblyInfo.cs \
    bazel-krpc/external/_main~_repo_rules~csharp_protobuf_unity \
    bazel-krpc/external/_main~_repo_rules~csharp_protobuf \
    bazel-krpc/external/_main~_repo_rules~csharp_nunit \
    bazel-krpc/external/_main~_repo_rules~csharp_moq \
    bazel-krpc/external/_main~_repo_rules~csharp_json \
    bazel-krpc/external/_main~_repo_rules~csharp_options \
    bazel-krpc/external/_main~_repo_rules~csharp_castle_core \
    bazel-bin/tools/build/ksp/Google.Protobuf.dll \
    bazel-bin/tools/build/ksp/KRPC.IO.Ports.dll \
    bazel-bin/tools/build/mono-4.5/Google.Protobuf.dll
