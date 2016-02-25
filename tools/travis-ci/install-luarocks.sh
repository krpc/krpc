#!/bin/bash
set -ev
VERSION=2.2.0
wget http://keplerproject.github.io/luarocks/releases/luarocks-$VERSION.tar.gz
tar -xf luarocks-$VERSION.tar.gz
(cd luarocks-$VERSION; ./configure && make build && sudo make install)
rm -rf luarocks-$VERSION luarocks-$VERSION.tar.gz
