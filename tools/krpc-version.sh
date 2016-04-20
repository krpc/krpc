#!/bin/bash

cat config.bzl | grep '^version = ' | sed -n -e "s/version\s*=\s*'\(.*\)'/\1/p"
