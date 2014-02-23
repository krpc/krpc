ICONS = icon-online.png icon-offline.png

BINDIR = ../bin
BINICONS = $(foreach ICON,$(ICONS),$(BINDIR)/$(ICON))

all: $(BINICONS)

clean:
	-rm -f $(BINICONS)

$(BINICONS): $(BINDIR)/%.png: %.svg
	mkdir -p $(BINDIR)
	inkscape --export-png=$@ $< --export-width=24 --export-height=24

.PHONY: all clean
