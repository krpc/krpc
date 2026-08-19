"""Draws an SVG image into a PNG.

  rasterize.py --out PNG SVG

Rendered by libvips, which draws SVG through librsvg and arrives as a wheel
carrying both, so there is no system image library in the build. The image comes
out at the size the SVG asks for, which is the size the icons and the diagrams
are drawn at.
"""

import argparse
import os
import sys
import tempfile


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    parser.add_argument("svg")
    options = parser.parse_args()

    # Fontconfig, which librsvg goes through to draw the text in a diagram,
    # keeps an index of the fonts it found under the user's cache directory. A
    # build action has no home to write one in, and fontconfig says so on stderr
    # once per image unless it is given somewhere. Point it at a directory that
    # goes away with the process: an index rebuilt per image costs little at
    # this many images, and nothing is left behind for the next build to find.
    with tempfile.TemporaryDirectory() as cache:
        os.environ["XDG_CACHE_HOME"] = cache

        # Imported here rather than at the top of the file so that libvips, and
        # the fontconfig it loads, start up with the cache directory set.
        import pyvips  # pylint: disable=import-outside-toplevel

        pyvips.Image.new_from_file(options.svg).write_to_file(options.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
