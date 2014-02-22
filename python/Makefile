.PHONY: all protobuf test clean

all: protobuf

protobuf:
	make -C .. protobuf-python

test:
	PYTHONPATH=`pwd` test/test.py

clean:
	make -C .. protobuf-python-clean
