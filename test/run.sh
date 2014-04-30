#!/bin/bash

PYTHONPATH=../python ./test.py
ret=$?
if [ $ret != 0 ]; then
  exit $ret
fi
