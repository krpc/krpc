.PHONY: all protobuf bin generate build test dist release install clean

CSHARP_CONFIG=Release

all: dist

protobuf:
	make -C .. protobuf-cpp

bin:
	make -C .. TestServer
	mkdir -p bin/TestServer
	cp ../test/TestServer/bin/$(CSHARP_CONFIG)/TestServer.exe \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/kRPC.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/Google.ProtocolBuffers.Serialization.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/Google.ProtocolBuffers.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/kRPC.xml \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/TestServer.xml \
	   ./bin/TestServer/

generate:
	make -C .. protobuf-python
	PYTHONPATH=../python ./generate include/krpc/services ../src/*/bin/$(CSHARP_CONFIG)/*.json
	PYTHONPATH=../python ./generate test/services ../test/TestServer/bin/$(CSHARP_CONFIG)/*.json

build: protobuf bin generate
	./waf configure
	./waf build --notests

test: build bin
	../tools/start-server ./bin/TestServer/TestServer.exe 50011 50012
	./waf build --alltests
	../tools/stop-server

dist: protobuf bin

release: protobuf bin

install:
	./waf install

clean:
	-./waf clean
	find . -name "*.pyc" -exec rm -rf {} \;
	rm -rf bin build include/krpc/services test/services
	rm -f include/krpc/KRPC.pb.h src/KRPC.pb.cc test/Test.pb.h test/Test.pb.cc
