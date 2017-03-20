#!/bin/bash
set -ev
cd /build/krpc

bazel fetch //...
bazel build \
  //:krpc \
  //:csproj \
  //doc:html \
  //doc:compile-scripts \
  //tools/krpctools \
  //tools/TestServer:archive \
  //:ci-test
xbuild KRPC.sln

bazel test //:ci-test
client/cpp/test-build.sh

tools/dist/genfiles.sh
tools/travis-ci/before-deploy.sh
