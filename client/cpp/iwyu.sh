#!/bin/bash

IWYU="include-what-you-use -Xiwyu --no_comments -std=c++11 -Iinclude -I../../bazel-genfiles/client/cpp/include -I../../bazel-krpc/external/cpp_googletest/googletest/include -I../../bazel-krpc/external/cpp_googletest/googlemock/include -I../../bazel-genfiles/client/cpp/test -I../../bazel-krpc/external/cpp_protobuf/src/google/protobuf"

${IWYU} src/client.cpp
${IWYU} src/connection.cpp
${IWYU} src/decoder.cpp
${IWYU} src/encoder.cpp
${IWYU} src/krpc.cpp
${IWYU} src/platform.cpp
${IWYU} src/stream_manager.cpp

${IWYU} test/test_client.cpp
${IWYU} test/test_encodedecode.cpp
${IWYU} test/test_object.cpp
${IWYU} test/test_stream.cpp
${IWYU} test/test_decoder.cpp
${IWYU} test/test_encoder.cpp
${IWYU} test/test_services.cpp
