.PHONY: all protobuf test dist release clean

all: protobuf

protobuf:
	make -C .. protobuf-python

test:
	(cd test; ./run.sh)

dist:
	python setup.py sdist --formats=gztar,zip

release:
	python setup.py register
	python setup.py sdist --formats=gztar,zip upload

clean:
	make -C .. protobuf-python-clean
	find . -name "*.pyc" -exec rm -rf {} \;
	-rm -f test/server.log
	-rm -rf dist
