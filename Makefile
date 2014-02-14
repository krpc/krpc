KSP_DIR := ../KerbalSpaceProgram
KRPC_DIR := $(KSP_DIR)/GameData/kRPC

CSHARP_PROJECTS := kRPC kRPCTest TestingTools
CSHARP_PROJECT_DIRS := $(foreach project,$(CSHARP_PROJECTS),src/$(project))
CSHARP_BIN_DIRS := $(foreach project,$(CSHARP_PROJECT_DIRS),$(project)/obj) $(foreach project,$(CSHARP_PROJECTS),$(project)/bin)

CSHARP_PROTOGEN := "lib/protobuf-csharp-port-2.4.1.521-release-binaries/tools/ProtoGen.exe"
PROTOS := RPC Control Orbit
PROTOS := $(foreach proto,$(PROTOS),src/kRPC/Schema/$(proto).proto)

all: dist

build: protobuf
	mdtool build -c:Release $(foreach $(project),$(CSHARP_PROJECT_DIRS),$($(project)/$(project).csproj))
	find . -name "*.pyc" -exec rm -rf {} \;

dist: build
	rm -rf dist
	mkdir -p dist
	cp -r \
		LICENSE.txt \
		src/kRPC/bin/Release/krpc.dll \
		lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.dll \
		lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.Serialization.dll \
		python \
		dist
	cp lib/protobuf-csharp-port-2.4.1.521-release-binaries/license.txt dist/protobuf-license.txt
	mkdir -p dist/schema
	cp -r src/kRPC/Schema/*.proto dist/schema/

pre-release: dist
	cd dist; zip krpc-pre-`date +"%Y-%m-%d"`.zip ./*

clean: protobuf-clean
	rm -rf dist $(CSHARP_BIN_DIRS)
	find . -name "*.pyc" -exec rm -rf {} \;

dist-clean: clean
	rm -r dist

# Run release copy in KSP with testing tools

ksp: dist
	rm -rf $(KRPC_DIR)
	mkdir -p $(KRPC_DIR)
	cp -r dist/* src/TestingTools/bin/Release/TestingTools.dll $(KRPC_DIR)/
	$(KSP_DIR)/KSP.x86_64 &
	tail -f "$(HOME)/.config/unity3d/Squad/Kerbal Space Program/Player.log"

# Protocol Buffers

protobuf: protobuf-csharp protobuf-python

python/proto/%.py:
	rm -f $(@:.py=.pyc)
	mkdir -p python/proto
	cd src/kRPC/Schema; protoc $*.proto --python_out=../../../python/proto

%.protobin:
	protoc $*.proto -o$*.protobin --include_imports

src/kRPC/Schema/%.cs: src/kRPC/Schema/%.protobin
	mono $(CSHARP_PROTOGEN) \
		src/kRPC/Schema/$*.protobin -namespace=KRPC.Schema.$* \
		-umbrella_classname=$* -output_directory=src/kRPC/Schema

protobuf-csharp: $(PROTOS) $(PROTOS:.proto=.cs)

protobuf-python: $(PROTOS) $(foreach proto,$(notdir $(PROTOS:.proto=.py)),python/proto/$(proto))
	echo "" > python/proto/__init__.py

protobuf-clean:
	rm -rf $(PROTOS:proto=cs) $(PROTOS:proto=protobin) python/proto
