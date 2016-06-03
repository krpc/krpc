#!/bin/bash
set -ev
VERSION=`tools/ksp-version.sh`
mkdir -p lib/ksp
wget -O lib/ksp/ksp-$VERSION.tar.gz https://s3.amazonaws.com/krpc/lib/ksp-$VERSION.tar.gz
(cd lib/ksp; tar -xf ksp-$VERSION.tar.gz)
rm lib/ksp/ksp-$VERSION.tar.gz
