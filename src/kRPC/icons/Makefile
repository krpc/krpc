ICONS = icon-online.png icon-offline.png button-disconnect-client.png

BINDIR = ../bin
BINICONS = $(foreach ICON,$(ICONS),$(BINDIR)/$(ICON))

all: $(BINICONS)

clean:
	-rm -f $(BINICONS)

$(BINICONS): $(BINDIR)/%.png: %.svg
	mkdir -p $(BINDIR)
	inkscape --export-png=$@ $<

.PHONY: all clean
