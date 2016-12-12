#!/bin/bash
set -ev

if [[ $TRAVIS_PULL_REQUEST != "false" ]]; then
    NAME="pr/"$TRAVIS_PULL_REQUEST
else
    NAME=$TRAVIS_BRANCH
fi

DEPLOYPATH=deploy/$NAME/$TRAVIS_JOB_NUMBER
VERSION=`tools/krpc-version.sh`
echo $VERSION

rm -rf $DEPLOYPATH
mkdir -p $DEPLOYPATH

# Copy archives
cp bazel-bin/krpc-$VERSION.zip $DEPLOYPATH/
cp bazel-bin/tools/krpctools/krpctools-$VERSION.zip $DEPLOYPATH/
cp bazel-bin/krpc-genfiles-$VERSION.zip $DEPLOYPATH/
cp bazel-bin/tools/TestServer/TestServer-$VERSION.zip $DEPLOYPATH/

# Extract release archive
(cd $DEPLOYPATH; unzip -q krpc-$VERSION.zip)

# Extract documentation
mkdir $DEPLOYPATH/doc
cp bazel-bin/doc/html.zip $DEPLOYPATH/doc/html.zip
(cd $DEPLOYPATH/doc; unzip -q html.zip; rm -f html.zip)
