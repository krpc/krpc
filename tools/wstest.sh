#!/bin/bash
# Run the autobahn test suite for the websockets server

bazel build //tools/TestServer

TMP_DIR=bazel-bin/wstest
REPORT_DIR=`pwd`/bazel-bin/wstest-report
virtualenv $TMP_DIR/env --python python3 --system-site-packages
source $TMP_DIR/env/bin/activate
pip install autobahntestsuite

pkill -f TestServer.exe
bazel-bin/tools/TestServer/TestServer --type=websockets-echo --quiet 1>$TMP_DIR/stdout &

while ! grep "Server started successfully" $TMP_DIR/stdout >/dev/null 2>&1; do sleep 0.1 ; done
RPC_PORT=`awk '/rpc_port = /{print $NF}' $TMP_DIR/stdout`
STREAM_PORT=`awk '/stream_port = /{print $NF}' $TMP_DIR/stdout`
echo "Server started, rpc port = $RPC_PORT, stream port = $STREAM_PORT"

echo "{ \"outdir\": \"$REPORT_DIR\", \"servers\": [ {\"url\": \"ws://localhost:$RPC_PORT\"} ], \"cases\": [\"*\"] }" > $TMP_DIR/krpc.spec
wstest -m fuzzingclient -s $TMP_DIR/krpc.spec

deactivate
pkill -f TestServer.exe

echo "Test run complete. Results can be found in $REPORT_DIR/index.html"
