.PHONY: all protobuf bin test rockspec release clean

LUA = lua5.2
CSHARP_CONFIG = Release
VERSION = 0.1.9
DISTDIR = dist/krpc-$(VERSION)-0

all: release

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

rockspec:
	mkdir -p dist
	sed "s/%VERSION%/$(VERSION)/g" krpc.rockspec > dist/krpc-$(VERSION)-0.rockspec

release: protobuf bin rockspec
	mkdir -p $(DISTDIR)
	mkdir -p $(DISTDIR)/lua
	cp -R krpc $(DISTDIR)/lua/
	(cd dist; tar czvpf krpc-$(VERSION)-0.tar.gz krpc-$(VERSION)-0/)

clean:
	make -C .. protobuf-lua-clean
	-rm -rf dist bin
