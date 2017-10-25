#!/bin/bash

IWYU="include-what-you-use -Xiwyu --no_comments -Iinclude -I../../bazel-genfiles/client/cnano/include -I../../bazel-krpc/external/c_nanopb"

${IWYU} src/krpc.c
${IWYU} src/communication.c
${IWYU} src/decoder.c
${IWYU} src/encoder.c
${IWYU} src/error.c
${IWYU} src/memory.c
${IWYU} src/utils.c
