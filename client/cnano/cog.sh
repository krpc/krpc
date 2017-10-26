#!/bin/bash
set -ev
python -m cogapp -r \
  client/cnano/include/krpc/encoder.h \
  client/cnano/src/encoder.c \
  client/cnano/include/krpc/decoder.h \
  client/cnano/src/decoder.c
