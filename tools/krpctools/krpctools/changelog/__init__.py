"""Generate a single, unified changelog page (reStructuredText) for the docs
site from the per-component ``CHANGES.txt`` files.

The ``CHANGES.txt`` files are not rendered markdown, so they are parsed rather
than passed through. Each file is a sequence of ``vX.Y.Z[-pre]`` version headers
followed by ``  * `` bullet items; wrapped continuation lines are indented and
carry no bullet. Issue/PR references appear inline as ``(#NNN)``.

Items may start with an inline ``**Breaking:**`` or ``**Deprecated:**`` marker
(used from v0.5.4 onward). Such items are hoisted into a highlighted callout at
the top of their version's section, tagged with their component, and also kept
in place in the per-component list.
"""

import argparse
import re
from dataclasses import dataclass


@dataclass
class Item:
    """A single changelog entry, already RST-escaped and issue-linked."""

    text: str
    kind: str  # "", "breaking" or "deprecated"


# Inline markers, longest-first so matching is unambiguous.
_MARKERS = (
    ("**Breaking:**", "breaking"),
    ("**Deprecated:**", "deprecated"),
)

_VERSION_RE = re.compile(r"^v(\d[^\s]*)\s*$")
_RST_SPECIAL_RE = re.compile(r"([\\*`|_])")
_ISSUE_RE = re.compile(r"#(\d+)")
_NUM_RE = re.compile(r"^\d+$")


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
    text can never emit a warning under the strict ``-W`` docs build."""
    return _RST_SPECIAL_RE.sub(r"\\\1", text)


def linkify(text):
    """Turn ``#NNN`` issue/PR references into clickable ``:issue:`` roles."""
    return _ISSUE_RE.sub(r":issue:`\1`", text)


def split_marker(raw):
    """Return ``(kind, remaining_text)`` for a raw item, stripping any marker."""
    for marker, kind in _MARKERS:
        if raw.startswith(marker):
            return kind, raw[len(marker) :].strip()
    return "", raw


def parse_changes(text):
    """Parse a ``CHANGES.txt`` into ``{version: [Item, ...]}``."""
    versions = {}
    current = None
    fragments = None  # raw fragments of the item currently being read

    def flush():
        # Join wrapped continuation lines into a single item.
        if current is not None and fragments:
            raw = " ".join(fragments)
            kind, body = split_marker(raw)
            versions[current].append(Item(linkify(escape_rst(body)), kind))

    for raw_line in text.splitlines():
        line = raw_line.rstrip()
        stripped = line.strip()
        if not stripped:
            continue
        match = _VERSION_RE.match(line)
        if match and not line[0].isspace():
            flush()
            fragments = None
            current = match.group(1)
            versions[current] = []
            continue
        if current is None:
            continue
        if stripped.startswith("*"):
            flush()
            fragments = [stripped[1:].strip()]
        elif fragments is not None:
            fragments.append(stripped)
    flush()
    return versions


def heading(text, char):
    return "%s\n%s\n" % (text, char * len(text))


def render_callout(directive, title, tagged_items):
    """Render a Sphinx admonition holding hoisted items, each tagged with the
    component it came from. ``tagged_items`` is a list of ``(component, Item)``."""
    lines = [".. %s:: **%s**" % (directive, title), ""]
    for component, item in tagged_items:
        lines.append("   * *(%s)* %s" % (component, item.text))
    lines.append("")
    return "\n".join(lines) + "\n"


def render(components):
    """Render the merged changelog. ``components`` is an ordered list of
    ``(display_name, {version: [Item, ...]})``."""
    all_versions = set()
    for _, versions in components:
        all_versions.update(versions)
    ordered = sorted(all_versions, key=version_key, reverse=True)

    out = [heading("Changelog", "=")]

    for version in ordered:
        out.append("\n" + heading(version, "-"))

        breaking = []
        deprecated = []
        for name, versions in components:
            for item in versions.get(version, []):
                if item.kind == "breaking":
                    breaking.append((name, item))
                elif item.kind == "deprecated":
                    deprecated.append((name, item))
        if breaking:
            out.append("\n" + render_callout("warning", "Breaking changes", breaking))
        if deprecated:
            out.append("\n" + render_callout("note", "Deprecated", deprecated))

        for name, versions in components:
            items = versions.get(version, [])
            if not items:
                continue
            out.append("\n" + heading(name, "^"))
            for item in items:
                out.append("* %s" % item.text)
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
        help="A component display name and the path to its CHANGES.txt",
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
