ifndef KSP_DIR
$(error KSP_DIR is not set. See https://github.com/djungelorm/krpc/wiki/Compiling for details)
endif
KSP_DIR := $(shell readlink -f "$(KSP_DIR)")

SERVER_VERSION = $(shell cat VERSION.txt)
PYTHON_CLIENT_VERSION = $(shell grep "version=" python/setup.py | sed "s/\s*version='\(.*\)',/\1/")

DIST_DIR = dist
DIST_LIBS = \
  lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.dll \
  lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.Serialization.dll
DIST_ICONS = src/kRPC/bin/icons

CSHARP_MAIN_PROJECTS  = kRPC kRPCSpaceCenter kRPCInfernalRobotics kRPCKerbalAlarmClock
CSHARP_TEST_PROJECTS  = kRPCTest TestServer
CSHARP_TEST_UTILS_PROJECTS = TestingTools
CSHARP_CONFIG = Release

CSHARP_PROJECTS  = $(CSHARP_MAIN_PROJECTS) $(CSHARP_TEST_PROJECTS) $(CSHARP_TEST_UTILS_PROJECTS)
CSHARP_PROJECT_DIRS = $(foreach PROJECT,$(CSHARP_MAIN_PROJECTS),src/$(PROJECT)) \
                      $(foreach PROJECT,$(CSHARP_TEST_PROJECTS),test/$(PROJECT)) \
                      $(foreach PROJECT,$(CSHARP_TEST_UTILS_PROJECTS),test/$(PROJECT))
CSHARP_BINDIRS = $(foreach DIR,$(CSHARP_PROJECT_DIRS),$(DIR)/bin) \
                 $(foreach DIR,$(CSHARP_PROJECT_DIRS),$(DIR)/obj)
CSHARP_MAIN_LIBRARIES = $(foreach PROJECT,$(CSHARP_MAIN_PROJECTS),src/$(PROJECT)/bin/$(CSHARP_CONFIG)/$(PROJECT).dll)
CSHARP_LIBRARIES = $(foreach PROJECT,$(CSHARP_MAIN_PROJECTS),src/$(PROJECT)/bin/$(CSHARP_CONFIG)/$(PROJECT).dll) \
                   $(foreach PROJECT,$(CSHARP_TEST_PROJECTS),test/$(PROJECT)/bin/$(CSHARP_CONFIG)/$(PROJECT).dll) \
                   $(foreach PROJECT,$(CSHARP_TEST_UTILS_PROJECTS),test/$(PROJECT)/bin/$(CSHARP_CONFIG)/$(PROJECT).dll)
CSHARP_DOCS = $(foreach PROJECT,$(CSHARP_MAIN_PROJECTS),src/$(PROJECT)/bin/$(CSHARP_CONFIG)/$(PROJECT).xml)

PROTOS = $(wildcard src/kRPC/Schema/*.proto) $(wildcard src/kRPCSpaceCenter/Schema/*.proto)
PROTOS_TEST = $(wildcard test/kRPCTest/Schema/*.proto)

PROTOC = protoc
PROTOGEN = mono tools/ProtoGen.exe
MDTOOL = mdtool
MONODIS = monodis
NUNIT_CONSOLE = nunit-console
INKSCAPE = inkscape

.PHONY: all configure build dist install release clean dist-clean \
        test dist-python doc gh-pages cog protobuf icons logo ksp strip-bom

# Main targets -----------------------------------------------------------------

all: build

configure:
	@test -d "$(KSP_DIR)" || (echo "KSP_DIR directory does not exist: $(KSP_DIR)"; exit 1)
	test -d "$(KSP_DIR)/KSP_Data"
	-rm -rf lib/KSP_Data
	mkdir -p lib/KSP_Data
	cp -r "$(KSP_DIR)/KSP_Data/Managed" lib/KSP_Data/

build: configure protobuf cog icons $(CSHARP_MAIN_PROJECTS)

dist: build doc dist-python
	rm -rf $(DIST_DIR)
	mkdir -p $(DIST_DIR)
	mkdir -p $(DIST_DIR)/GameData/kRPC
	# Plugin files
	cp -r $(CSHARP_MAIN_LIBRARIES) $(CSHARP_DOCS) $(DIST_LIBS) $(DIST_ICONS) $(DIST_DIR)/GameData/kRPC/
	# Licenses
	cp LICENSE.txt $(DIST_DIR)/
	cp lib/protobuf-csharp-port-2.4.1.521-release-binaries/license.txt $(DIST_DIR)/protobuf-csharp-port-license.txt
	cp LICENSE.txt $(DIST_DIR)/*-license.txt $(DIST_DIR)/GameData/kRPC/
	# README
	echo "See https://github.com/djungelorm/krpc/wiki" >dist/README.txt
	cp $(DIST_DIR)/README.txt $(DIST_DIR)/GameData/kRPC/
	# CHANGES
	cp CHANGES.txt $(DIST_DIR)/
	# Version files
	echo $(SERVER_VERSION) > $(DIST_DIR)/VERSION.txt
	echo $(SERVER_VERSION) > $(DIST_DIR)/GameData/kRPC/VERSION.txt
	# Python client library
	mkdir $(DIST_DIR)/python-client
	cp python/dist/krpc-$(PYTHON_CLIENT_VERSION).zip $(DIST_DIR)/python-client/
	# protobuf source
	mkdir $(DIST_DIR)/schema
	echo "See http://djungelorm.github.io/krpc/docs/communication-protocol.html" > $(DIST_DIR)/schema/README.txt
	cp src/kRPC/Schema/KRPC.proto $(DIST_DIR)/schema/
	mkdir -p $(DIST_DIR)/schema/python
	cp -R python/krpc/schema/KRPC.py $(DIST_DIR)/schema/python/
	cp -R java cpp $(DIST_DIR)/schema/
	# Documentation
	cp doc/build/pdf/kRPC.pdf $(DIST_DIR)/

install: dist
	test -d "$(KSP_DIR)/GameData"
	rm -rf "$(KSP_DIR)/GameData/kRPC"
	cp -r $(DIST_DIR)/GameData/* "$(KSP_DIR)/GameData/"

release: dist test
	cd $(DIST_DIR); zip -r krpc-$(SERVER_VERSION).zip ./*

clean: protobuf-clean
	-rm -f logo.png
	-rm -rf lib/KSP_Data
	make -C src/kRPC/icons clean
	-rm -rf $(CSHARP_BINDIRS) test.log
	find . -name "*.pyc" -exec rm -rf {} \;
	-rm -f KSP.log TestResult.xml
	make -C python clean
	make -C lua clean
	make -C doc clean

dist-clean: clean
	-rm -rf dist

# Tests ------------------------------------------------------------------------

test: test-csharp test-python test-spacecenter

test-csharp: $(CSHARP_TEST_PROJECTS)
	$(NUNIT_CONSOLE) -nologo -nothread -trace=Off -output=test.log test/kRPCTest/bin/$(CSHARP_CONFIG)/kRPCTest.dll

test-python:
	make -C python test

test-spacecenter:
	make -C test/kRPCSpaceCenterTest KSP_DIR="$(KSP_DIR)" test

# C# projects ------------------------------------------------------------------

.PHONY: $(CSHARP_PROJECTS) $(CSHARP_LIBRARIES)

.SECONDEXPANSION:
$(CSHARP_MAIN_PROJECTS): src/$$@/bin/$(CSHARP_CONFIG)/$$@.dll

.SECONDEXPANSION:
$(CSHARP_TEST_PROJECTS): test/$$@/bin/$(CSHARP_CONFIG)/$$@.dll

.SECONDEXPANSION:
$(CSHARP_TEST_UTILS_PROJECTS): test/$$@/bin/$(CSHARP_CONFIG)/$$@.dll

$(CSHARP_LIBRARIES):
	$(eval $@_PROJECT := $(basename $(notdir $@)))
	$(MDTOOL) build -t:Build -c:$(CSHARP_CONFIG) -p:$($@_PROJECT) kRPC.sln

# Python -----------------------------------------------------------------------

dist-python:
	make -C python dist

# Documentation ----------------------------------------------------------------

doc:
	make -C doc build

gh-pages:
	make -C doc gh-pages
	tools/strip-bom.sh

# Cog --------------------------------------------------------------------------

cog:
	-python -m cogapp -D nargs=10 -r src/kRPC/Continuations/ParameterizedContinuation.cs
	-python -m cogapp -D nargs=8 -r src/kRPC/Utils/Tuple.cs

# Protocol Buffers -------------------------------------------------------------

.PHONY: protobuf-csharp protobuf-python protobuf-java protobuf-cpp protobuf-lua \
	      protobuf-clean protobuf-csharp-clean protobuf-python-clean protobuf-java-clean \
        protobuf-cpp-clean protobuf-lua-clean

protobuf: protobuf-csharp protobuf-python protobuf-java protobuf-cpp protobuf-lua
	# Fix for error in output of C# protobuf compiler
	-git apply krpc-proto.patch

protobuf-csharp: $(PROTOS) $(PROTOS_TEST) $(PROTOS:.proto=.cs) $(PROTOS_TEST:.proto=.cs)

protobuf-python: $(PROTOS) $(PROTOS_TEST) $(PROTOS:.proto=.py) $(PROTOS_TEST:.proto=.py)
	mkdir -p python/krpc/schema
	echo "" > python/krpc/schema/__init__.py
	cp $(PROTOS:.proto=.py) python/krpc/schema/
	cp $(PROTOS_TEST:.proto=.py) python/krpc/test/

protobuf-java: $(PROTOS) $(PROTOS:.proto=.java)
	mkdir -p java/krpc
	cp $(PROTOS:.proto=.java) java/krpc/

protobuf-cpp: $(PROTOS) $(PROTOS:.proto=.pb.h) $(PROTOS:.proto=.pb.cc)
	mkdir -p cpp/src/kRPC/Schema
	cp $(PROTOS:.proto=.pb.h) $(PROTOS:.proto=.pb.cc) cpp/src/kRPC/Schema/

protobuf-lua: $(PROTOS) $(PROTOS_TEST) $(PROTOS:.proto=.lua) $(PROTOS_TEST:.proto=.lua)
	mkdir -p lua/krpc/schema
	cp $(PROTOS:.proto=.lua) lua/krpc/schema/
	cp $(PROTOS_TEST:.proto=.lua) lua/krpc/test/

protobuf-clean: protobuf-csharp-clean protobuf-python-clean protobuf-java-clean protobuf-cpp-clean protobuf-lua-clean
	rm -rf $(PROTOS:.proto=.protobin) $(PROTOS_TEST:.proto=.protobin)

protobuf-csharp-clean:
	rm -rf $(PROTOS:.proto=.cs) $(PROTOS_TEST:.proto=.cs)

protobuf-python-clean:
	rm -rf $(PROTOS:.proto=.py) $(PROTOS_TEST:.proto=.py) python/krpc/schema python/krpc/test/Test.py

protobuf-java-clean:
	rm -rf java
	rm -rf $(PROTOS:.proto=.java)

protobuf-cpp-clean:
	rm -rf cpp
	rm -rf $(PROTOS:.proto=.pb.h) $(PROTOS:.proto=.pb.cc)

protobuf-lua-clean:
	rm -rf $(PROTOS:.proto=.lua) $(PROTOS_TEST:.proto=.lua) lua/krpc/schema lua/krpc/test/Test.lua

%.protobin: %.proto
	$(PROTOC) $*.proto -o$*.protobin --include_imports

%.cs: %.protobin
	$(PROTOGEN) \
		$*.protobin -namespace=KRPC.Schema.$(basename $(notdir $@)) \
		-umbrella_classname=$(basename $(notdir $@)) -output_directory=$(dir $@)

%.py: %.proto
	$(PROTOC) $< --python_out=.
	mv $*_pb2.py $@

JAVATMP:=$(shell mktemp -d)

%.java: %.proto
	$(PROTOC) $< --java_out=$(JAVATMP)
	# Following is an ugly hack
	mv $(JAVATMP)/krpc/KRPC.java $@

%.pb.h: %.proto
	$(PROTOC) $< --cpp_out=.

%.pb.cc: %.proto
	$(PROTOC) $< --cpp_out=.

%.lua: %.proto
	$(PROTOC) $< --lua_out=.
	mv $*_pb.lua $@

# Images -----------------------------------------------------------------------

icons:
	make -C src/kRPC/icons

logo:
	-$(INKSCAPE) --export-png=logo.png media/logo.svg

# Tools / Other ----------------------------------------------------------------

ksp: build TestingTools
	test -d "$(KSP_DIR)/GameData"
	rm -rf "$(KSP_DIR)/GameData/kRPC"
	mkdir "$(KSP_DIR)/GameData/kRPC"
	cp -r $(CSHARP_MAIN_LIBRARIES) $(CSHARP_DOCS) $(DIST_LIBS) $(DIST_ICONS) $(KSP_DIR)/GameData/kRPC/
	cp test/TestingTools/bin/$(CSHARP_CONFIG)/TestingTools.dll "$(KSP_DIR)/GameData/kRPC/"
	cp test/TestingTools/bin/$(CSHARP_CONFIG)/TestingTools.xml "$(KSP_DIR)/GameData/kRPC/"
	-cp settings.cfg "$(KSP_DIR)/GameData/kRPC/settings.cfg"
	test "!" -f "$(KSP_DIR)/KSP.x86_64" || "$(KSP_DIR)/KSP.x86_64" &
	test "!" -f "$(KSP_DIR)/KSP.exe" || "$(KSP_DIR)/KSP.exe" &
	test "!" -d "$(HOME)/.config/unity3d" || tail -f "$(HOME)/.config/unity3d/Squad/Kerbal Space Program/Player.log"
	test "!" -f "$(KSP_DIR)/KSP_Data/output_log.txt" || tail -f "$(KSP_DIR)/KSP_Data/output_log.txt"

strip-bom:
	tools/strip-bom.sh
