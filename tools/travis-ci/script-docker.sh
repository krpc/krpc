#!/bin/bash
set -ev
cd /build/krpc
rm -rf lib/ksp && ln -s /usr/local/lib/ksp lib/ksp

bazel test //client/cpp:ci-test
