.PHONY: all protobuf bin test dist release clean

all: dist

protobuf:
	make -C .. protobuf-python

bin:
	make -C .. TestServer
	mkdir -p bin/TestServer
	cp ../test/TestServer/bin/Release/TestServer.exe \
	../test/TestServer/bin/Release/kRPC.dll \
	../test/TestServer/bin/Release/Google.ProtocolBuffers.Serialization.dll \
	../test/TestServer/bin/Release/Google.ProtocolBuffers.dll \
	./bin/TestServer/

test: protobuf bin
	python setup.py test

dist: protobuf bin
	python setup.py sdist --formats=gztar,zip

release: protobuf bin
	python setup.py sdist --formats=gztar,zip upload

clean:
	make -C .. protobuf-python-clean
	find . -name "*.pyc" -exec rm -rf {} \;
	-rm -rf dist MANIFEST krpc.egg-info protobuf-*-py*.egg build bin/TestServer
