#!/bin/bash

root="$(dirname "$(dirname "$(readlink -f "$0")")")"

cat "$root/config.bzl" | grep '^version = ' | sed -n -e "s/version\s*=\s*\"\(.*\)\"/\1/p"
