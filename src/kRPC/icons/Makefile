ICONS = toolbar-online.png toolbar-offline.png \
        applauncher-online.png applauncher-offline.png \
        button-disconnect-client.png button-close-window.png

INKSCAPE = inkscape

BUILD_DIR = ../bin/icons
BUILD_ICONS = $(foreach ICON,$(ICONS),$(BUILD_DIR)/$(ICON))

.PHONY: all clean

all: $(BUILD_ICONS)

clean:
	-rm -f $(BUILD_ICONS)

$(BUILD_ICONS): $(BUILD_DIR)/%.png: %.svg
	mkdir -p $(BUILD_DIR)
	$(INKSCAPE) --export-png=$@ $<
