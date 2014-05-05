# Note: This must be an absolute path
KSP_DIR = "$(shell pwd)/../Kerbal Space Program"

SERVER_VERSION = $(shell cat VERSION.txt)
PYTHON_CLIENT_VERSION = $(shell grep "version=" python/setup.py | sed "s/\s*version='\(.*\)',/\1/")

DIST_DIR = dist
DIST_LIBS = \
  lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.dll \
  lib/protobuf-csharp-port-2.4.1.521-release-binaries/Release/cf35/Google.ProtocolBuffers.Serialization.dll
DIST_ICONS = src/kRPC/bin/icons

CSHARP_MAIN_PROJECTS  = kRPC kRPCSpaceCenter
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

PROTOS = $(wildcard src/kRPC/Schema/*.proto) $(wildcard src/kRPCSpaceCenter/Schema/*.proto)
PROTOS_TEST = $(wildcard test/kRPCTest/Schema/*.proto)

PROTOC = protoc
PROTOGEN = tools/ProtoGen.exe
MDTOOL = mdtool
MONODIS = monodis
NUNIT_CONSOLE = nunit-console
UNZIP = unzip
INKSCAPE = inkscape

# Main build targets
.PHONY: all configure logo build cog protobuf dist dist-python pre-release release install test ksp clean dist-clean strip-bom

all: build

configure:
	test -d $(KSP_DIR)/KSP_Data
	mkdir -p lib/KSP_Data
	test -d lib/KSP_Data/Managed || cp -r $(KSP_DIR)/KSP_Data/Managed lib/KSP_Data/

logo:
	-$(INKSCAPE) --export-png=logo.png logo.svg

build: configure protobuf cog $(CSHARP_MAIN_PROJECTS)
	make -C src/kRPC/icons

dist-python:
	make -C python dist

dist: build dist-python
	rm -rf $(DIST_DIR)
	mkdir -p $(DIST_DIR)
	mkdir -p $(DIST_DIR)/GameData/kRPC
	# Plugin files
	cp -r $(CSHARP_MAIN_LIBRARIES) $(DIST_LIBS) $(DIST_ICONS) $(DIST_DIR)/GameData/kRPC/
	# Toolbar
	$(UNZIP) lib/toolbar/Toolbar-1.7.1.zip -d $(DIST_DIR)
	mv $(DIST_DIR)/Toolbar-1.7.1/GameData/* $(DIST_DIR)/GameData/
	rm -r $(DIST_DIR)/Toolbar-1.7.1
	# Licenses
	cp LICENSE.txt $(DIST_DIR)/
	cp lib/protobuf-csharp-port-2.4.1.521-release-binaries/license.txt $(DIST_DIR)/protobuf-csharp-port-license.txt
	cp lib/toolbar/LICENSE.txt  $(DIST_DIR)/toolbar-license.txt
	cp LICENSE.txt $(DIST_DIR)/*-license.txt $(DIST_DIR)/GameData/kRPC/
	# README
	echo "See https://github.com/djungelorm/krpc/wiki" >dist/README.txt
	cp $(DIST_DIR)/README.txt $(DIST_DIR)/GameData/kRPC/
	# Version files
	echo $(SERVER_VERSION) > $(DIST_DIR)/VERSION.txt
	echo $(SERVER_VERSION) > $(DIST_DIR)/GameData/kRPC/VERSION.txt
	# Python client library
	mkdir $(DIST_DIR)/python-client
	cp python/dist/krpc-$(PYTHON_CLIENT_VERSION).zip $(DIST_DIR)/python-client/

pre-release: dist test
	cd $(DIST_DIR); zip -r krpc-$(SERVER_VERSION)-pre-`date +"%Y-%m-%d"`.zip ./*

release: dist test
	cd $(DIST_DIR); zip -r krpc-$(SERVER_VERSION).zip ./*

install: dist
	test -d $(KSP_DIR)/GameData
	rm -rf $(KSP_DIR)/GameData/kRPC
	rm -rf $(KSP_DIR)/GameData/000_Toolbar
	cp -r $(DIST_DIR)/GameData/* $(KSP_DIR)/GameData/

test: test-csharp test-python test-spacecenter

test-csharp: $(CSHARP_TEST_PROJECTS)
	$(NUNIT_CONSOLE) -nologo -nothread -trace=Off -output=test.log test/kRPCTest/bin/$(CSHARP_CONFIG)/kRPCTest.dll

test-python:
	make -C python test

test-spacecenter:
	make -C test/kRPCSpaceCenterTest KSP_DIR=$(KSP_DIR) test

ksp: install TestingTools
	cp test/TestingTools/bin/Release/TestingTools.dll $(KSP_DIR)/GameData/
	-cp settings.cfg $(KSP_DIR)/GameData/kRPC/settings.cfg
	test "!" -f $(KSP_DIR)/KSP.x86_64 || optirun $(KSP_DIR)/KSP.x86_64 &
	test "!" -f $(KSP_DIR)/KSP.exe || $(KSP_DIR)/KSP.exe &
	test "!" -d $(HOME)/.config/unity3d || tail -f "$(HOME)/.config/unity3d/Squad/Kerbal Space Program/Player.log"
	test "!" -f $(KSP_DIR)/KSP_Data/output_log.txt || tail -f $(KSP_DIR)/KSP_Data/output_log.txt

clean: protobuf-clean
	-rm -f logo.png
	-rm -rf lib/KSP_Data
	make -C src/kRPC/icons clean
	-rm -rf $(CSHARP_BINDIRS) test.log
	find . -name "*.pyc" -exec rm -rf {} \;
	-rm -f KSP.log TestResult.xml
	make -C python clean

dist-clean: clean
	-rm -rf dist

strip-bom:
	tools/strip-bom.sh

# C# projects
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

# Cog
cog:
	-python -m cogapp -D nargs=10 -r src/kRPC/Continuations/ParameterizedContinuation.cs

# Protocol Buffers
.PHONY: protobuf-csharp protobuf-python protobuf-clean protobuf-csharp-clean protobuf-python-clean

protobuf: protobuf-csharp protobuf-python
	# Fix for error in output of C# protobuf compiler
	-dos2unix src/kRPC/Schema/KRPC.cs
	-patch -p1 --forward --reject-file=- < krpc-proto.patch
	-rm -f src/kRPC/Schema/KRPC.cs.orig

protobuf-csharp: $(PROTOS) $(PROTOS_TEST) $(PROTOS:.proto=.cs) $(PROTOS_TEST:.proto=.cs)

protobuf-python: $(PROTOS) $(PROTOS_TEST) $(PROTOS:.proto=.py) $(PROTOS_TEST:.proto=.py)
	echo "" > python/krpc/schema/__init__.py
	test -f python/krpc/test/Test.py || mv python/krpc/schema/Test.py python/krpc/test/Test.py

protobuf-clean: protobuf-csharp-clean protobuf-python-clean
	-rm -rf $(PROTOS:.proto=.protobin) $(PROTOS_TEST:.proto=.protobin)

protobuf-csharp-clean:
	-rm -rf $(PROTOS:.proto=.cs) $(PROTOS_TEST:.proto=.cs)

protobuf-python-clean:
	-rm -rf $(PROTOS:.proto=.py) $(PROTOS_TEST:.proto=.py) python/krpc/schema python/krpc/test/Test.py

%.protobin: %.proto
	$(PROTOC) $*.proto -o$*.protobin --include_imports

%.py: %.proto
	$(PROTOC) $< --python_out=.
	mv $*_pb2.py $@
	mkdir -p python/krpc/schema
	cp $@ python/krpc/schema/$(notdir $@)

%.cs: %.protobin
	$(PROTOGEN) \
		$*.protobin -namespace=KRPC.Schema.$(basename $(notdir $@)) \
		-umbrella_classname=$(basename $(notdir $@)) -output_directory=$(dir $@)
