"""PNG and JPEG images: checking the ones an RPC returns, and writing a PNG to send."""

import struct
import zlib
from collections.abc import Callable, Sequence

Color = Sequence[int]

PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
JPEG_SIGNATURE = b"\xff\xd8\xff"


def png_size(data):
    """The width and height of a PNG image, from its header."""
    return struct.unpack(">II", data[16:24])


def jpeg_size(data):
    """The width and height of a JPEG image, from its start of frame segment."""
    pos = 2
    while pos + 4 <= len(data):
        marker, length = struct.unpack(">2sH", data[pos : pos + 4])
        # Baseline and progressive
        if marker in (b"\xff\xc0", b"\xff\xc2"):
            height, width = struct.unpack(">HH", data[pos + 5 : pos + 9])
            return width, height
        pos += 2 + length
    raise ValueError("JPEG image has no start of frame segment")


def assert_png(data, size=None):
    """Check that data is a PNG image, of the given width and height if given."""
    if data[:8] != PNG_SIGNATURE:
        raise AssertionError("Not a PNG image, starts %r" % data[:8])
    if size is not None and png_size(data) != tuple(size):
        raise AssertionError(
            "PNG image is %dx%d, expected %dx%d" % (png_size(data) + tuple(size))
        )


def assert_jpeg(data, size=None):
    """Check that data is a JPEG image, of the given width and height if given."""
    if data[:3] != JPEG_SIGNATURE:
        raise AssertionError("Not a JPEG image, starts %r" % data[:3])
    if size is not None and jpeg_size(data) != tuple(size):
        raise AssertionError(
            "JPEG image is %dx%d, expected %dx%d" % (jpeg_size(data) + tuple(size))
        )


def png(width: int, height: int, color: Color | Callable[[int, int], Color]):
    """A PNG image. color is an RGB or RGBA tuple of bytes, or a function of the pixel's x
    and y, from the top left, that returns one."""

    def pixel(x: int, y: int) -> Color:
        return color(x, y) if callable(color) else color

    channels = len(pixel(0, 0))
    rows = b"".join(
        b"\x00" + b"".join(bytes(pixel(x, y)) for x in range(width))
        for y in range(height)
    )

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body))

    # Color type 2 is RGB and 6 is RGBA, at 8 bits per channel
    color_type = 2 if channels == 3 else 6
    header = struct.pack(">IIBBBBB", width, height, 8, color_type, 0, 0, 0)
    return (
        PNG_SIGNATURE
        + chunk(b"IHDR", header)
        + chunk(b"IDAT", zlib.compress(rows))
        + chunk(b"IEND", b"")
    )
