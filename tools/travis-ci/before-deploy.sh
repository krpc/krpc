#!/bin/bash
set -ev
mkdir deploy

if [[ $TRAVIS_PULL_REQUEST != "false" ]]; then
    NAME="pr/"$TRAVIS_PULL_REQUEST
else
    NAME=$TRAVIS_BRANCH
fi
DEPLOYPATH=deploy/$NAME/$TRAVIS_JOB_NUMBER
VERSION=`tools/krpc-version.sh`
mkdir -p $DEPLOYPATH
cp bazel-bin/krpc-$VERSION.zip $DEPLOYPATH/
(cd $DEPLOYPATH; unzip *.zip)
