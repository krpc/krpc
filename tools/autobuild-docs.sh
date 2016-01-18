#!/bin/bash

root=`pwd`
pidfile=tools/autobuild-docs.pid

trap ctrl_c INT
function ctrl_c() {
    if [ -f $pidfile ]; then
        kill `cat $pidfile`
    fi
    exit 0
}

if [ -f $pidfile ]; then
    kill `cat $pidfile`
fi

while [ true ] ; do
    bazel build //doc:html
    rm -rf docs
    unzip -q bazel-bin/doc/html.zip -d docs
    cd docs
    python -m SimpleHTTPServer &
    cd $root
    serverpid=$!
    echo $serverpid > $pidfile
    inotifywait -r -e modify,move,create,delete doc --exclude='/\.'
    kill $serverpid
    rm $pidfile
done
