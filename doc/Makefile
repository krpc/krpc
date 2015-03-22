SRC_DIR = src
BUILD_DIR = build
IMAGE_DIR = src/images

PNGS = $(foreach dir, $(IMAGE_DIR), $(patsubst %.svg,%.png,$(wildcard $(IMAGE_DIR)/**/*.svg)))

SPHINX_BUILD = sphinx-build
SPHINX_AUTOBUILD = sphinx-autobuild
SPHINX_OPTS = -d $(BUILD_DIR)/doctrees $(SRC_DIR)

INKSCAPE = inkscape
INKSCAPE_FLAGS = -C -d=90 --export-background-opacity=\#00

.PHONY: all build serve gh-pages clean html pdf python-api

all: build

build: html pdf

#TODO: doesn't watch changes in api/
serve: html
	$(SPHINX_AUTOBUILD) -b html $(SPHINX_OPTS) $(BUILD_DIR)/html

gh-pages: python-api
	rm -rf ../docs
	$(SPHINX_BUILD) -b html $(SPHINX_OPTS) ../docs

clean:
	rm -rf $(BUILD_DIR) ../docs src/python-api $(PNGS)

html: images python-api
	$(SPHINX_BUILD) -b html $(SPHINX_OPTS) $(BUILD_DIR)/html

pdf: images python-api
	$(SPHINX_BUILD) -b latex $(SPHINX_OPTS) $(BUILD_DIR)/pdf
	make -C $(BUILD_DIR)/pdf

%.png : %.svg
	$(INKSCAPE) $(INKSCAPE_FLAGS) -f $< -e $@

images: $(PNGS)

python-api:
	rm -rf src/python-api
	./build-api.py api src/python-api
