"""Generate a single, unified changelog page (reStructuredText) for the docs
site from the per-component ``CHANGELOG.md`` files and the changelog fragments.

The files are parsed by the shared ``changelog`` module, which merges each
fragment into the unreleased section of its components. Issue/PR references
appear inline as ``(#NNN)``, and code identifiers as markdown inline ``code``
spans.

Items may start with an inline ``**Breaking:**`` or ``**Deprecated:**`` marker.
Such items stay in their component's list, where the marker is rendered as a small
highlighted label in front of the entry text.
"""

import argparse
import os
import re
import sys
from dataclasses import dataclass, field

import changelog


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

_RST_SPECIAL_RE = re.compile(r"([\\*`|_])")
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
    return changelog.NUMBER_RE.sub(r":issue:`\1`", text)


def split_marker(raw):
    """Return ``(kind, remaining_text)`` for a raw item, stripping any marker."""
    for marker, kind in _MARKERS:
        if raw.startswith(marker):
            return kind, raw[len(marker) :].strip()
    return "", raw


def to_items(nodes):
    """Convert parsed entries into Items."""
    items = []
    for node in nodes:
        kind, body = split_marker(node.text)
        items.append(Item(linkify(escape_rst(body)), kind, to_items(node.children)))
    return items


def parse_component(path, version, fragments):
    """Parse a component's ``CHANGELOG.md`` into ``(versions, unreleased)``,
    with its fragment entries merged into the unreleased section of
    ``version``.

    ``versions`` maps each version to its ``[Item, ...]``; ``unreleased`` is the
    set of versions whose header carried the `` - unreleased`` marker."""
    with open(path, encoding="utf-8") as fp:
        sections = changelog.parse_changelog(fp.read(), path)
    versions = {}
    unreleased = set()
    for section in sections:
        versions[section.version] = section.nodes
        if section.unreleased:
            unreleased.add(section.version)
    additions = changelog.additions(fragments, os.path.dirname(path))
    if additions:
        section = changelog.unreleased_section(sections, version, path)
        versions[version] = changelog.merge(section.nodes if section else [], additions)
        unreleased.add(version)
    return {v: to_items(nodes) for v, nodes in versions.items()}, unreleased


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


def render(components, unreleased=()):
    """Render the merged changelog. ``components`` is an ordered list of
    ``(display_name, {version: [Item, ...]})``; ``unreleased`` is the set of
    versions to mark as not yet released in their heading."""
    all_versions = set()
    for _, versions in components:
        all_versions.update(versions)
    ordered = sorted(all_versions, key=version_key, reverse=True)

    out = [_ROLES, heading("Changelog", "=")]

    for version in ordered:
        title = (
            "v%s - unreleased" % version if version in unreleased else "v%s" % version
        )
        out.append("\n" + heading(title, "-"))

        for name, versions in components:
            items = versions.get(version, [])
            if not items:
                continue
            # A page of one component needs no component headings
            if len(components) > 1:
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
        "--changelog",
        action="append",
        default=[],
        help="The path to a component's CHANGELOG.md",
    )
    parser.add_argument(
        "--fragment",
        action="append",
        default=[],
        help="A changelog fragment, merged into the unreleased sections",
    )
    parser.add_argument(
        "--version", required=True, help="The version the fragments are for"
    )
    args = parser.parse_args()

    paths = {os.path.dirname(path): path for path in args.changelog}
    missing = [c for c, _ in changelog.shown_components() if c not in paths]
    if missing:
        print(f"no --changelog for {', '.join(missing)}", file=sys.stderr)
        return 1

    try:
        fragments = changelog.load_fragments(args.fragment)
        components = []
        unreleased = set()
        for component, name in changelog.shown_components():
            versions, marked = parse_component(
                paths[component], args.version, fragments
            )
            components.append((name, versions))
            unreleased |= marked
    except changelog.ChangelogError as error:
        print(error, file=sys.stderr)
        return 1

    content = render(components, unreleased)
    with open(args.output, "w", encoding="utf-8") as fp:
        fp.write(content)


if __name__ == "__main__":
    sys.exit(main())
