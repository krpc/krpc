.PHONY: all protobuf generate build test install clean

CSHARP_CONFIG=Release

all: build

protobuf:
	make -C .. protobuf-cpp

bin:
	make -C .. TestServer
	mkdir -p bin/TestServer
	cp ../test/TestServer/bin/$(CSHARP_CONFIG)/TestServer.exe \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/kRPC.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/Google.ProtocolBuffers.Serialization.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/Google.ProtocolBuffers.dll \
	   ./bin/TestServer/

generate: protobuf
	PYTHONPATH=../python ./generate

build: generate
	./waf configure
	./waf build --notests

test: bin
	./startserver.sh
	./waf build --alltests
	./stopserver.sh

install:
	./waf install

clean:
	-./waf clean
	rm -rf include/krpc/KRPC.pb.h src/KRPC.pb.cc
	rm -rf include/krpc/services
	rm -rf bin
	find . -name "*.pyc" -exec rm -rf {} \;
	rm -rf build
