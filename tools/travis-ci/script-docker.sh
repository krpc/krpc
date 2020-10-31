#!/bin/bash
set -ev
cd /build/krpc
ln -s /usr/lib/mono/4.5 lib/mono-4.5
rm -rf lib/ksp && ln -s /usr/local/lib/ksp lib/ksp

bazel fetch //...
bazel build \
  //:krpc \
  //:csproj \
  //doc:html \
  //doc:compile-scripts \
  //tools/krpctools \
  //tools/TestServer:archive \
  //:ci-test
msbuild KRPC.sln /warnaserror /nowarn:MSB3276,MSB3277,CS1685

bazel test //:ci-test
client/cpp/test-build.sh
client/cnano/test-build.sh

tools/dist/genfiles.sh
tools/travis-ci/before-deploy.sh
