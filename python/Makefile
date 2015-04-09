.PHONY: all protobuf bin test dist release clean

PYTHON2 = python2
PYTHON3 = python3
CSHARP_CONFIG = Release

all: dist

protobuf:
	make -C .. protobuf-python

bin:
	make -C .. TestServer
	mkdir -p bin/TestServer
	cp ../test/TestServer/bin/$(CSHARP_CONFIG)/TestServer.exe \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/kRPC.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/Google.ProtocolBuffers.Serialization.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/Google.ProtocolBuffers.dll \
	   ./bin/TestServer/

test: protobuf bin
	$(PYTHON2) setup.py test
	$(PYTHON3) setup.py test

dist: protobuf bin
	$(PYTHON2) setup.py sdist --formats=gztar,zip

release: protobuf bin
	$(PYTHON2) setup.py sdist --formats=gztar,zip register upload

clean:
	make -C .. protobuf-python-clean
	-find . -name "*.pyc" -exec rm -rf {} \;
	-find . -name "__pycache__" -exec rm -rf {} \;
	-rm -rf dist MANIFEST krpc.egg-info protobuf-*-py*.egg build bin/TestServer
