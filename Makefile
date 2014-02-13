KSP_DIR := ../KerbalSpaceProgram
KRPC_DIR := $(KSP_DIR)/GameData/kRPC

CSHARP_PROJECTS := KRPC KRPCTest TestingTools
CSHARP_BIN_DIRS := $(foreach project,$(CSHARP_PROJECTS),$(project)/obj) $(foreach project,$(CSHARP_PROJECTS),$(project)/bin)

CSHARP_PROTOGEN := "Lib/protobuf-csharp-port-2.4.1.521-release-binaries/tools/ProtoGen.exe"
PROTOS := KRPC/Schema/RPC.proto KRPC/Schema/Control.proto KRPC/Schema/Orbit.proto

all: dist

build: protobuf
	mdtool build -c:Release $(foreach $(project),$(CSHARP_PROJECTS),$($(project)/$(project).csproj))
	find . -name "*.pyc" -exec rm -rf {} \;

dist: build
	rm -rf dist
	mkdir -p dist
	cp -r \
		LICENSE.txt \
		KRPC/bin/Release/krpc.dll \
		Lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.dll \
		Lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.Serialization.dll \
		python \
		dist
	cp Lib/protobuf-csharp-port-2.4.1.521-release-binaries/license.txt dist/protobuf-license.txt
	mkdir -p dist/schema
	cp -r KRPC/Schema/*.proto dist/schema/

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
	cp -r dist/* TestingTools/bin/Release/TestingTools.dll $(KRPC_DIR)/
	$(KSP_DIR)/KSP.x86_64 &
	tail -f "$(HOME)/.config/unity3d/Squad/Kerbal Space Program/Player.log"

# Protocol Buffers

protobuf: protobuf-csharp protobuf-python

python/proto/%.py:
	rm -f $(@:.py=.pyc)
	mkdir -p python/proto
	cd KRPC/Schema; protoc $*.proto --python_out=../../python/proto

%.protobin:
	protoc $*.proto -o$*.protobin --include_imports

KRPC/Schema/%.cs: KRPC/Schema/%.protobin
	mono $(CSHARP_PROTOGEN) \
		KRPC/Schema/$*.protobin -namespace=KRPC.Schema.$* \
		-umbrella_classname=$* -output_directory=KRPC/Schema

protobuf-csharp: $(PROTOS) $(PROTOS:.proto=.cs)

protobuf-python: $(PROTOS) $(foreach proto,$(notdir $(PROTOS:.proto=.py)),python/proto/$(proto))
	echo "" > python/proto/__init__.py

protobuf-clean:
	rm -rf $(PROTOS:proto=cs) $(PROTOS:proto=protobin) python/proto
