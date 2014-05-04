#!/bin/bash

PYTHONPATH=../../python KSP_DIR="$1" ./test.py
ret=$?
if [ $ret != 0 ]; then
  exit $ret
fi
