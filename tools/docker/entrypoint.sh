#!/bin/bash
set -e

user_id=${local_user_id:-1000}
echo "Starting with UID : $user_id"
addgroup -q --gid $user_id build
adduser -q --system --uid $user_id --home /build --disabled-password --ingroup build build
usermod -aG sudo build
passwd -q -d build
chown build:build -R /build
cd /build

exec /usr/sbin/gosu build ${@:-bash}
