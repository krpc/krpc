#!/bin/bash
set -ev
VERSION=1.1-pre
KEY=$KSP_LIB_1_1_PRE_KEY
mkdir lib/ksp
wget --quiet -O lib/ksp/ksp-lib-$VERSION.tar.gpg https://s3.amazonaws.com/krpc/ksp-lib-$VERSION.tar.gpg
gpg --version
(cd lib/ksp; echo $KEY | gpg -d --batch --no-tty --yes --passphrase-fd=0 ksp-lib-$VERSION.tar.gpg > ksp-lib-$VERSION.tar)
(cd lib/ksp; tar -xf ksp-lib-$VERSION.tar)
rm lib/ksp/ksp-lib-$VERSION.tar.gpg lib/ksp/ksp-lib-$VERSION.tar
