#!/bin/bash
set -ev

VERSION=`tools/krpc-version.sh`
COMMIT=`git rev-parse HEAD`

# Build cnano library
bazel build //client/cnano

# Clone arduino library
arduino=bazel-genfiles/client/cnano/arduino
rm -rf $arduino
git clone git@github.com:krpc/krpc-arduino $arduino

# Update arduino library
rm -rf $arduino/*.h $arduino/*.cpp $arduino/krpc
cp bazel-bin/client/cnano/krpc-cnano-$VERSION.zip $arduino/
cd $arduino
unzip krpc-cnano-$VERSION.zip
mv krpc-cnano-$VERSION/include/* ./
mv krpc-cnano-$VERSION/src/*.c ./
rm Makefile.*
for file in *.c; do
  mv "$file" "$(basename "$file" .c).cpp"
done
rm -rf krpc-cnano-$VERSION krpc-cnano-$VERSION.zip
git add .
git diff-index --quiet HEAD || git commit -m "Updated from https://github.com/krpc/krpc commit $COMMIT"
#git push origin master
