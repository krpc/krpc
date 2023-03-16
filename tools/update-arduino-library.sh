#!/bin/bash
set -e

VERSION=`tools/krpc-version.sh`
COMMIT=`git rev-parse HEAD`

# Build cnano library
echo 'Building library...'
bazel build //client/cnano

# Clone arduino library repository
arduino=`pwd`/bazel-bin/client/cnano/arduino
if [ ! -d $arduino ]; then
  echo 'Cloning krpc-arduino repository...'
  git clone git@github.com:krpc/krpc-arduino $arduino
fi

# Update src files
echo 'Updating src files...'
rm -rf $arduino/src
mkdir $arduino/src
cp bazel-bin/client/cnano/krpc-cnano-$VERSION.zip $arduino/src/
cd $arduino/src
unzip -q krpc-cnano-$VERSION.zip
# Restructure files
mv krpc-cnano-$VERSION/include/* ./
mv krpc-cnano-$VERSION/src/*.c ./
rm Makefile.*
rm -rf krpc-cnano-$VERSION krpc-cnano-$VERSION.zip
# Rename .c to .cpp for arduino builder
for file in *.c; do
  mv "$file" "$(basename "$file" .c).cpp"
done
# Enable PB_NO_ERRMSG by default
echo -e "#ifndef PB_NO_ERRMSG\n#define PB_NO_ERRMSG\n#endif\n\n$(cat krpc_cnano/pb.h)" > krpc_cnano/pb.h
cd $arduino
echo "Arduino library source in $arduino"

# Update library.properties
echo "Updating library.properties to v${VERSION}"
sed -i -e "s/version=.*/version=${VERSION}/g" library.properties

if [ "$1" == "push" ]; then
  echo 'Pushing changes...'
  git add .
  git diff-index --quiet HEAD || git commit -m "Updated from https://github.com/krpc/krpc commit $COMMIT"
  git push origin master
else
  echo "Skipping pushing changes (enable by passing 'push' as argument)"
fi

if [ "$1" == "release" ]; then
  echo 'Pushing release tag...'
  git tag -a v$VERSION -m v$VERSION
  git push --tags
else
  echo "Skipping pushing release tag (enable by passing 'release' as argument)"
fi

echo 'Done'
