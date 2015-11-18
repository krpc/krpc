#!/bin/bash

bin/TestServer/TestServer.exe 50011 50012 --quiet &
PID=$!
echo $PID > .testserver.pid
sleep 3
