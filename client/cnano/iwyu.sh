#!/bin/bash

root=$( cd $(dirname $0) ; pwd -P )
cd $root

iwyu="include-what-you-use -Xiwyu --no_comments"
includes="-Iinclude -I../../bazel-genfiles/client/cnano/include -I../../bazel-krpc/external/c_nanopb"

for path in src/*.c; do
  ${iwyu} ${includes} ${path}
  ${iwyu} -D__AVR__ ${includes} ${path}
done
