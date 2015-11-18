#!/bin/bash

PID=`cat .testserver.pid`
kill $PID
rm .testserver.pid
