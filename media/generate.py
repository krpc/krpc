#!/usr/bin/env python3
"""Generates the kRPC logos and banners into the build directory.

Run with generate.sh, which installs the dependencies in requirements.txt
into a virtualenv. The fonts are downloaded into build/fonts, as
Nasalization's license does not allow redistributing it. All text is written
as outlines, so the SVGs need no fonts to display. The PNGs are rendered with
rsvg-convert.
"""

import argparse
import base64
import hashlib
import io
import math
import shutil
import struct
import subprocess
import urllib.request
import zipfile
from pathlib import Path

import uharfbuzz as hb
from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer
from pygments.lexers import PythonLexer
from pygments.token import Token

MEDIA = Path(__file__).resolve().parent
BUILD = MEDIA / "build"
PUBLISHED = ["logo-mark.svg", "logo-on-light.svg", "logo-on-dark.svg"]
# The icon for mod listings that take only a raster image, such as Borea
PUBLISHED_PNG = ["logo-mark.png", "logo-square.png"]

# Fonts: (file name, URL, member of a zip archive or None, sha256 of the font file)
FONTS = {
    "nasalization": (
        "Nasalization Rg.otf",
        "https://dl.dafont.com/dl/?f=nasalization",
        "Nasalization Rg.otf",
        "aefedadd5c49e84537ba3add3c487e1e8c2d1e27b4dbf4fefecd19000374a9d1",
    ),
    "inter": (
        "Inter[opsz,wght].ttf",
        "https://raw.githubusercontent.com/google/fonts/"
        "e1d6480102fed30739fead0faee463101f892c8f/ofl/inter/Inter%5Bopsz,wght%5D.ttf",
        None,
        "29160a80ff49ddcab2c97711247e08b1fab27a484a329ce8b813d820dc559031",
    ),
    "dejavu-mono": (
        "DejaVuSansMono.ttf",
        "https://github.com/dejavu-fonts/dejavu-fonts/releases/download/"
        "version_2_37/dejavu-fonts-ttf-2.37.zip",
        "dejavu-fonts-ttf-2.37/ttf/DejaVuSansMono.ttf",
        "b4a6c3e4faab8773f4ff761d56451646409f29abedd68f05d38c2df667d3c582",
    ),
}

# Colors
ACCENT = "#ff7a2f"
TILE_TOP = "#1d2d4d"
TILE_BOTTOM = "#0b1322"
INK = {"on-light": "#13203a", "on-dark": "#dfe4ec"}

# The k in the tile: Inter ExtraBold at 80px on baseline 80, with its bounds
K = (
    "M 4.4921875,80 H 18.125 V 66.757812 l 2.890625,-3.4375 L 31.640625,80 "
    "H 47.382812 L 31.171875,55.390625 46.757812,36.328125 H 31.25 "
    "l -12.5,15.78125 h -0.625 v -30.3125 H 4.4921875 Z"
)
K_LEFT, K_RIGHT, K_TOP = 4.49, 47.38, 21.8
# The notch between the k's stem and arm
K_NOTCH = "M 18.125,36.33 H 31.25 L 18.125,52.1 Z"
K_SCALE = 0.6
K_CENTER_Y = 33

# The orbit, an ellipse on the 64x64 tile. Each weight sets its width, the
# clearance between the orbit and the k, and the ellipse center and minor radius.
ORBIT_ANGLE = -28
ORBIT_CX = 32
ORBIT_RX = 26
# The left side's width as a fraction of the top and bottom width
ORBIT_TAPER = 0.5
# The orbit passes in front of the k's leg, which lies right of this x
ORBIT_FRONT_X = 29.5
LARGE = {"w": 2.4, "gap": 1.4, "cy": 36.2, "ry": 6}
SMALL = {"w": 4.0, "gap": 1.1, "cy": 35.8, "ry": 5.5}

# The wordmark: RPC in Nasalization, at the cap height of the tile's k
CAP = 80 - K_TOP
# The R and P stems, restored from the top bar down to a gap above the middle
# bar, in font units at 80px. The gap is a fraction of the stroke width.
STEM_LEFT, STEM_RIGHT, STEM_WIDTH = 6.4, 15.92, 9.44
TOP_BAR_BOTTOM, MIDDLE_BAR_TOP = 35.44, 50.08
STEM_GAP = 0.5
CORNER_RADIUS = 8.5
# Lockup: the tile scale, the gap before RPC, and RPC's scale
TILE_SCALE = 0.8
TILE_SIZE = 64 * TILE_SCALE
WORD_GAP = 6
WORD_SCALE = 0.62

# The forum banner, and the SpaceDock header. SpaceDock shows the header at its
# own pixel size from the top left, cropped to 400 pixels high and the window
# width. The header is the banner's layout, scaled to that height, with the
# screenshot continuing to the right and fading to black for wide windows.
# SpaceDock makes the listing thumbnail from the center of the header, scaled
# to the thumbnail's height, so the center holds a scene of its own: the logo,
# rocket and code, past the width of a full screen 4K window.
BANNER_W, BANNER_H = 1920, 560
SPACEDOCK_W, SPACEDOCK_H = 8600, 400
SPACEDOCK_SCALE = 0.65
THUMBNAIL_W, THUMBNAIL_H = 640, 390
# The thumbnail scene in the background, wider than the thumbnail to fade at its edges
THUMBNAIL_SCENE_W = 800
THUMBNAIL_SCENE_X = (SPACEDOCK_W - THUMBNAIL_SCENE_W) // 2
# The center of the rocket in each banner screenshot, which the headers place
# in the layout's gap and in the thumbnail scene
ROCKETS = {"ksp": (929, 269), "ksa": (956, 260)}
# Each thumbnail scene's scale of its screenshot, and how far it lowers it
THUMBNAIL_SCENES = {"ksp": (0.68, 76), "ksa": (0.85, 30)}
GAMES = list(ROCKETS)
BANNER_CODE = """import krpc
conn = krpc.connect(name='Launch')
vessel = conn.space_center.active_vessel

vessel.auto_pilot.engage()
vessel.auto_pilot.target_pitch_and_heading(90, 90)
vessel.control.throttle = 1.0
vessel.control.activate_next_stage()

flight = vessel.flight()
altitude = conn.add_stream(getattr, flight, 'mean_altitude')
while altitude() < 10000:
    pass
vessel.auto_pilot.target_pitch_and_heading(60, 90)"""
CODE_COLORS = {
    Token.Keyword: "#c792ea",
    Token.Name.Builtin: "#82aaff",
    Token.Literal.String: "#c3e88d",
    Token.Literal.Number: "#f78c6c",
    Token.Operator: "#89ddff",
    Token.Name.Namespace: "#ffcb6b",
    Token.Comment: "#697098",
}
CODE_DEFAULT = "#d6deeb"


def fetch_fonts(cache):
    """Downloads the fonts into the cache, checking each against its hash."""
    cache.mkdir(parents=True, exist_ok=True)
    paths = {}
    for key, (name, url, member, sha256) in FONTS.items():
        path = cache / name
        if not path.exists():
            print("downloading", url)
            request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
            with urllib.request.urlopen(request) as response:
                data = response.read()
            if member:
                with zipfile.ZipFile(io.BytesIO(data)) as archive:
                    data = archive.read(member)
            path.write_bytes(data)
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        if digest != sha256:
            raise RuntimeError(f"{path} has sha256 {digest}, expected {sha256}")
        paths[key] = path
    return paths


class Font:
    """A font, with outlines and shaping, at the given variation."""

    def __init__(self, path, **variation):
        self.ttfont = TTFont(path)
        if variation:
            self.ttfont = instancer.instantiateVariableFont(self.ttfont, variation)
        self.glyphs = self.ttfont.getGlyphSet()
        self.order = self.ttfont.getGlyphOrder()
        self.cmap = self.ttfont.getBestCmap()
        self.upm = self.ttfont["head"].unitsPerEm
        face = hb.Face(Path(path).read_bytes())
        self.hb_font = hb.Font(face)
        if variation:
            self.hb_font.set_variations(variation)

    def advance(self, char, size):
        return self.glyphs[self.cmap[ord(char)]].width * size / self.upm

    def path(self, text, size, x=0.0, baseline=0.0, shape=True):
        """The outline of text as SVG path data, and its bounds, with y down."""
        if shape:
            buf = hb.Buffer()
            buf.add_str(text)
            buf.guess_segment_properties()
            hb.shape(self.hb_font, buf, {"kern": True, "liga": True})
            run = [
                (self.order[info.codepoint], pos.x_advance, pos.x_offset)
                for info, pos in zip(buf.glyph_infos, buf.glyph_positions)
            ]
        else:
            run = [
                (self.cmap[ord(c)], self.glyphs[self.cmap[ord(c)]].width, 0)
                for c in text
            ]
        s = size / self.upm
        svg = SVGPathPen(self.glyphs)
        bounds = BoundsPen(self.glyphs)
        for name, x_advance, x_offset in run:
            transform = (s, 0, 0, -s, x + x_offset * s, baseline)
            self.glyphs[name].draw(TransformPen(svg, transform))
            self.glyphs[name].draw(TransformPen(bounds, transform))
            x += x_advance * s
        return svg.getCommands(), bounds.bounds


def orbit_point(cy, ry, t):
    a = math.radians(ORBIT_ANGLE)
    t = math.radians(t)
    x = ORBIT_RX * math.cos(t)
    y = ry * math.sin(t)
    return ORBIT_CX + x * math.cos(a) - y * math.sin(a), cy + x * math.sin(
        a
    ) + y * math.cos(a)


def ellipse(cx, cy, rx, ry):
    return (
        f"M {cx - rx:.3f},{cy} a {rx:.3f},{ry:.3f} 0 1,0 {2 * rx:.3f},0 "
        f"a {rx:.3f},{ry:.3f} 0 1,0 {-2 * rx:.3f},0 Z"
    )


def tile(w, gap, cy, ry, uid=""):
    """The 64x64 tile: the k with the orbit around it."""
    kx = 32 - (K_LEFT + K_RIGHT) / 2 * K_SCALE
    ky = K_CENTER_Y - (K_TOP + 80) / 2 * K_SCALE
    k_transform = f"translate({kx:.2f} {ky:.2f}) scale({K_SCALE})"
    # The ring, its inner ellipse shifted left so the ring tapers to the left
    shift = w * (1 - ORBIT_TAPER)
    ring_d = (
        ellipse(ORBIT_CX, cy, ORBIT_RX + w / 2, ry + w / 2)
        + " "
        + ellipse(ORBIT_CX - shift, cy, ORBIT_RX - w / 2, ry - w / 2)
    )
    ring = f'<path d="{ring_d}" fill-rule="evenodd" transform="rotate({ORBIT_ANGLE} {ORBIT_CX} {cy})"'
    # The front of the orbit, from right of the k to between its stem and leg
    t_end = next(t for t in range(90, 180) if orbit_point(cy, ry, t)[0] < ORBIT_FRONT_X)
    (x0, y0), (x1, y1) = orbit_point(cy, ry, 35), orbit_point(cy, ry, t_end)
    front = f"M {x0:.2f},{y0:.2f} A {ORBIT_RX} {ry} {ORBIT_ANGLE} 0 1 {x1:.2f},{y1:.2f}"
    # Note: a mask applies in the user space of its element, so masked elements
    # carry no transform of their own.
    return (
        f'<defs><linearGradient id="bg{uid}" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{TILE_TOP}"/><stop offset="1" stop-color="{TILE_BOTTOM}"/></linearGradient>'
        f'<mask id="front{uid}" maskUnits="userSpaceOnUse" x="0" y="0" width="64" height="64">'
        f'<path d="{front}" fill="none" stroke="#fff" stroke-width="{2.4 * w + 2 * gap:.2f}"/></mask>'
        f'<mask id="kgap{uid}" maskUnits="userSpaceOnUse" x="0" y="0" width="64" height="64">'
        f'<rect width="64" height="64" fill="#fff"/><g mask="url(#front{uid})">'
        f'{ring} fill="#000" stroke="#000" stroke-width="{2 * gap:.2f}"/></g></mask>'
        f'<mask id="rgap{uid}" maskUnits="userSpaceOnUse" x="0" y="0" width="64" height="64">'
        f'<rect width="64" height="64" fill="#fff"/>'
        f'<path d="{K}" transform="{k_transform}" fill="#000" stroke="#000" '
        f'stroke-width="{2 * gap / K_SCALE:.2f}" stroke-linejoin="round"/>'
        f'<path d="{K_NOTCH}" transform="{k_transform}" fill="#000"/></mask></defs>'
        f'<rect width="64" height="64" rx="14" fill="url(#bg{uid})"/>'
        f'<rect x="0.75" y="0.75" width="62.5" height="62.5" rx="13.25" fill="none" '
        f'stroke="#ffffff" stroke-opacity="0.14" stroke-width="1.5"/>'
        # The orbit behind the k, the k, then the orbit in front of it
        f'<g mask="url(#rgap{uid})">{ring} fill="{ACCENT}"/></g>'
        f'<g mask="url(#kgap{uid})"><path d="{K}" transform="{k_transform}" fill="{INK["on-dark"]}"/></g>'
        f'<g mask="url(#front{uid})">{ring} fill="{ACCENT}"/></g>'
    )


def stemmed(d):
    """Closes the open upper stem of an R or P, short of the middle bar, with a rounded corner."""
    head = "M6.4 26.0V35.44H"
    assert d.startswith(head), d[:40]
    outer, _, counters = d.partition("Z")
    r = CORNER_RADIUS
    # Cubic approximation of a quarter circle
    k = 1 - 0.5523
    bottom = MIDDLE_BAR_TOP - STEM_GAP * STEM_WIDTH
    outer = (
        f"M{STEM_LEFT} {26 + r}V{bottom}H{STEM_RIGHT}V{TOP_BAR_BOTTOM}H"
        + outer[len(head) :]
        + f"H{STEM_LEFT + r}C{STEM_LEFT + r * k} 26 {STEM_LEFT} {26 + r * k} {STEM_LEFT} {26 + r}Z"
    )
    return outer + counters


def wordmark(font):
    """RPC at cap height CAP on baseline 80, and its horizontal bounds."""
    # Glyphs at 80px, where the stem constants apply, then scaled to the cap height
    f = CAP / (80 - font.path("R", 80, baseline=80, shape=False)[1][1])
    paths = []
    x = 0.0
    for char in "RPC":
        d, _ = font.path(char, 80, baseline=80, shape=False)
        if char in "RP":
            d = stemmed(d)
        paths.append(f'<path transform="translate({x:.3f} 0)" d="{d}"/>')
        x += font.advance(char, 80)
    _, (x0, _, x1, _) = font.path("RPC", 80, baseline=80, shape=False)
    group = f'<g transform="translate(0 {80 - 80 * f:.3f}) scale({f:.5f})">{"".join(paths)}</g>'
    return group, x0 * f, x1 * f


def lockup(word, orbit, ink, uid=""):
    """The tile followed by RPC, and its width, with the tile's height."""
    group, x0, x1 = word
    dx = TILE_SIZE + WORD_GAP - x0 * WORD_SCALE
    dy = TILE_SIZE / 2 - (K_TOP + 80) / 2 * WORD_SCALE
    body = (
        f'<g transform="scale({TILE_SCALE})">{tile(**orbit, uid=uid)}</g>'
        f'<g fill="{ink}" transform="translate({dx:.2f} {dy:.2f}) scale({WORD_SCALE})">{group}</g>'
    )
    return body, TILE_SIZE + WORD_GAP + (x1 - x0) * WORD_SCALE + 1


def square(word):
    """The logo on a navy rounded square, the mod listing icon, and the square's side."""
    body, width = lockup(word, LARGE, INK["on-dark"])
    # The logo spans the square but for a margin of a twentieth of its side
    side = width / 0.9
    y = (side - TILE_SIZE) / 2
    radius = side * 14 / 64
    return (
        f'<defs><linearGradient id="square" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{TILE_TOP}"/><stop offset="1" stop-color="{TILE_BOTTOM}"/></linearGradient></defs>'
        f'<rect width="{side:.2f}" height="{side:.2f}" rx="{radius:.2f}" fill="url(#square)"/>'
        f'<g transform="translate({side * 0.05:.2f} {y:.2f})">{body}</g>'
    ), side


def svg(width, height, body, scale=1.0):
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width:.1f} {height:.1f}" '
        f'width="{width * scale:.0f}" height="{height * scale:.0f}"><title>kRPC</title>{body}</svg>\n'
    )


def code_lines(font, x, y, size, line_height):
    """The banner's code, syntax highlighted, one path per color."""
    by_color = {}
    advance = font.advance("m", size)
    for row, line in enumerate(BANNER_CODE.split("\n")):
        column = 0
        for token, text in PythonLexer().get_tokens(line):
            text = text.rstrip("\n")
            color = CODE_DEFAULT
            while token is not Token:
                if token in CODE_COLORS:
                    color = CODE_COLORS[token]
                    break
                token = token.parent
            if text.strip():
                d, _ = font.path(
                    text, size, x + column * advance, y + row * line_height, shape=False
                )
                by_color.setdefault(color, []).append(d)
            column += len(text)
    return "".join(
        f'<path fill="{c}" d="{" ".join(ds)}"/>' for c, ds in by_color.items()
    )


def check_size(background, w, h):
    """Checks the screenshot is cropped to the image. The PNG header holds the
    width and height at bytes 16 to 24."""
    with open(MEDIA / background, "rb") as f:
        size = struct.unpack(">II", f.read(24)[16:24])
    if size != (w, h):
        raise RuntimeError(f"{background} is {size[0]}x{size[1]}, expected {w}x{h}")


def title_block(fonts, word):
    """The logo, tagline and languages, from the logo's top left."""
    logo, _ = lockup(word, LARGE, INK["on-dark"])
    logo_scale = 2.645
    semibold = Font(fonts["inter"], wght=600, opsz=14)
    regular = Font(fonts["inter"], wght=400, opsz=14)
    tagline, _ = semibold.path("Control your rockets with code", 40, 2, 202.88)
    languages, _ = regular.path("Python, C#, C++, C, Java, Lua and more", 26, 2, 250.88)
    block = (
        f'<g transform="scale({logo_scale})">{logo}</g>'
        f'<path fill="{INK["on-dark"]}" d="{tagline}"/><path fill="#a9b4c4" d="{languages}"/>'
    )
    return block


# The code panel's width, font size and line height
PANEL_W = 730
CODE_SIZE, CODE_LINE_HEIGHT = 18, 27
PANEL_H = (BANNER_CODE.count("\n") + 1) * CODE_LINE_HEIGHT + 62


def code_panel(fonts):
    """The code in a panel with a title bar, from its top left."""
    mono = Font(fonts["dejavu-mono"])
    pw, ph = PANEL_W, PANEL_H
    size, line_height = CODE_SIZE, CODE_LINE_HEIGHT
    title, _ = mono.path("launch.py", 14, 22, 27, shape=False)
    code = code_lines(mono, 26, 72, size, line_height)
    panel = (
        f'<rect width="{pw}" height="{ph}" rx="14" fill="#0b1220" fill-opacity="0.72" '
        f'stroke="#ffffff" stroke-opacity="0.12" stroke-width="1.5"/>'
        f'<path d="M 0 14 a 14 14 0 0 1 14 -14 h {pw - 28} a 14 14 0 0 1 14 14 v 26 h {-pw} z" '
        f'fill="#ffffff" fill-opacity="0.05"/>'
        f'<path fill="#8792a8" d="{title}"/>{code}'
    )
    return panel


def backdrop(
    w, h, background, shade_stops, body, screenshot='<use xlink:href="#screenshot"/>'
):
    """The screenshot under a horizontal shade and a vertical vignette, then the body.
    The screenshot is embedded, so the SVG stands alone, and screenshot places it."""
    image = base64.b64encode((MEDIA / background).read_bytes()).decode()
    shade = "#070c16"
    stops = "".join(
        f'<stop offset="{offset}" stop-color="{shade}" stop-opacity="{opacity}"/>'
        for offset, opacity in shade_stops
    )
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" '
        f'viewBox="0 0 {w} {h}" width="{w}" height="{h}"><title>kRPC</title><defs>'
        f'<linearGradient id="shade" x1="0" y1="0" x2="1" y2="0">{stops}</linearGradient>'
        f'<linearGradient id="vignette" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{shade}" stop-opacity="0.35"/>'
        f'<stop offset="0.3" stop-color="{shade}" stop-opacity="0"/>'
        f'<stop offset="0.75" stop-color="{shade}" stop-opacity="0"/>'
        f'<stop offset="1" stop-color="{shade}" stop-opacity="0.5"/></linearGradient>'
        f'<image id="screenshot" xlink:href="data:image/png;base64,{image}" '
        f'width="{BANNER_W}" height="{BANNER_H}"/></defs>'
        f'<rect width="{w}" height="{h}" fill="{shade}"/>{screenshot}'
        f'<rect width="{w}" height="{h}" fill="url(#shade)"/><rect width="{w}" height="{h}" fill="url(#vignette)"/>'
        f"{body}</svg>\n"
    )


# The banner's shade, horizontally across its layout
BANNER_SHADE = [(0, 0.92), (0.36, 0.35), (0.52, 0), (0.62, 0.25), (1, 0.75)]


def banner_layout(fonts, word):
    """Logo and tagline, a gap for the rocket, then the code, on the banner."""
    title = title_block(fonts, word)
    panel = code_panel(fonts)
    return (
        f'<g transform="translate(110 {BANNER_H / 2 - TILE_SIZE / 2 * 2.3 - 52:.2f})">{title}</g>'
        f'<g transform="translate(1120 84)">{panel}</g>'
    )


def banner(fonts, word, game):
    """The forum banner."""
    background = f"background-{game}.png"
    check_size(background, BANNER_W, BANNER_H)
    body = banner_layout(fonts, word)
    return backdrop(BANNER_W, BANNER_H, background, BANNER_SHADE, body)


def spacedock(fonts, word, game):
    """The SpaceDock header: the banner's layout, scaled and centered vertically."""
    w, h = SPACEDOCK_W, SPACEDOCK_H
    background = f"background-{game}.png"
    check_size(background, BANNER_W, BANNER_H)
    s = SPACEDOCK_SCALE
    screenshot = spacedock_screenshot(game)
    width = BANNER_W * s / w
    # The shade lifts over the thumbnail scene, which carries a shade of its own
    scene_x0 = THUMBNAIL_SCENE_X / w
    scene_x1 = (THUMBNAIL_SCENE_X + THUMBNAIL_SCENE_W) / w
    shade = [(offset * width, opacity) for offset, opacity in BANNER_SHADE] + [
        (scene_x0 - 0.01, 0.75),
        (scene_x0, 0),
        (scene_x1, 0),
        (scene_x1 + 0.01, 0.75),
    ]
    # The thumbnail: the center of the header, at the thumbnail's aspect ratio
    thumb_w = THUMBNAIL_W * h / THUMBNAIL_H
    thumb_x = (w - thumb_w) / 2
    # Logo top left, rocket bottom left, and the code to the right, running off
    # the thumbnail's edge and clipped at the scene's
    logo, _ = lockup(word, LARGE, INK["on-dark"])
    logo_scale = 72 / TILE_SIZE
    code_scale = 0.62
    body = (
        f'<g transform="translate(0 {(h - BANNER_H * s) / 2:.2f}) scale({s})">'
        f"{banner_layout(fonts, word)}</g>"
        f'<clipPath id="thumbnail-clip"><rect x="{THUMBNAIL_SCENE_X}" '
        f'width="{THUMBNAIL_SCENE_W}" height="{h}"/></clipPath>'
        f'<g transform="translate({thumb_x + 28:.2f} 28) scale({logo_scale:.4f})">{logo}</g>'
        f'<g clip-path="url(#thumbnail-clip)"><g transform="translate({thumb_x + 300:.2f} '
        f"{(h - PANEL_H * code_scale) / 2:.2f}) "
        f'scale({code_scale})">{code_panel(fonts)}</g></g>'
    )
    return backdrop(w, h, background, shade, body, screenshot)


def fade_mask(name, left, right, x0, x1, stops):
    """A mask from left to right, fading across x0 to x1 with stops of (offset, opacity)."""
    gradient = "".join(
        f'<stop offset="{offset}" stop-color="#fff" stop-opacity="{opacity}"/>'
        for offset, opacity in stops
    )
    return (
        f'<linearGradient id="{name}-fade" gradientUnits="userSpaceOnUse" '
        f'x1="{x0:.1f}" y1="0" x2="{x1:.1f}" y2="0">{gradient}</linearGradient>'
        f'<mask id="{name}" maskUnits="userSpaceOnUse" x="0" y="0" '
        f'width="{SPACEDOCK_W}" height="{SPACEDOCK_H}">'
        f'<rect x="{left:.1f}" width="{right - left:.1f}" height="{SPACEDOCK_H}" '
        f'fill="url(#{name}-fade)"/>'
        f"</mask>"
    )


def spacedock_screenshot(game):
    """The banner screenshot, twice: filling the header's height, with the rocket
    in the layout's gap and fading out to the right, and in the thumbnail scene."""
    rx, _ = ROCKETS[game]
    k = SPACEDOCK_H / BANNER_H
    x = rx * SPACEDOCK_SCALE - rx * k
    right = x + BANNER_W * k
    # The thumbnail scene, with the rocket at its bottom left. The screenshot
    # sits lower than the scene's top, and fades in from the black above it.
    scene_scale, scene_y = THUMBNAIL_SCENES[game]
    scene_x = THUMBNAIL_SCENE_X + 222 - rx * scene_scale
    scene_end = THUMBNAIL_SCENE_X + THUMBNAIL_SCENE_W
    edge = 72 / THUMBNAIL_SCENE_W
    return (
        f"<defs>{fade_mask('main-mask', 0, right, right - 200, right, [(0, 1), (1, 0)])}"
        + fade_mask(
            "scene-mask",
            THUMBNAIL_SCENE_X,
            scene_end,
            THUMBNAIL_SCENE_X,
            scene_end,
            [(0, 0), (edge, 1), (1 - edge, 1), (1, 0)],
        )
        + '<linearGradient id="scene-top-fade" gradientUnits="userSpaceOnUse" '
        f'x1="0" y1="{scene_y}" x2="0" y2="{scene_y + 60}">'
        '<stop offset="0" stop-color="#fff" stop-opacity="0"/>'
        '<stop offset="1" stop-color="#fff" stop-opacity="1"/></linearGradient>'
        f'<mask id="scene-top" maskUnits="userSpaceOnUse" x="0" y="0" '
        f'width="{SPACEDOCK_W}" height="{SPACEDOCK_H}">'
        f'<rect width="{SPACEDOCK_W}" height="{SPACEDOCK_H}" fill="url(#scene-top-fade)"/></mask>'
        "</defs>"
        f'<g mask="url(#main-mask)"><use xlink:href="#screenshot" '
        f'transform="translate({x:.2f} 0) scale({k:.5f})"/></g>'
        f'<g mask="url(#scene-mask)"><g mask="url(#scene-top)"><use xlink:href="#screenshot" '
        f'transform="translate({scene_x:.2f} {scene_y}) scale({scene_scale})"/></g></g>'
    )


def preview(word):
    """A sheet of the logos on light, dark and mid backgrounds, from large down to 16px high."""
    backgrounds = ["#ffffff", "#0d1117", "#5b6b5b"]
    heights = [160, 64, 32, 24, 16]
    rows = []
    uid = 0
    y = 0.0
    columns = [LARGE, SMALL]
    sheet_w = 0.0
    for variant in INK:
        for background in backgrounds:
            row_h = heights[0] + 16
            rows.append(
                f'<rect x="0" y="{y}" width="100%" height="{row_h}" fill="{background}"/>'
            )
            x = 8.0
            for orbit in columns:
                for height in heights:
                    uid += 1
                    body, width = lockup(word, orbit, INK[variant], uid=str(uid))
                    s = height / TILE_SIZE
                    rows.append(
                        f'<g transform="translate({x:.1f} {y + (row_h - height) / 2:.1f}) scale({s:.4f})">{body}</g>'
                    )
                    x += width * s + 16
                x += 24
            sheet_w = max(sheet_w, x)
            y += row_h
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {sheet_w:.0f} {y:.0f}" '
        f'width="{sheet_w:.0f}" height="{y:.0f}">{"".join(rows)}</svg>\n'
    )


def render_png(source, target, *size):
    subprocess.run(["rsvg-convert", *size, str(source), "-o", str(target)], check=True)


def main():
    parser = argparse.ArgumentParser(
        description="Generates the kRPC logos and banners."
    )
    parser.add_argument(
        "--font-cache",
        type=Path,
        default=BUILD / "fonts",
        help="where to download the fonts",
    )
    parser.add_argument(
        "--preview", type=Path, help="also write a preview sheet to this PNG"
    )
    args = parser.parse_args()

    fonts = fetch_fonts(args.font_cache)
    word = wordmark(Font(fonts["nasalization"]))

    outputs = {"logo-mark.svg": svg(64, 64, tile(**LARGE))}
    for variant, ink in INK.items():
        for prefix, orbit in (("logo", LARGE), ("logo-small", SMALL)):
            body, width = lockup(word, orbit, ink)
            outputs[f"{prefix}-{variant}.svg"] = svg(width, TILE_SIZE, body, scale=2.5)
    body, side = square(word)
    outputs["logo-square.svg"] = svg(side, side, body)
    for game in GAMES:
        outputs[f"banner-{game}.svg"] = banner(fonts, word, game)
        outputs[f"spacedock-{game}.svg"] = spacedock(fonts, word, game)
    BUILD.mkdir(exist_ok=True)
    for name, content in outputs.items():
        (BUILD / name).write_text(content)
        print("wrote", f"build/{name}")
    # The logos the README and documentation use, which are committed
    for name in PUBLISHED:
        shutil.copy(BUILD / name, MEDIA / name)
        print("wrote", name)

    if not shutil.which("rsvg-convert"):
        print("rsvg-convert not found, skipping the PNGs")
        return
    # PNG sizes, by default the SVG's own
    sizes = {"logo-mark.svg": ["-w", "512"], "logo-square.svg": ["-w", "512"]}
    for variant in INK:
        sizes[f"logo-{variant}.svg"] = ["-h", "256"]
        sizes[f"logo-small-{variant}.svg"] = ["-h", "64"]
    for name in outputs:
        render_png(
            BUILD / name, (BUILD / name).with_suffix(".png"), *sizes.get(name, [])
        )
    for name in PUBLISHED_PNG:
        shutil.copy(BUILD / name, MEDIA / name)
        print("wrote", name)
    if args.preview:
        sheet = args.preview.with_suffix(".svg")
        sheet.write_text(preview(word))
        render_png(sheet, args.preview)
        print("wrote", args.preview)


if __name__ == "__main__":
    main()
