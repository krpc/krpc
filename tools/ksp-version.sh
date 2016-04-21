#!/bin/bash

cat config.bzl | grep '^ksp_version = ' | sed -n -e "s/ksp_version\s*=\s*'\(.*\)'/\1/p"
