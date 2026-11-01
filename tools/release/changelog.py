"""Changelog entries: the CHANGELOG.md and fragment parsers, and the merge of
fragments into a component's unreleased section.

A ``CHANGELOG.md`` is a sequence of ``## [vX.Y.Z]`` version headers, the
in-development one suffixed `` - unreleased``, each followed by ``- `` bullets.
Wrapped lines are indented and carry no bullet. A ``  - `` bullet indented under
an entry is a sub-item, and an entry with sub-items is a topic.

A fragment in ``changelog.d/`` holds the unreleased entries of one pull
request: a ``## <component>`` section per component, each holding bullets in
the same syntax. The unreleased section of a component is its ``CHANGELOG.md``
section followed by the entries of every fragment, in fragment order. A topic
merges by text with an earlier topic of the same text.

Shared by the docs changelog page, the release notes, the release fold and the
CI checks. Imports nothing from the release scripts, as it runs inside Bazel
actions too.
"""

import copy
import re
from dataclasses import dataclass, field
from pathlib import Path

# The directory holding the fragments, relative to the repository root
FRAGMENT_DIR = "changelog.d"

# The components on the kRPC release version, by directory, with the name the
# docs page and the release notes show them under, in the order shown. Each has
# a CHANGELOG.md whose unreleased header takes the version from config.bzl.
# krpctest takes fragments but is shown in neither.
COMPONENTS = {
    "core": "Core",
    "server": "Server",
    "service/SpaceCenter": "SpaceCenter service",
    "service/Drawing": "Drawing service",
    "service/UI": "User Interface service",
    "service/InfernalRobotics": "InfernalRobotics service",
    "service/KerbalAlarmClock": "KerbalAlarmClock service",
    "service/RemoteTech": "RemoteTech service",
    "service/LiDAR": "LIDAR service",
    "service/DockingCamera": "DockingCamera service",
    "service/Debug": "Debug service",
    "client/cnano": "C-nano client",
    "client/csharp": "C# client",
    "client/cpp": "C++ client",
    "client/java": "Java client",
    "client/lua": "Lua client",
    "client/python": "Python client",
    "tools/krpctools": "krpctools",
    "tools/krpctest": None,
}


def shown_components():
    """The components the docs page and the release notes show, as
    (directory, name) pairs in the order shown."""
    return [(component, name) for component, name in COMPONENTS.items() if name]


FRAGMENT_FORMAT = (
    "A fragment is a '## <component>' heading per component, such as "
    "'## client/python', each followed by '- ' entries. See CONTRIBUTING.md."
)

_NAME_RE = re.compile(r"^[A-Za-z0-9._-]+\.md$")
_HEADER_RE = re.compile(r"^##\s*\[")
_VERSION_RE = re.compile(r"^## \[v(\d+\.\d+\.\d+(?:\.post\d+)?)\]( - unreleased)?$")
_COMPONENT_RE = re.compile(r"^##\s+(\S+)\s*$")
# An issue or pull request number, as against the version in C#10
NUMBER_RE = re.compile(r"(?<![\w#])#(\d+)")


class ChangelogError(Exception):
    """A changelog or fragment that does not parse. The message starts with the
    file and line."""


def _error(path, line, message):
    return ChangelogError(f"{path}:{line}: {message}" if line else f"{path}: {message}")


@dataclass
class Node:
    """One entry: its source lines and any sub-items.

    The lines are kept as written, so a fold writes existing entries back
    unchanged. ``blanks`` counts the blank lines above the entry.
    """

    lines: list[str]
    line: int
    children: list["Node"] = field(default_factory=list)
    blanks: int = 0

    @property
    def text(self):
        """The entry text, with the bullet dropped and wrapped lines joined."""
        return " ".join(line.strip() for line in self.lines)[2:]

    def render(self):
        """The source lines of the entry and its sub-items."""
        out = [""] * self.blanks + self.lines
        for child in self.children:
            out.extend(child.render())
        return out

    def numbers(self):
        """Every '#NNN' cited by the entry or its sub-items."""
        found = [int(n) for n in NUMBER_RE.findall(self.text)]
        for child in self.children:
            found.extend(child.numbers())
        return found


@dataclass
class Section:
    """One version of a CHANGELOG.md. ``start`` is the index of its header line
    and ``end`` the index of the next header, or the line count."""

    version: str
    unreleased: bool
    nodes: list[Node]
    start: int
    end: int


@dataclass
class Fragment:
    """The entries of one fragment file, by component."""

    path: str
    sections: dict[str, list[Node]]

    @property
    def name(self):
        return Path(self.path).name

    def numbers(self):
        return [
            n
            for nodes in self.sections.values()
            for node in nodes
            for n in node.numbers()
        ]

    def sort_key(self):
        """Fragments citing a number come first, by their highest number, then
        the rest by file name."""
        numbers = self.numbers()
        if numbers:
            return (0, max(numbers), self.name)
        return (1, 0, self.name)


class _ItemParser:
    """Collects the bullets of one section into nodes."""

    def __init__(self, path):
        self.path = path
        self.nodes = []
        self.blanks = 0
        self.top = None
        self.current = None

    def blank(self):
        self.blanks += 1

    def add(self, number, line):
        """Add one non-blank line, which must belong to an entry."""
        stripped = line.strip()
        indent = len(line) - len(line.lstrip(" "))
        if stripped.startswith("- ") and indent == 0:
            self.top = Node([line], number, blanks=self.blanks)
            self.nodes.append(self.top)
            self.current = self.top
        elif stripped.startswith("- ") and indent not in (0, 2):
            raise _error(
                self.path,
                number,
                "an entry is a '- ' line, unindented, and a sub-item a '  - ' line "
                "indented 2 spaces; sub-items have no sub-items",
            )
        elif stripped.startswith("- ") and indent == 2 and self.top is not None:
            self.current = Node([line], number, blanks=self.blanks)
            self.top.children.append(self.current)
        elif indent > 0 and self.current is not None:
            if self.blanks:
                raise _error(
                    self.path,
                    number,
                    "blank line inside an entry; indent a new entry as '  - '",
                )
            self.current.lines.append(line)
        else:
            raise _error(
                self.path,
                number,
                "expected a '- ' entry, a '  - ' sub-item under an entry, or an indented "
                f"continuation line, found '{stripped}'",
            )
        self.blanks = 0


def parse_changelog(text, path):
    """Parse a CHANGELOG.md into its sections, newest first as written.

    Lines before the first version header are a preamble, and are skipped."""
    lines = text.splitlines()
    sections = []
    parser = None
    for index, raw in enumerate(lines):
        line = raw.rstrip()
        match = _VERSION_RE.match(line)
        if _HEADER_RE.match(line) and not match:
            raise _error(
                path,
                index + 1,
                f"expected '## [vX.Y.Z]' or '## [vX.Y.Z] - unreleased', found '{line}'",
            )
        if match:
            if sections:
                sections[-1].end = index
            # The section shares the parser's list, which fills as lines are added
            parser = _ItemParser(path)
            sections.append(
                Section(
                    match.group(1),
                    match.group(2) is not None,
                    parser.nodes,
                    index,
                    len(lines),
                )
            )
        elif parser is None:
            continue
        elif not line:
            parser.blank()
        elif line.startswith("#"):
            raise _error(
                path, index + 1, f"expected a '## [vX.Y.Z]' header, found '{line}'"
            )
        else:
            parser.add(index + 1, line)
    return sections


def check_name(path):
    """Raise for a fragment path that is not 'changelog.d/<name>.md'."""
    path = Path(path)
    if path.parent.name != FRAGMENT_DIR:
        raise _error(path, 0, f"a fragment sits directly in {FRAGMENT_DIR}/")
    if not _NAME_RE.match(path.name):
        raise _error(
            path,
            0,
            "a fragment is named '<branch>.md', from letters, digits, '.', '_' and '-'",
        )


def parse_fragment(text, path):
    """Parse a fragment. Every line belongs to a component section."""
    sections = {}
    parser = None
    component = None
    heading = 0

    def close():
        if parser is not None:
            if not parser.nodes:
                raise _error(path, heading, f"'## {component}' has no entries")
            sections[component] = parser.nodes

    for index, raw in enumerate(text.splitlines()):
        number = index + 1
        line = raw.rstrip()
        if line.startswith("#"):
            match = _COMPONENT_RE.match(line)
            if not match:
                raise _error(
                    path, number, f"expected '## <component>'. {FRAGMENT_FORMAT}"
                )
            close()
            component = match.group(1)
            if component not in COMPONENTS:
                raise _error(
                    path,
                    number,
                    f"'{component}' is not a component on the kRPC release version. "
                    f"Expected one of: {', '.join(COMPONENTS)}",
                )
            if component in sections:
                raise _error(path, number, f"'## {component}' appears twice")
            parser = _ItemParser(path)
            heading = number
        elif not line:
            if parser is not None:
                parser.blank()
        elif parser is None:
            raise _error(
                path, number, f"text outside a component section. {FRAGMENT_FORMAT}"
            )
        else:
            parser.add(number, line)
    close()
    if not sections:
        raise _error(path, 0, f"no entries. {FRAGMENT_FORMAT}")
    return Fragment(str(path), sections)


def load_fragments(paths):
    """Parse the fragment files, in fragment order. Every problem in every file
    is reported, in one error."""
    fragments = []
    errors = []
    for path in paths:
        try:
            check_name(path)
            fragments.append(
                parse_fragment(Path(path).read_text(encoding="utf-8"), path)
            )
        except ChangelogError as error:
            errors.append(str(error))
    if errors:
        raise ChangelogError("\n".join(errors))
    return sorted(fragments, key=Fragment.sort_key)


def fragment_paths(root):
    """Every file under the fragment directory of a checkout, or none if the
    directory is absent."""
    directory = Path(root) / FRAGMENT_DIR
    if not directory.exists():
        return []
    return sorted(str(path) for path in directory.rglob("*") if not path.is_dir())


def merge(nodes, additions):
    """Append entries to a section, merging a topic into an earlier topic of
    the same text. Returns new nodes, laid out as a fold writes them: topics
    set off by a blank line."""
    merged = copy.deepcopy(nodes)
    topics = {node.text: node for node in merged if node.children}
    for addition in additions:
        node = copy.deepcopy(addition)
        topic = topics.get(node.text) if node.children else None
        if topic is not None:
            for child in node.children:
                child.blanks = 0
            topic.children.extend(node.children)
            continue
        for child in node.children:
            child.blanks = 0
        if not merged:
            node.blanks = 0
        else:
            node.blanks = 1 if node.children or merged[-1].children else 0
        merged.append(node)
        if node.children:
            topics[node.text] = node
    return merged


def additions(fragments, component):
    """The entries for a component, from every fragment in order."""
    return [
        node for fragment in fragments for node in fragment.sections.get(component, [])
    ]


def unreleased_header(version):
    return f"## [v{version}] - unreleased"


def unreleased_section(sections, version, path):
    """The section that fragments merge into: the version's unreleased section,
    or None if the changelog has no section for the version yet."""
    for section in sections:
        if section.version == version:
            if not section.unreleased:
                raise _error(path, section.start + 1, f"v{version} is already released")
            return section
    return None


def fold(text, path, version, fragments, component):
    """Write a component's fragment entries into its CHANGELOG.md text, adding
    the unreleased section if it has none. Existing lines are kept as written."""
    nodes = additions(fragments, component)
    if not nodes:
        return text
    sections = parse_changelog(text, path)
    section = unreleased_section(sections, version, path)
    lines = text.splitlines()
    if section is None:
        start = sections[0].start if sections else len(lines)
        body = [unreleased_header(version)] + _render(merge([], nodes))
        if sections:
            body.append("")
        lines[start:start] = body
    else:
        trailing = 0
        while (
            section.end - 1 - trailing > section.start
            and not lines[section.end - 1 - trailing].strip()
        ):
            trailing += 1
        merged = merge(section.nodes, nodes)
        lines[section.start + 1 : section.end] = _render(merged) + [""] * trailing
    return "\n".join(lines) + "\n"


def _render(nodes):
    out = []
    for node in nodes:
        out.extend(node.render())
    return out


def strip_unreleased(text, version):
    """Mark a version's section as released."""
    return text.replace(unreleased_header(version), f"## [v{version}]")
