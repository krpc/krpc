#!/bin/bash
set -ev
VERSION=`tools/ksp-version.sh`
KEY_VAR="KEY_KSP_${VERSION//./_}"
KEY=${!KEY_VAR}
mkdir -p lib/ksp
wget -O lib/ksp/ksp-$VERSION.tar.gpg https://s3.amazonaws.com/krpc/lib/ksp-$VERSION.tar.gpg
gpg --version
(cd lib/ksp; echo $KEY | gpg -d --batch --no-tty --yes --passphrase-fd=0 ksp-$VERSION.tar.gpg > ksp-$VERSION.tar)
(cd lib/ksp; tar -xf ksp-$VERSION.tar)
rm lib/ksp/ksp-$VERSION.tar.gpg lib/ksp/ksp-$VERSION.tar
