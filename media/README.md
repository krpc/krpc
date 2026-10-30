# Media

The kRPC logos and banners. `generate.py` writes them into `build/`, from the game screenshots
`background-ksp.png` and `background-ksa.png`, cropped to the banner size of 1920x560.

The generated files, each as an SVG and a PNG:

 * `build/logo-mark` - the tile, for icons and avatars.
 * `build/logo-on-light`, `build/logo-on-dark` - the logo, for light and dark backgrounds.
 * `build/logo-small-on-light`, `build/logo-small-on-dark` - the logo with a thicker orbit, for
   small sizes.
 * `build/banner-ksp`, `build/banner-ksa` - the forum banners.
 * `build/spacedock-ksp`, `build/spacedock-ksa` - the SpaceDock headers, at 8600x400. SpaceDock shows
   the header at its own size from the left, cropped to the window width, so the headers are the
   banner layout at 400 pixels high, with the screenshot continuing to the right. SpaceDock makes
   the listing thumbnail from the center of the header, which holds the logo, rocket and code.

`build/` is not committed. The script also copies `logo-mark.svg`, `logo-on-light.svg` and
`logo-on-dark.svg` into this directory, where they are committed for the README and the
documentation.

`generate.sh` installs the Python dependencies into a virtualenv and runs `generate.py`. The script
downloads the fonts into `build/fonts`, as Nasalization's license does not allow redistributing
it, and needs `rsvg-convert` for the PNGs:

```bash
media/generate.sh --preview /tmp/preview.png
```

The preview shows every logo on light, dark and mid backgrounds, from large down to 16 pixels high.
