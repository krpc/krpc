# Note: This must be an absolute path
KSP_DIR = "$(shell pwd)/../Kerbal Space Program"

VERSION = $(shell cat VERSION)

DIST_DIR = dist
DIST_LIBS = \
  lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.dll \
  lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.Serialization.dll
DIST_ICONS = src/kRPC/bin/icons

CSHARP_MAIN_PROJECTS  = kRPC kRPCServices
CSHARP_TEST_PROJECTS  = kRPCTest TestServer
CSHARP_OTHER_PROJECTS = TestingTools
CSHARP_CONFIG = Release

CSHARP_PROJECTS  = $(CSHARP_MAIN_PROJECTS) $(CSHARP_TEST_PROJECTS) $(CSHARP_OTHER_PROJECTS)
CSHARP_BINDIRS   = $(foreach PROJECT,$(CSHARP_PROJECTS),src/$(PROJECT)/bin) \
                   $(foreach PROJECT,$(CSHARP_PROJECTS),src/$(PROJECT)/obj)
CSHARP_MAIN_LIBRARIES = $(foreach PROJECT,$(CSHARP_MAIN_PROJECTS),src/$(PROJECT)/bin/$(CSHARP_CONFIG)/$(PROJECT).dll)
CSHARP_LIBRARIES      = $(foreach PROJECT,$(CSHARP_PROJECTS),src/$(PROJECT)/bin/$(CSHARP_CONFIG)/$(PROJECT).dll)

PROTOS = $(wildcard src/kRPC/Schema/*.proto) $(wildcard src/kRPCServices/Schema/*.proto)

PROTOC = protoc
PROTOGEN = tools/ProtoGen.exe
MDTOOL = mdtool
MONODIS = monodis
NUNIT_CONSOLE = nunit-console
UNZIP = unzip
MARKDOWN = markdown
HTML2TEXT = html2text

# Main build targets
.PHONY: all configure build dist pre-release release install test ksp clean dist-clean strip-bom

all: build

configure:
	test -d $(KSP_DIR)/KSP_Data
	test -L lib/KSP_Data || ln -s -t lib/ $(KSP_DIR)/KSP_Data

build: configure protobuf $(CSHARP_MAIN_PROJECTS)
	make -C src/kRPC/icons

dist: build
	rm -rf $(DIST_DIR)
	mkdir -p $(DIST_DIR)
	mkdir -p $(DIST_DIR)/GameData/kRPC
	# Plugin files
	cp -r $(CSHARP_MAIN_LIBRARIES) $(DIST_LIBS) $(DIST_ICONS) $(DIST_DIR)/GameData/kRPC/
	# Toolbar
	$(UNZIP) lib/toolbar/Toolbar-1.6.0.zip -d $(DIST_DIR)
	mv $(DIST_DIR)/Toolbar-1.6.0/GameData/* $(DIST_DIR)/GameData/
	rm -r $(DIST_DIR)/Toolbar-1.6.0
	# Python client library
	mkdir -p $(DIST_DIR)/python
	cp -r python/*.py python/*.craft python/schema $(DIST_DIR)/python/
	# Schema
	mkdir -p $(DIST_DIR)/schema
	cp -r $(PROTOS) $(DIST_DIR)/schema/

pre-release: dist test
	# Licenses
	cp LICENSE.txt $(DIST_DIR)/
	cp lib/protobuf-csharp-port-2.4.1.521-release-binaries/license.txt $(DIST_DIR)/protobuf-csharp-port-license.txt
	cp python/protobuf-license.txt $(DIST_DIR)/protobuf-license.txt
	cp python/protobuf-license.txt $(DIST_DIR)/python/protobuf-license.txt
	cp lib/toolbar/LICENSE.txt  $(DIST_DIR)/toolbar-license.txt
	cp LICENSE.txt $(DIST_DIR)/*-license.txt $(DIST_DIR)/GameData/kRPC/
	# README
	$(MARKDOWN) README.md | $(HTML2TEXT) -rcfile tools/html2textrc | sed -e "/Compiling from Source/,//d" > $(DIST_DIR)/README.txt
	cp $(DIST_DIR)/README.txt $(DIST_DIR)/GameData/kRPC/
	# Plugin files
	$(MONODIS) --assembly $(DIST_DIR)/GameData/kRPC/kRPC.dll | grep -m1 Version | sed -n -e 's/^Version:\s*//p' > $(DIST_DIR)/GameData/kRPC/kRPC-version.txt
	$(MONODIS) --assembly $(DIST_DIR)/GameData/kRPC/kRPCServices.dll | grep -m1 Version | sed -n -e 's/^Version:\s*//p' > $(DIST_DIR)/GameData/kRPC/kRPCServices-version.txt
	cd $(DIST_DIR); zip -r krpc-$(VERSION)-pre-`date +"%Y-%m-%d"`.zip ./*

release: dist test
	cd $(DIST_DIR); zip -r krpc-$(VERSION).zip ./*

install: dist
	test -d $(KSP_DIR)/GameData
	rm -rf $(KSP_DIR)/GameData/kRPC
	rm -rf $(KSP_DIR)/GameData/000_Toolbar
	cp -r $(DIST_DIR)/GameData/* $(KSP_DIR)/GameData/

test: test-csharp test-python

test-csharp: $(CSHARP_TEST_PROJECTS)
	$(NUNIT_CONSOLE) -nologo -nothread -trace=Off -output=test.log src/kRPCTest/bin/$(CSHARP_CONFIG)/kRPCTest.dll

test-python:
	make -C python test

ksp: install TestingTools
	cp src/TestingTools/bin/Release/TestingTools.dll $(KSP_DIR)/GameData/
	$(KSP_DIR)/KSP.x86_64 &
	tail -f "$(HOME)/.config/unity3d/Squad/Kerbal Space Program/Player.log"

clean: protobuf-clean
	-rm -f lib/KSP_Data
	make -C src/kRPC/icons clean
	-rm -rf $(CSHARP_BINDIRS) test.log
	find . -name "*.pyc" -exec rm -rf {} \;
	-rm -f KSP.log TestResult.xml

dist-clean: clean
	-rm -rf dist

strip-bom:
	tools/strip-bom.sh

# C# projects
.PHONY: $(CSHARP_PROJECTS) $(CSHARP_LIBRARIES)

.SECONDEXPANSION:
$(CSHARP_PROJECTS): src/$$@/bin/$(CSHARP_CONFIG)/$$@.dll

$(CSHARP_LIBRARIES):
	$(eval $@_PROJECT := $(basename $(notdir $@)))
	$(MDTOOL) build -t:Build -c:$(CSHARP_CONFIG) -p:$($@_PROJECT) src/kRPC.sln

# Protocol Buffers
.PHONY: protobuf protobuf-csharp protobuf-python protobuf-clean protobuf-csharp-clean protobuf-python-clean

protobuf: protobuf-csharp protobuf-python
	# Fix for error in output of C# protobuf compiler
	-patch -p1 --forward --reject-file=- < krpc-proto.patch
	-rm -f src/kRPC/Schema/KRPC.cs.orig

protobuf-csharp: $(PROTOS) $(PROTOS:.proto=.cs)

protobuf-python: $(PROTOS) $(PROTOS:.proto=.py)
	echo "" > python/schema/__init__.py

protobuf-clean: protobuf-csharp-clean protobuf-python-clean
	-rm -rf $(PROTOS:.proto=.protobin)

protobuf-csharp-clean:
	-rm -rf $(PROTOS:.proto=.cs)

protobuf-python-clean:
	-rm -rf $(PROTOS:.proto=.py) python/schema

%.protobin: %.proto
	$(PROTOC) $*.proto -o$*.protobin --include_imports

%.py: %.proto
	$(PROTOC) $< --python_out=.
	mv $*_pb2.py $@
	mkdir -p python/schema
	cp $@ python/schema/$(notdir $@)

%.cs: %.protobin
	$(PROTOGEN) \
		$*.protobin -namespace=KRPC.Schema.$(basename $(notdir $@)) \
		-umbrella_classname=$(basename $(notdir $@)) -output_directory=$(dir $@)
