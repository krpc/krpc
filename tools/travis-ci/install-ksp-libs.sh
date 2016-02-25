#!/bin/bash
set -ev
VERSION=1.0.5
KEY=$KSP_LIB_1_0_5_KEY
mkdir lib/ksp
wget --quiet -O lib/ksp/ksp-lib-$VERSION.tar.gpg https://s3.amazonaws.com/krpc/ksp-lib-$VERSION.tar.gpg
(cd lib/ksp; gpg --no-tty --batch --yes --passphrase $KEY ksp-lib-$VERSION.tar.gpg)
(cd lib/ksp; tar -xf ksp-lib-$VERSION.tar)
rm lib/ksp/ksp-lib-$VERSION.tar.gpg lib/ksp/ksp-lib-$VERSION.tar
