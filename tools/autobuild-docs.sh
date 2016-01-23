#!/bin/bash

# Usage tools/autobuild-docs.sh PORT

set -e

port=$1

trap ctrl_c INT
function ctrl_c() {
    pkill -f "python -m SimpleHTTPServer $port" || true
    exit 0
}

while [ true ] ; do
    sleep 0.2
    if bazel build //doc:html ; then
        rm -rf docs
        unzip -q bazel-bin/doc/html.zip -d docs
    fi
    (cd docs; python -m SimpleHTTPServer $port &)
    inotifywait -r -e modify,move,create,delete doc --exclude='/\.'
    pkill -f "python -m SimpleHTTPServer $port"
done
