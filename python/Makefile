.PHONY: all protobuf bin test dist release clean

all: dist

protobuf:
	make -C .. protobuf-python

bin:
	make -C .. TestServer
	mkdir -p bin/TestServer
	cp ../src/TestServer/bin/Release/TestServer.exe \
           ../src/TestServer/bin/Release/kRPC.dll \
           ../src/TestServer/bin/Release/Google.ProtocolBuffers.Serialization.dll \
           ../src/TestServer/bin/Release/Google.ProtocolBuffers.dll \
           ./bin/TestServer/

test: protobuf bin
	python setup.py test

dist: protobuf bin
	python setup.py sdist --formats=gztar,zip

release: protobuf bin
	python setup.py register
	python setup.py sdist --formats=gztar,zip upload

clean:
	make -C .. protobuf-python-clean
	find . -name "*.pyc" -exec rm -rf {} \;
	-rm -rf dist MANIFEST bin/TestServer
