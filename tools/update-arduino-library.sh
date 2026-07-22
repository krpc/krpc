#!/bin/bash
set -e

VERSION=`tools/krpc-version.sh`
COMMIT=`git rev-parse HEAD`

# Build cnano library, and fetch the nanopb runtime that the Arduino library
# vendors (the cnano archive itself takes nanopb as an external dependency)
echo 'Building library...'
bazel build //client/cnano @c_nanopb//:srcs
nanopb=`pwd`/bazel-krpc/external/+http_archive+c_nanopb

# Clone arduino library repository. Kept out of bazel-bin/client/cnano to avoid
# colliding with the 'arduino' test binary Bazel builds into that directory.
arduino=`pwd`/bazel-bin/client/cnano/arduino-library
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
rm -rf krpc-cnano-$VERSION krpc-cnano-$VERSION.zip
# Vendor the nanopb runtime: headers beside the other krpc_cnano headers
# (they are included as <krpc_cnano/pb.h>), sources at the top level
cp $nanopb/pb.h $nanopb/pb_common.h $nanopb/pb_encode.h $nanopb/pb_decode.h krpc_cnano/
cp $nanopb/pb_common.c $nanopb/pb_encode.c $nanopb/pb_decode.c ./
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
  git push origin main
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
