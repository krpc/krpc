all: icon-online.png icon-offline.png

%.png: %.svg
	inkscape --export-png=../bin/$@ $< --export-width=24 --export-height=24
