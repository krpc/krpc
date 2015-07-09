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
	$(LUA) krpc/test/init.lua

dist: protobuf bin
	rm -rf dist
	mkdir -p dist/krpc-0.1.0-0
	mkdir -p dist/krpc-0.1.0-0/lua
	cp -R krpc dist/krpc-0.1.0-0/lua/
	(cd dist; tar czvpf krpc-0.1.0-0.tar.gz krpc-0.1.0-0/)

release: protobuf bin
	#TODO

clean:
	make -C .. protobuf-lua-clean
	-rm -rf dist build bin/TestServer
