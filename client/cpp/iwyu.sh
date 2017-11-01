#!/bin/bash

root=$( cd $(dirname $0) ; pwd -P )
cd $root

iwyu="include-what-you-use -Xiwyu --no_comments -std=c++11"
includes="-Iinclude -I../../bazel-genfiles/client/cpp/include -I../../bazel-krpc/external/cpp_googletest/googletest/include -I../../bazel-krpc/external/cpp_googletest/googlemock/include -I../../bazel-genfiles/client/cpp/test -I../../bazel-krpc/external/cpp_protobuf/src/google/protobuf"

for path in src/*.cpp; do
  ${iwyu} ${includes} ${path}
done

for path in test/*.cpp; do
  ${iwyu} ${includes} ${path}
done
