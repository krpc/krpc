.PHONY: all protobuf bin test dist release clean

LUA = lua5.2
CSHARP_CONFIG = Release

all: dist

protobuf:
	make -C .. protobuf-lua

bin:
	make -C .. TestServer
	mkdir -p bin/TestServer
	cp ../test/TestServer/bin/$(CSHARP_CONFIG)/TestServer.exe \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/kRPC.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/Google.ProtocolBuffers.Serialization.dll \
	   ../test/TestServer/bin/$(CSHARP_CONFIG)/Google.ProtocolBuffers.dll \
	   ./bin/TestServer/

test: protobuf bin
	lua5.1 test.lua
	#TODO

dist: protobuf bin
	#TODO

release: protobuf bin
	#TODO

clean:
	make -C .. protobuf-lua-clean
	-rm -rf dist build bin/TestServer
