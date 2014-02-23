.PHONY: all protobuf test clean

all: protobuf

protobuf:
	make -C .. protobuf-python

test:
	(cd test; ./run.sh)

clean:
	make -C .. protobuf-python-clean
	find . -name "*.pyc" -exec rm -rf {} \;
	-rm -f test/server.log
