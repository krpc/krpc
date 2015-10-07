SRC_DIR = src
API_DIR = api
BUILD_DIR = build
IMAGE_DIR = src/images
PYTHON_API_DIR = src/python/api
CPP_API_DIR = src/cpp/api
LUA_API_DIR = src/lua/api
GH_PAGES_DIR = ../docs

CSHARP_CONFIG=Release

SPHINX_BUILD = sphinx-build
SPHINX_AUTOBUILD = sphinx-autobuild
SPHINX_OPTS = -W -d $(BUILD_DIR)/doctrees $(SRC_DIR)

INKSCAPE = inkscape
INKSCAPE_FLAGS = -C -d=90 --export-background-opacity=\#00

# Main targets -----------------------------------------------------------------

.PHONY: all build clean

all: build

build: spelling html pdf

clean: clean-images
	rm -rf $(BUILD_DIR) $(PYTHON_API_DIR) $(CPP_API_DIR) $(LUA_API_DIR) $(GH_PAGES_DIR)

# Build specific types of doc---------------------------------------------------

.PHONY: spelling html pdf gh-pages

spelling: images api
	$(SPHINX_BUILD) -b spelling $(SPHINX_OPTS) $(BUILD_DIR)/spelling

html: images api
	$(SPHINX_BUILD) -b html $(SPHINX_OPTS) $(BUILD_DIR)/html

pdf: images api
	$(SPHINX_BUILD) -b latex $(SPHINX_OPTS) $(BUILD_DIR)/pdf
	make -C $(BUILD_DIR)/pdf

gh-pages: images api
	rm -rf $(GH_PAGES_DIR)
	$(SPHINX_BUILD) -b html $(SPHINX_OPTS) $(GH_PAGES_DIR)

# Images -----------------------------------------------------------------------

.PHONY: images clean-images

PNGS = $(foreach dir, $(IMAGE_DIR), $(patsubst %.svg,%.png,$(wildcard $(IMAGE_DIR)/**/*.svg)))

images: $(PNGS)

clean-images:
	rm -f $(PNGS)
	rm -f lib/*.pyc

%.png : %.svg
	$(INKSCAPE) $(INKSCAPE_FLAGS) -f $< -e $@

# API docs ---------------------------------------------------------------------

.PHONY: api python-api cpp-api lua-api

api: python-api cpp-api lua-api

python-api:
	PYTHONPATH=../python ./generate python $(API_DIR) $(PYTHON_API_DIR) ../src/*/bin/$(CSHARP_CONFIG)/*.json

cpp-api:
	PYTHONPATH=../python ./generate cpp $(API_DIR) $(CPP_API_DIR) ../src/*/bin/$(CSHARP_CONFIG)/*.json

lua-api:
	PYTHONPATH=../python ./generate lua $(API_DIR) $(LUA_API_DIR) ../src/*/bin/$(CSHARP_CONFIG)/*.json

# Sphinx server on localhost ---------------------------------------------------

.PHONY: serve wait-api-changed sphinx-autobuild

serve: html
	./serve

wait-api-changed:
	inotifywait -r -e modify,move,create,delete $(API_DIR)

sphinx-autobuild:
	$(SPHINX_AUTOBUILD) -b html $(SPHINX_OPTS) $(BUILD_DIR)/html
