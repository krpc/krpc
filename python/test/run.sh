#!/bin/bash

CSHARP_CONFIG=Release
TEST_SERVER=../../src/TestServer/bin/$CSHARP_CONFIG/TestServer.exe

test -f $TEST_SERVER || make -C ../.. CSHARP_CONFIG=$CSHARP_CONFIG protobuf-csharp TestServer
$TEST_SERVER 1>server.log &
PID=$!
echo "Test server running as process $PID"
# Wait for server to start properly
sleep 1
PYTHONPATH=`pwd`/.. ./test.py
ret=$?
kill $PID
if [ $ret != 0 ]; then
  exit $ret
fi
