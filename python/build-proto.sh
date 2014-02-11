#!/bin/bash

rm -f *_pb2.py *.pyc

cd ../KRPC/Schema; protoc RPC.proto Control.proto Orbit.proto --python_out=../../python/proto
