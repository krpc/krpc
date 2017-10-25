#!/bin/bash
set -ev

VERSION=`tools/krpc-version.sh`
COMMIT=`git rev-parse HEAD`

# Build cnano library
bazel build //client/cnano

# Clone arduino library repository
arduino=`pwd`/bazel-genfiles/client/cnano/arduino
rm -rf $arduino
git clone git@github.com:krpc/krpc-arduino $arduino

# Update src files
rm -rf $arduino/src
mkdir $arduino/src
cp bazel-bin/client/cnano/krpc-cnano-$VERSION.zip $arduino/src/
cd $arduino/src
unzip krpc-cnano-$VERSION.zip
mv krpc-cnano-$VERSION/include/* ./
mv krpc-cnano-$VERSION/src/*.c ./
rm Makefile.*
for file in *.c; do
  mv "$file" "$(basename "$file" .c).cpp"
done
rm -rf krpc-cnano-$VERSION krpc-cnano-$VERSION.zip
cd $arduino

# Update library.properties
sed -i -e "s/version=.*/version=${VERSION}/g" library.properties

git add .
git diff-index --quiet HEAD || git commit -m "Updated from https://github.com/krpc/krpc commit $COMMIT"
git push origin master
