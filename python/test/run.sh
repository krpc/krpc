#!/bin/bash

TEST_SERVER=../../src/TestServer/bin/Debug/TestServer.exe

test -f $TEST_SERVER || make -C ../.. CSHARP_CONFIG=Debug protobuf-csharp TestServer
$TEST_SERVER 1>server.log &
PID=$!
echo "Test server running as process $PID"
# Wait for server to start properly
sleep 0.1
PYTHONPATH=`pwd`/.. ./test.py
kill $PID
