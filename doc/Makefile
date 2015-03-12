.PHONY: all build serve clean

SPHINX_BUILD = sphinx-build
SPHINX_AUTOBUILD = sphinx-autobuild
SRC_DIR = src
BUILD_DIR = html
SPHINX_OPTS = -d doctrees $(SRC_DIR)

all: build

build:
	$(SPHINX_BUILD) -b html $(SPHINX_OPTS) $(BUILD_DIR)

serve: build
	$(SPHINX_AUTOBUILD) -b html $(SPHINX_OPTS) $(BUILD_DIR)

gh-pages:
	rm -rf ../docs
	$(SPHINX_BUILD) -b html $(SPHINX_OPTS) ../docs

clean:
	rm -rf html doctrees ../docs
