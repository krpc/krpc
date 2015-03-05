ICONS = toolbar-online.png toolbar-offline.png \
        applauncher-online.png applauncher-offline.png \
        button-disconnect-client.png button-close-window.png

INKSCAPE = inkscape

BINDIR = ../bin/icons
BINICONS = $(foreach ICON,$(ICONS),$(BINDIR)/$(ICON))

all: $(BINICONS)

clean:
	-rm -f $(BINICONS)

$(BINICONS): $(BINDIR)/%.png: %.svg
	mkdir -p $(BINDIR)
	$(INKSCAPE) --export-png=$@ $<

.PHONY: all clean
