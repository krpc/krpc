KSP_DIR := "../Kerbal Space Program"
KRPC_DIR := $(KSP_DIR)/GameData/kRPC

CSHARP_PROJECTS := kRPC kRPCServices kRPCTest TestingTools
CSHARP_PROJECT_DIRS := $(foreach project,$(CSHARP_PROJECTS),src/$(project))
CSHARP_BIN_DIRS := $(foreach project,$(CSHARP_PROJECT_DIRS),$(project)/obj) $(foreach project,$(CSHARP_PROJECT_DIRS),$(project)/bin)

PROTOC := protoc
CSHARP_PROTOGEN := "tools/ProtoGen.exe"
PROTOS := src/kRPC/Schema/KRPC.proto \
          src/kRPCServices/Schema/Control.proto \
          src/kRPCServices/Schema/Orbit.proto \
          src/kRPCServices/Schema/Flight.proto \
          src/kRPCServices/Schema/Vessel.proto

all: dist

%.dll:
	mdtool build -t:Build -c:Release src/$*/$*.csproj

build: protobuf $(foreach project,$(CSHARP_PROJECTS),$(project).dll)
	make -C src/kRPC/icons all
	find . -name "*.pyc" -exec rm -rf {} \;

dist: build
	rm -rf dist
	mkdir -p dist
	# Licenses
	cp LICENSE.txt dist
	cp lib/protobuf-csharp-port-2.4.1.521-release-binaries/license.txt dist/protobuf-license.txt
	cp lib/toolbar/LICENSE.txt dist/toolbar-license.txt
	# Plugin files
	mkdir -p dist/GameData/kRPC
	cp -r \
		src/kRPC/bin/Release/krpc.dll \
		src/kRPCServices/bin/Release/krpc-services.dll \
		src/kRPC/bin/*.png \
		lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.dll \
		lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.Serialization.dll \
		dist/GameData/kRPC/
	# Toolbar
	unzip lib/toolbar/Toolbar-1.6.0.zip -d dist
	mv dist/Toolbar-1.6.0/GameData/* dist/GameData/
	rm -r dist/Toolbar-1.6.0
	# Python client library
	mkdir -p dist/python
	cp -r python/*.py python/*.craft python/proto dist/python/
	# Schema
	mkdir -p dist/schema
	cp -r $(PROTOS) dist/schema/

pre-release: dist
	cd dist; zip -r krpc-pre-`date +"%Y-%m-%d"`.zip ./*

clean: protobuf-clean
	rm -rf $(CSHARP_BIN_DIRS)
	find . -name "*.pyc" -exec rm -rf {} \;

dist-clean: clean
	rm -rf dist

install: dist
	rm -rf $(KRPC_DIR)
	mkdir -p $(KRPC_DIR)
	cp -r dist/GameData/kRPC/* $(KRPC_DIR)/

ksp: install
	cp src/TestingTools/bin/Release/TestingTools.dll $(KRPC_DIR)/
	$(KSP_DIR)/KSP.x86_64 &
	tail -f "$(HOME)/.config/unity3d/Squad/Kerbal Space Program/Player.log"

# Protocol Buffers

protobuf: protobuf-csharp protobuf-python

protobuf-csharp: $(PROTOS) $(PROTOS:.proto=.cs)

protobuf-python: $(PROTOS) $(PROTOS:.proto=.py)
	echo "" > python/proto/__init__.py

%.protobin: %.proto
	$(PROTOC) $*.proto -o$*.protobin --include_imports

%.py: %.proto
	# FIXME: dependency checks don't work for this, as target is different to the file that's created
	rm -f $*.pyc
	mkdir -p python/proto
	cp $*.proto python/proto/$(notdir $*.proto)
	$(PROTOC) python/proto/$(notdir $*.proto) --python_out=.
	mv python/proto/$(notdir $*_pb2.py) python/proto/$(notdir $*.py)
	rm python/proto/$(notdir $*.proto)

%.cs: %.protobin
	# TODO: put .cs proto files in different namespaces?
	mono $(CSHARP_PROTOGEN) \
		$*.protobin -namespace=KRPC.Schema.$(basename $(notdir $@)) \
		-umbrella_classname=$(basename $(notdir $@)) -output_directory=$(dir $@)

protobuf-clean:
	rm -rf $(PROTOS:proto=cs) $(PROTOS:proto=protobin) python/proto
