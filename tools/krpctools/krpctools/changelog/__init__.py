"""Generate a single, unified changelog page (reStructuredText) for the docs
site from the per-component ``CHANGELOG.md`` files.

The ``CHANGELOG.md`` files are markdown, but the page is built by parsing them
rather than passing them through. Each file is a sequence of ``## [X.Y.Z[-pre]]``
version headers (the in-development version carries an `` - unreleased`` suffix,
which is ignored) followed by ``- `` bullet items; wrapped continuation lines are
indented and carry no bullet. A ``  - `` bullet indented under an entry is a
sub-item, used to group a long version's entries under topic headings. Issue/PR
references appear inline as ``(#NNN)``, and code identifiers as markdown inline
``code`` spans.

Items may start with an inline ``**Breaking:**`` or ``**Deprecated:**`` marker.
Such items stay in their component's list, where the marker is rendered as a small
highlighted label in front of the entry text.
"""

import argparse
import re
from dataclasses import dataclass, field


@dataclass
class Item:
    """A single changelog entry, already RST-escaped and issue-linked.

    An entry may carry nested sub-items (from ``  - `` sub-bullets), used to
    group a long version's entries under topic headings."""

    text: str
    kind: str  # "", "breaking" or "deprecated"
    children: list = field(default_factory=list)


# Inline markers, longest-first so matching is unambiguous.
_MARKERS = (
    ("**Breaking:**", "breaking"),
    ("**Deprecated:**", "deprecated"),
)

# Label text shown in the rendered marker for each kind.
_LABELS = {"breaking": "Breaking", "deprecated": "Deprecated"}

# Custom inline roles carrying the marker labels. Both share a class that styles
# them as a label, plus a per-kind class that colors them by severity; the rules
# live in the docs theme's custom.css.
_ROLES = "\n".join(
    ".. role:: %s\n   :class: changelog-marker %s\n" % (kind, kind) for kind in _LABELS
)

_VERSION_RE = re.compile(r"^##\s+\[(\d[^\]]*)\]")
_RST_SPECIAL_RE = re.compile(r"([\\*`|_])")
_ISSUE_RE = re.compile(r"#(\d+)")
_NUM_RE = re.compile(r"^\d+$")
# Markdown inline code span: `code`.
_CODE_RE = re.compile(r"`([^`]+)`")


def version_key(version):
    """Semver-ish sort key; a higher tuple is a newer version. A release (no
    pre-release suffix) sorts above all of its own pre-releases."""
    core, _, pre = version.partition("-")
    nums = [int(p) if _NUM_RE.match(p) else 0 for p in core.split(".")]
    while len(nums) < 3:
        nums.append(0)
    if pre == "":
        return (tuple(nums), 1, ())
    pre_parts = tuple(
        (0, int(p)) if _NUM_RE.match(p) else (1, p) for p in pre.split(".")
    )
    return (tuple(nums), 0, pre_parts)


def escape_rst(text):
    """Escape reStructuredText inline-markup characters so free-form changelog
    text can never emit a warning under the strict ``-W`` docs build.

    Markdown inline ``code`` spans become RST inline literals (``` ``code`` ```),
    whose contents are left verbatim; RST specials are escaped only in the prose
    between them."""
    parts = []
    pos = 0
    for match in _CODE_RE.finditer(text):
        parts.append(_RST_SPECIAL_RE.sub(r"\\\1", text[pos : match.start()]))
        parts.append("``%s``" % match.group(1))
        pos = match.end()
    parts.append(_RST_SPECIAL_RE.sub(r"\\\1", text[pos:]))
    return "".join(parts)


def linkify(text):
    """Turn ``#NNN`` issue/PR references into clickable ``:issue:`` roles."""
    return _ISSUE_RE.sub(r":issue:`\1`", text)


def split_marker(raw):
    """Return ``(kind, remaining_text)`` for a raw item, stripping any marker."""
    for marker, kind in _MARKERS:
        if raw.startswith(marker):
            return kind, raw[len(marker) :].strip()
    return "", raw


def _make_item(fragments):
    """Build an Item from a bullet's raw fragments (its text plus any wrapped
    continuation lines), stripping and rendering the marker and inline markup."""
    kind, body = split_marker(" ".join(fragments))
    return Item(linkify(escape_rst(body)), kind)


def parse_changes(text):
    """Parse a ``CHANGELOG.md`` into ``{version: [Item, ...]}``.

    A ``- `` bullet at the margin is a top-level entry; a ``  - `` bullet
    indented under it is a sub-item; other indented lines continue the current
    bullet's text. One level of nesting is supported."""
    versions = {}
    current = None
    top = None  # open top-level Item (holds accumulating children)
    top_fragments = None  # its raw text fragments
    sub_fragments = None  # open sub-item's raw fragments, or None

    def flush_sub():
        nonlocal sub_fragments
        if sub_fragments is not None:
            top.children.append(_make_item(sub_fragments))
            sub_fragments = None

    def flush_top():
        nonlocal top, top_fragments
        flush_sub()
        if top is not None:
            item = _make_item(top_fragments)
            item.children = top.children
            versions[current].append(item)
            top = None
            top_fragments = None

    for raw_line in text.splitlines():
        line = raw_line.rstrip()
        stripped = line.strip()
        if not stripped:
            continue
        match = _VERSION_RE.match(line)
        if match:
            flush_top()
            current = match.group(1)
            versions[current] = []
            continue
        if current is None:
            continue
        indent = len(line) - len(line.lstrip(" "))
        if stripped.startswith("- "):
            body = stripped[2:].strip()
            if indent == 0 or top is None:
                flush_top()
                top = Item("", "")  # placeholder; real text set at flush
                top_fragments = [body]
            else:
                flush_sub()
                sub_fragments = [body]
        elif sub_fragments is not None:
            sub_fragments.append(stripped)
        elif top_fragments is not None:
            top_fragments.append(stripped)
    flush_top()
    return versions


def heading(text, char):
    return "%s\n%s\n" % (text, char * len(text))


def render_item(item, level=0):
    """Render one bullet (and any nested sub-list), prefixing marked items with
    their label role. A nested list is set off by blank lines and indented so
    its markers align within the parent bullet, as RST requires."""
    indent = "  " * level
    if item.kind:
        line = "%s* :%s:`%s` %s" % (indent, item.kind, _LABELS[item.kind], item.text)
    else:
        line = "%s* %s" % (indent, item.text)
    if not item.children:
        return line
    parts = [line, ""]
    parts.extend(render_item(child, level + 1) for child in item.children)
    parts.append("")
    return "\n".join(parts)


def render(components):
    """Render the merged changelog. ``components`` is an ordered list of
    ``(display_name, {version: [Item, ...]})``."""
    all_versions = set()
    for _, versions in components:
        all_versions.update(versions)
    ordered = sorted(all_versions, key=version_key, reverse=True)

    out = [_ROLES, heading("Changelog", "=")]

    for version in ordered:
        out.append("\n" + heading(version, "-"))

        for name, versions in components:
            items = versions.get(version, [])
            if not items:
                continue
            out.append("\n" + heading(name, "^"))
            for item in items:
                out.append(render_item(item))
            out.append("")

    return "\n".join(out).rstrip() + "\n"


def main():
    parser = argparse.ArgumentParser(
        description="Generate the unified kRPC changelog page",
    )
    parser.add_argument("output", help="Path to write the changelog .rst to")
    parser.add_argument(
        "--entry",
        action="append",
        nargs=2,
        metavar=("NAME", "PATH"),
        default=[],
        help="A component display name and the path to its CHANGELOG.md",
    )
    args = parser.parse_args()

    components = []
    for name, path in args.entry:
        with open(path, "r", encoding="utf-8") as fp:
            components.append((name, parse_changes(fp.read())))

    content = render(components)
    with open(args.output, "w", encoding="utf-8") as fp:
        fp.write(content)


if __name__ == "__main__":
    main()
