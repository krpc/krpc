#!/bin/bash
set -ev

if [[ $TRAVIS_PULL_REQUEST != "false" ]]; then
    NAME="pr/"$TRAVIS_PULL_REQUEST
else
    NAME=$TRAVIS_BRANCH
fi
DEPLOYPATH=deploy/$NAME/$TRAVIS_JOB_NUMBER
VERSION=`tools/krpc-version.sh`
rm -rf $DEPLOYPATH
mkdir -p $DEPLOYPATH
cp bazel-bin/krpc-$VERSION.zip $DEPLOYPATH/
cp bazel-bin/tools/krpctools/krpctools-$VERSION.zip $DEPLOYPATH/
cp bazel-bin/krpc-genfiles-$VERSION.zip $DEPLOYPATH/
(cd $DEPLOYPATH; unzip krpc-$VERSION.zip)
