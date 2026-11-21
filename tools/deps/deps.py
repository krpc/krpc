"""Reports and updates the third-party dependency pins listed in deps.toml.

Usage:
  python3 tools/deps/deps.py check
  python3 tools/deps/deps.py outdated
  python3 tools/deps/deps.py update [NAME...] [--all] [--no-regen] [--to VERSION]
  python3 tools/deps/deps.py mods
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import os
import re
import subprocess
import sys
import urllib.request
from collections.abc import Callable
from dataclasses import dataclass, field
from pathlib import Path

import tomllib

# Resolved, so that under bazel test it is the source tree the runfiles link to
REPO_ROOT = Path(__file__).resolve().parents[2]
MANIFEST = Path("tools/deps/deps.toml")

# The lock file regenerators, in the order they run, as bazel commands run from the repo
# root. The upgrade form also moves the transitive pins to their newest releases. NuGet and
# Maven resolve every pin afresh, so they have one form.
PNPM = ["bazel", "run", "--", "@pnpm", "--dir", "{root}/client/js"]
REGENERATORS: dict[str, tuple[list[str], list[str]]] = {
    "pip": (
        ["bazel", "run", "//tools/build/python:requirements.update"],
        ["bazel", "run", "//tools/build/python:requirements.update", "--", "--upgrade"],
    ),
    "nuget": (
        ["bazel", "run", "//tools/build/dotnet:update"],
        ["bazel", "run", "//tools/build/dotnet:update"],
    ),
    "maven": (
        ["bazel", "run", "@unpinned_maven//:pin"],
        ["bazel", "run", "@unpinned_maven//:pin"],
    ),
    "pnpm": (
        [*PNPM, "install", "--lockfile-only"],
        [*PNPM, "update", "--lockfile-only"],
    ),
}

VERSION_RE = re.compile(r"^\d+(\.\d+)*$")
# {dep:NAME} in a source, the version of another dependency
DEP_REF_RE = re.compile(r"\{dep:([^}]+)\}")
PLACEHOLDER_RE = re.compile(r"\{(version|major|minor|patch|major_minor)(?::([^}]*))?\}")


def parse_version(version: str) -> tuple[int, ...]:
    return tuple(int(part) for part in version.split("."))


def is_release(version: str) -> bool:
    return VERSION_RE.match(version) is not None


def render(template: str, version: str) -> str:
    """Fills a template's placeholders from a version.

    {version} is the version, and {version:_} joins its parts with the separator after the
    colon. {major}, {minor}, {patch} and {major_minor} are its leading parts.
    """
    parts = version.split(".")

    def replace(match: re.Match[str]) -> str:
        name, sep = match.group(1), match.group(2)
        if name == "version":
            return version if sep is None else sep.join(parts)
        if name == "major_minor":
            return ".".join(parts[:2])
        index = {"major": 0, "minor": 1, "patch": 2}[name]
        if index >= len(parts):
            raise ValueError(
                f"version {version} has no {name} part, for template {template!r}"
            )
        return parts[index]

    return PLACEHOLDER_RE.sub(replace, template)


def anchor(template: str) -> str:
    """The literal text of a template before its first placeholder."""
    match = PLACEHOLDER_RE.search(template)
    return template[: match.start()] if match else template


def line_at(content: str, position: int) -> tuple[int, str]:
    """The number and stripped text of the line holding a position."""
    start = content.rfind("\n", 0, position) + 1
    end = content.find("\n", position)
    line = content[start : end if end >= 0 else len(content)]
    return content.count("\n", 0, position) + 1, line.strip()


def has_placeholder(template: str) -> bool:
    return PLACEHOLDER_RE.search(template) is not None


def _continues_before(content: str, position: int) -> bool:
    """Whether the text before a position is part of a version, as in 1.2 before .3."""
    before = content[max(0, position - 2) : position]
    return bool(re.search(r"(?:\w|\w[._-])$", before))


def _continues_after(content: str, position: int) -> bool:
    after = content[position : position + 2]
    return bool(re.match(r"\w|[._+-]\w", after))


def pin_spans(content: str, template: str, version: str) -> list[tuple[int, int]]:
    """The spans of the content holding the template rendered at the version.

    A placeholder at either end of the template matches a whole version only, so
    protobuf==7.{version} at 36.2 does not match protobuf==7.36.21.
    """
    rendered = render(template, version)
    leading = PLACEHOLDER_RE.match(template) is not None
    trailing = any(m.end() == len(template) for m in PLACEHOLDER_RE.finditer(template))
    spans = []
    start = content.find(rendered)
    while start >= 0:
        end = start + len(rendered)
        if not (leading and _continues_before(content, start)) and not (
            trailing and _continues_after(content, end)
        ):
            spans.append((start, end))
        start = content.find(rendered, start + 1)
    return spans


def calls(content: str) -> list[tuple[int, int]]:
    """The top-level calls of a Starlark or CMake file, each from the start of the line
    that opens it to its closing parenthesis. Brackets in strings and comments are
    skipped."""
    spans = []
    depth = 0
    start = 0
    i = 0
    while i < len(content):
        c = content[i]
        if c == "#":
            i = content.find("\n", i)
            if i < 0:
                break
            continue
        if c in "\"'":
            quote = c * 3 if content.startswith(c * 3, i) else c
            i += len(quote)
            while i < len(content) and not content.startswith(quote, i):
                i += 2 if content[i] == "\\" else 1
            i += len(quote)
            continue
        if c in "([{":
            if depth == 0 and c == "(":
                start = content.rfind("\n", 0, i) + 1
            depth += 1
        elif c in ")]}":
            depth -= 1
            if depth == 0 and c == ")":
                spans.append((start, i + 1))
        i += 1
    return spans


def enclosing_call(content: str, position: int) -> tuple[int, int] | None:
    return next(
        ((start, end) for start, end in calls(content) if start <= position < end), None
    )


# The hash of a download, in a Bazel repository rule or a CMake FetchContent_Declare
SHA256_RE = re.compile(r'(sha256 = "|SHA256=)[0-9a-f]{64}')


@dataclass
class Location:
    file: str
    text: str
    sha256: bool = False
    strict: bool = True
    generated: bool = False

    @staticmethod
    def parse(spec: str | dict) -> Location:
        if isinstance(spec, str):
            file, sep, text = spec.partition(": ")
            if not sep:
                raise ValueError(f"location {spec!r} is not 'file: text'")
            return Location(file, text)
        return Location(
            spec["file"],
            spec["text"],
            spec.get("sha256", False),
            spec.get("strict", True),
            spec.get("generated", False),
        )


@dataclass
class Verify:
    source: str
    version: str
    tag: str | None = None


@dataclass
class Dep:
    name: str
    version: str
    source: str | None = None
    tag: str | None = None
    below: str | None = None
    hold: str | None = None
    note: str | None = None
    locations: list[Location] = field(default_factory=list)
    verify: list[Verify] = field(default_factory=list)
    regen: list[str] = field(default_factory=list)

    @staticmethod
    def parse(table: dict) -> Dep:
        known = {
            "name",
            "version",
            "source",
            "tag",
            "below",
            "hold",
            "note",
            "at",
            "verify",
            "regen",
        }
        unknown = set(table) - known
        if unknown:
            raise ValueError(f"{table.get('name')}: unknown keys {sorted(unknown)}")
        for regen in table.get("regen", []):
            if regen not in REGENERATORS:
                raise ValueError(f"{table['name']}: unknown regenerator {regen!r}")
        return Dep(
            name=table["name"],
            version=table["version"],
            source=table.get("source"),
            tag=table.get("tag"),
            below=table.get("below"),
            hold=table.get("hold"),
            note=table.get("note"),
            locations=[Location.parse(spec) for spec in table.get("at", [])],
            verify=[
                Verify(v["source"], v["version"], v.get("tag"))
                for v in table.get("verify", [])
            ],
            regen=table.get("regen", []),
        )

    def accepts(self, version: str) -> bool:
        if not is_release(version):
            return False
        return self.below is None or parse_version(version) < parse_version(self.below)


def load_manifest(root: Path) -> list[Dep]:
    with open(root / MANIFEST, "rb") as f:
        data = tomllib.load(f)
    deps = [Dep.parse(table) for table in data.get("dep", [])]
    names = [dep.name for dep in deps]
    duplicates = {name for name in names if names.count(name) > 1}
    if duplicates:
        raise ValueError(f"duplicate names in {MANIFEST}: {sorted(duplicates)}")
    return deps


def expand(root: Path, pattern: str) -> list[Path]:
    """The files a location names. A pattern with a * is a glob."""
    if "*" in pattern:
        return sorted(path for path in root.glob(pattern) if path.is_file())
    return [root / pattern]


# ----------------------------------------------------------------------------------------
# check


def check_location(root: Path, dep: Dep, location: Location) -> list[str]:
    """The problems with one location of a dependency at its manifest version."""
    files = expand(root, location.file)
    if not files or not all(path.exists() for path in files):
        return [f"{dep.name}: {location.file} not found"]
    rendered = render(location.text, dep.version)
    prefix = anchor(location.text)
    errors = []
    found = False

    def error(path: Path, content: str, position: int, problem: str) -> None:
        line_number, line = line_at(content, position)
        relative = path.relative_to(root).as_posix()
        errors.append(f"{dep.name}: {relative}:{line_number} {line!r} {problem}")

    for path in files:
        content = path.read_text(encoding="utf-8")
        spans = pin_spans(content, location.text, dep.version)
        found = found or bool(spans)
        if location.strict and prefix and prefix != location.text:
            starts = {start for start, _ in spans}
            for match in re.finditer(re.escape(prefix), content):
                if match.start() not in starts:
                    error(path, content, match.start(), f"is not {rendered!r}")
        if location.sha256:
            for start, _ in spans:
                call = enclosing_call(content, start)
                block = content[call[0] : call[1]] if call else ""
                if len(SHA256_RE.findall(block)) != 1:
                    error(path, content, start, "is not in a call with one sha256")
    if not found and not errors:
        errors.append(f"{dep.name}: {location.file} does not contain {rendered!r}")
    return errors


def check(root: Path, deps: list[Dep]) -> list[str]:
    errors = []
    for dep in deps:
        for location in dep.locations:
            errors.extend(check_location(root, dep, location))
    return errors


# The forms a pin takes in each kind of file. Each pin must be part of a location in the
# manifest, so a dependency cannot be added without an entry. A call with a sha256 must
# also hold a sha256 location, as an update rewrites the hash.
STARLARK = ("MODULE.bazel", "**/BUILD.bazel", "**/*.bzl")
CMAKE = ("**/CMakeLists.txt*", "**/*.cmake")
PIN_FORMS: list[tuple[tuple[str, ...], re.Pattern[str]]] = [
    (STARLARK, re.compile(r'bazel_dep\(name = "[^"]+", version = "[^"]+"')),
    (STARLARK, re.compile(r"\bnuget\.[a-z0-9._-]+?\.v\d[\w.+-]*")),
    (STARLARK, re.compile(r'\b\w*version = "\d[^"]*"')),
    (STARLARK, re.compile(r'"[\w.-]+:[\w.-]+:\d[^"]*"')),
    (STARLARK, re.compile(r'"[A-Z][\w.]*": "\d[^"]*"')),
    (
        ("**/*.csproj",),
        re.compile(r'<PackageReference Include="[^"]+" Version="[^"]+"'),
    ),
    (("**/paket.dependencies",), re.compile(r"(?m)^[ \t]*nuget \S+ \d\S*")),
    (("**/requirements.in",), re.compile(r"(?m)^[\w.-]+==\S+")),
    (("**/package.json",), re.compile(r'"(?!version")[@\w/.-]+": "[\^~>=<]*\d[^"]*"')),
    ((".github/**/*.yml",), re.compile(r"uses: (?!\./)\S+@\S+|ghcr\.io/\S+:\S+")),
    (CMAKE, re.compile(r"\b(?:GIT_TAG|URL)\s+\S+|find_package\(\w+ \d[\d.]*")),
    (("**/pyproject.toml",), re.compile(r'"[\w.-]+ *[<>=~!]=? *\d[^"]*"')),
    (("**/pyproject.toml",), re.compile(r'requires-python = "[^"]*"')),
    (("**/rockspec.tmpl",), re.compile(r'"[\w.-]+ *[<>=~]=? *\d[^"]*"')),
    (("**/vcpkg.json",), re.compile(r'"version>?=": "[^"]*"')),
    (("**/Dockerfile",), re.compile(r"(?m)^FROM \S+|https?://\S*\d\S*")),
    ((".bazelversion",), re.compile(r"\S+")),
    ((".bazelrc",), re.compile(r"_version=\S+")),
]

# Matches of the forms above that are not dependencies
NOT_PINS: list[tuple[str, re.Pattern[str]]] = [
    ("config.bzl", re.compile(r'(?m)^version = "')),  # kRPC's own version
    ("tools/krpctools/pyproject.toml", re.compile(r'"krpc >= ')),  # set by the build
    # kRPC for KSA's own version, the kRPC client release it works with, and the KSA build,
    # which is closed source and has no release to check. 01-update-ksa.py moves the KSA build.
    ("experimental/ksa/config.bzl", re.compile(r'(?m)^(mod|client|ksa)_version = "')),
]

# Not scanned: lock files the regenerators write, and the KSP mods (the mods command and
# service/registry.bzl's own checks cover them)
SKIPPED_FILES = {
    "service/registry.bzl",
    "tools/build/dotnet/paket.main.bzl",
    "tools/build/dotnet/paket.tools.bzl",
}


def source_files(root: Path) -> list[str]:
    """The paths of the repository's files relative to the root, tracked or new, and
    neither ignored nor deleted. Without git, every file outside the hidden and
    output directories."""
    try:
        output = subprocess.run(
            ["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"],
            cwd=root,
            check=True,
            capture_output=True,
            text=True,
        ).stdout
        paths = output.split("\0")
    except (OSError, subprocess.CalledProcessError):
        paths = []
        for directory, subdirs, files in os.walk(root):
            subdirs[:] = [
                d
                for d in subdirs
                if d == ".github"
                or not (d.startswith((".", "bazel-")) or d == "node_modules")
            ]
            relative = Path(directory).relative_to(root)
            paths.extend((relative / name).as_posix() for name in files)
    return [path for path in paths if path and (root / path).is_file()]


def untracked(root: Path, deps: list[Dep]) -> list[str]:
    """The pins in the repository that no manifest location covers."""
    covered: dict[str, list[tuple[int, int, bool]]] = {}
    for dep in deps:
        for location in dep.locations:
            for path in expand(root, location.file):
                if not path.exists():
                    continue
                content = path.read_text(encoding="utf-8")
                relative = path.relative_to(root).as_posix()
                covered.setdefault(relative, []).extend(
                    (start, end, location.sha256)
                    for start, end in pin_spans(content, location.text, dep.version)
                )
    errors = []
    for relative in source_files(root):
        if relative not in SKIPPED_FILES:
            errors.extend(untracked_in_file(root, relative, covered.get(relative, [])))
    return errors


def untracked_in_file(
    root: Path, relative: str, covered: list[tuple[int, int, bool]]
) -> list[str]:
    """The pins in one file outside the covered spans. Each covered span records whether
    it is a sha256 location."""
    forms = [
        form
        for patterns, form in PIN_FORMS
        if any(glob_match(relative, pattern) for pattern in patterns)
    ]
    has_calls = any(glob_match(relative, pattern) for pattern in STARLARK + CMAKE)
    if not forms and not has_calls:
        return []
    content = (root / relative).read_text(encoding="utf-8")
    exempt = {
        m.start()
        for file, pattern in NOT_PINS
        if file == relative
        for m in pattern.finditer(content)
    }
    problems: dict[int, str] = {}
    for form in forms:
        for match in form.finditer(content):
            if match.start() not in exempt and not any(
                s < match.end() and match.start() < e for s, e, _ in covered
            ):
                problems.setdefault(match.start(), f"is not in {MANIFEST}")
    if has_calls:
        for start, end in calls(content):
            block = SHA256_RE.search(content, start, end)
            if block and not any(sha and start <= s < end for s, _, sha in covered):
                problems.setdefault(
                    block.start(), f"has no sha256 location in {MANIFEST}"
                )
    errors = {}
    for position, problem in sorted(problems.items()):
        number, line = line_at(content, position)
        errors.setdefault(number, f"{relative}:{number} {line!r} {problem}")
    return list(errors.values())


def glob_match(path: str, pattern: str) -> bool:
    """Whether a relative path matches a glob, where ** spans directories and a pattern
    with no / matches the file name in any directory."""
    if "/" not in pattern:
        return re.fullmatch(_glob_regex(pattern), path.rpartition("/")[2]) is not None
    return re.fullmatch(_glob_regex(pattern), path) is not None


def _glob_regex(pattern: str) -> str:
    regex = ""
    i = 0
    while i < len(pattern):
        if pattern.startswith("**/", i):
            regex += "(?:.*/)?"
            i += 3
        elif pattern[i] == "*":
            regex += "[^/]*"
            i += 1
        else:
            regex += re.escape(pattern[i])
            i += 1
    return regex


# ----------------------------------------------------------------------------------------
# Upstream versions

Fetch = Callable[[str], bytes]


def http_fetch(url: str) -> bytes:
    request = urllib.request.Request(url, headers={"User-Agent": "krpc-deps"})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read()


def gh_api(path: str) -> list | dict:
    output = subprocess.run(
        ["gh", "api", "--paginate", "--slurp", path],
        check=True,
        capture_output=True,
        text=True,
    ).stdout
    pages = json.loads(output)
    if pages and isinstance(pages[0], list):
        return [item for page in pages for item in page]
    return pages[0]


def tag_version(tag: str, pattern: str | None) -> str | None:
    """The version a tag names. The pattern's first group is the version, with _ and - as
    separators."""
    match = re.fullmatch(pattern or r"v?(\d+(?:\.\d+)*)", tag)
    if not match:
        return None
    return re.sub(r"[_-]", ".", match.group(1))


def supports_python(files: list[dict], python: str) -> bool:
    """Whether a PyPI release supports the Python version, by the Requires-Python of its
    first file. Clauses other than comparisons with a plain version are ignored."""
    spec = files[0].get("requires_python") or ""
    target = parse_version(python)
    for clause in spec.split(","):
        match = re.fullmatch(r"\s*(>=|>|<=|<|==|!=)\s*(\d+(?:\.\d+)*)\s*", clause)
        if not match:
            continue
        op, version = match.group(1), parse_version(match.group(2))
        # Compare at the precision of the clause, so 3.12 satisfies >=3.12.0
        current = target[: len(version)] + (0,) * (len(version) - len(target))
        ok = {
            ">=": current >= version,
            ">": current > version,
            "<=": current <= version,
            "<": current < version,
            "==": current == version,
            "!=": current != version,
        }[op]
        if not ok:
            return False
    return True


class Sources:
    """Looks up the released versions of a source, written as kind:id."""

    def __init__(
        self,
        fetch: Fetch = http_fetch,
        gh: Callable[[str], list | dict] = gh_api,
        pins: dict[str, str] | None = None,
    ):
        self.fetch = fetch
        self.gh = gh
        self.pins = pins or {}
        self.cache: dict[tuple[str, str | None], list[str]] = {}

    def json(self, url: str) -> dict | list:
        data = self.fetch(url)
        # The NuGet registration is served gzipped whatever the request asks for
        if data.startswith(b"\x1f\x8b"):
            data = gzip.decompress(data)
        return json.loads(data)

    def versions(self, source: str, tag: str | None = None) -> list[str]:
        source = DEP_REF_RE.sub(lambda m: self.pins[m.group(1)], source)
        key = (source, tag)
        if key not in self.cache:
            self.cache[key] = self.lookup(source, tag)
        return self.cache[key]

    def lookup(self, source: str, tag: str | None) -> list[str]:
        kind, _, ident = source.partition(":")
        if kind == "bcr":
            data = self.json(f"https://bcr.bazel.build/modules/{ident}/metadata.json")
            assert isinstance(data, dict)
            yanked = data.get("yanked_versions", {})
            return [v for v in data["versions"] if v not in yanked]
        if kind == "pypi":
            data = self.json(f"https://pypi.org/pypi/{ident}/json")
            assert isinstance(data, dict)
            python = self.pins.get("python")
            return [
                v
                for v, files in data["releases"].items()
                if files
                and not all(f["yanked"] for f in files)
                and (python is None or supports_python(files, python))
            ]
        if kind == "npm":
            data = self.json(f"https://registry.npmjs.org/{ident}")
            assert isinstance(data, dict)
            return list(data["versions"])
        if kind == "nuget":
            # The registration lists whether each version is listed. A publisher
            # unlists a broken release.
            index = self.json(
                f"https://api.nuget.org/v3/registration5-gz-semver2/{ident.lower()}/index.json"
            )
            assert isinstance(index, dict)
            versions = []
            for page in index["items"]:
                if "items" not in page:
                    page = self.json(page["@id"])
                    assert isinstance(page, dict)
                for leaf in page["items"]:
                    entry = leaf["catalogEntry"]
                    if entry.get("listed", True):
                        versions.append(entry["version"])
            return versions
        if kind == "maven":
            group, artifact = ident.split(":")
            url = f"https://repo1.maven.org/maven2/{group.replace('.', '/')}/{artifact}/maven-metadata.xml"
            return re.findall(r"<version>([^<]+)</version>", self.fetch(url).decode())
        if kind == "github-release":
            releases = self.gh(f"repos/{ident}/releases?per_page=100")
            assert isinstance(releases, list)
            tags = [
                r["tag_name"]
                for r in releases
                if not r["draft"] and not r["prerelease"]
            ]
            return [v for v in (tag_version(t, tag) for t in tags) if v]
        if kind == "github-tag":
            tags = self.gh(f"repos/{ident}/tags?per_page=100")
            assert isinstance(tags, list)
            return [v for v in (tag_version(t["name"], tag) for t in tags) if v]
        if kind == "node":
            data = self.json("https://nodejs.org/dist/index.json")
            assert isinstance(data, list)
            return [entry["version"].lstrip("v") for entry in data]
        if kind == "dotnet-sdk":
            url = f"https://builds.dotnet.microsoft.com/dotnet/release-metadata/{ident}/releases.json"
            data = self.json(url)
            assert isinstance(data, dict)
            return [
                sdk["version"]
                for release in data["releases"]
                for sdk in release.get("sdks", [])
            ]
        if kind == "lua":
            html = self.fetch("https://www.lua.org/ftp/").decode()
            return sorted(set(re.findall(r"lua-(\d+\.\d+\.\d+)\.tar\.gz", html)))
        if kind == "docker":
            # Docker Hub orders tags by when they were pushed, so read every page
            url = f"https://hub.docker.com/v2/repositories/{ident}/tags?page_size=100"
            names = []
            while url:
                data = self.json(url)
                assert isinstance(data, dict)
                names.extend(r["name"] for r in data["results"])
                url = data.get("next")
            return [v for v in (tag_version(n, tag) for n in names) if v]
        if kind == "regex":
            if tag is None:
                raise ValueError(f"{source} needs a tag regex")
            content = self.fetch(ident).decode()
            return [re.sub(r"[_-]", ".", m) for m in re.findall(tag, content)]
        raise ValueError(f"unknown source kind {kind!r} in {source!r}")


@dataclass
class Status:
    dep: Dep
    target: str | None
    newest: str | None
    error: str | None = None

    @property
    def outdated(self) -> bool:
        return self.target is not None and parse_version(self.target) > parse_version(
            self.dep.version
        )

    @property
    def capped(self) -> bool:
        """Whether the newest release is past the target, held back by below or verify."""
        return (
            self.newest is not None
            and self.target is not None
            and parse_version(self.newest) > parse_version(self.target)
        )


def verified(sources: Sources, dep: Dep, version: str) -> bool:
    for v in dep.verify:
        if render(v.version, version) not in sources.versions(v.source, v.tag):
            return False
    return True


def status(sources: Sources, dep: Dep) -> Status:
    """The newest version the dependency accepts, and the newest release of its source."""
    if dep.source is None:
        return Status(dep, None, None)
    try:
        versions = [v for v in sources.versions(dep.source, dep.tag) if is_release(v)]
        if not versions:
            return Status(dep, None, None, f"no releases found for {dep.source}")
        newest = max(versions, key=parse_version)
        current = parse_version(dep.version)
        candidates = sorted(
            (v for v in versions if dep.accepts(v) and parse_version(v) >= current),
            key=parse_version,
            reverse=True,
        )
        target = next((v for v in candidates if verified(sources, dep, v)), dep.version)
        return Status(dep, target, newest)
    except Exception as e:  # noqa: BLE001 - one failed lookup must not stop the report
        return Status(dep, None, None, f"{type(e).__name__}: {e}")


def describe(st: Status) -> str:
    if st.error:
        return f"error: {st.error}"
    if st.dep.source is None:
        return "no source"
    words = []
    if st.outdated:
        words.append(f"-> {st.target}")
    if st.capped:
        words.append(f"(newest {st.newest}, outside 'below' or 'verify')")
    if st.dep.hold:
        words.append(f"held: {st.dep.hold}")
    return " ".join(words) or "ok"


# ----------------------------------------------------------------------------------------
# update


def replace_sha256(content: str, positions: list[int], digest: str) -> str:
    """Replaces the sha256 of each call that holds one of the positions."""
    for position in positions:
        call = enclosing_call(content, position)
        if call is None:
            raise ValueError(f"no call holds the download at {position}")
        start, end = call
        block, count = SHA256_RE.subn(lambda m: m.group(1) + digest, content[start:end])
        if count != 1:
            line = content.count("\n", 0, position) + 1
            raise ValueError(
                f"expected one sha256 in the call at line {line}, found {count}"
            )
        content = content[:start] + block + content[end:]
    return content


def set_manifest_version(manifest: str, name: str, old: str, new: str) -> str:
    pattern = re.compile(
        rf'(\[\[dep\]\]\s*\nname = "{re.escape(name)}"\n(?:(?!\[\[dep\]\]).*\n)*?version = "){re.escape(old)}"'
    )
    result, count = pattern.subn(rf'\g<1>{new}"', manifest)
    if count != 1:
        raise ValueError(f"cannot find the version line of {name} in {MANIFEST}")
    return result


def plan_update(
    root: Path, updates: list[tuple[Dep, str]], fetch: Fetch = http_fetch
) -> dict[Path, str]:
    """The new content of each file the updates change. Raises before any file is written."""
    contents: dict[Path, str] = {}
    digests: dict[str, str] = {}

    def read(path: Path) -> str:
        if path not in contents:
            contents[path] = path.read_text(encoding="utf-8")
        return contents[path]

    for dep, new in updates:
        if not any(
            has_placeholder(loc.text) and not loc.generated for loc in dep.locations
        ):
            raise ValueError(f"{dep.name} has no versioned locations to update")
        for location in dep.locations:
            if location.generated or not has_placeholder(location.text):
                continue
            old_text = render(location.text, dep.version)
            new_text = render(location.text, new)
            matched = False
            for path in expand(root, location.file):
                content = read(path)
                spans = pin_spans(content, location.text, dep.version)
                if not spans:
                    continue
                matched = True
                for start, end in reversed(spans):
                    content = content[:start] + new_text + content[end:]
                if location.sha256:
                    if new_text not in digests:
                        print(f"  downloading {new_text}")
                        digests[new_text] = hashlib.sha256(fetch(new_text)).hexdigest()
                    digest = digests[new_text]
                    positions = [s for s, _ in pin_spans(content, location.text, new)]
                    content = replace_sha256(content, positions, digest)
                contents[path] = content
            if not matched:
                raise ValueError(
                    f"{dep.name}: {location.file} does not contain {old_text!r}"
                )
        manifest = root / MANIFEST
        contents[manifest] = set_manifest_version(
            read(manifest), dep.name, dep.version, new
        )
    return contents


def regenerate(root: Path, regens: set[str], upgrade: bool) -> None:
    for name, (plain, upgraded) in REGENERATORS.items():
        if name in regens:
            command = [
                arg.format(root=root) for arg in (upgraded if upgrade else plain)
            ]
            env = dict(os.environ)
            if name == "maven":
                env["RULES_JVM_EXTERNAL_REPIN"] = "1"
            print(f"regenerating {name}: {' '.join(command)}")
            subprocess.run(command, cwd=root, env=env, check=True)


# ----------------------------------------------------------------------------------------
# KSP mods


@dataclass
class ModPin:
    name: str
    repo: str
    version: str


def parse_mods(registry: str) -> list[ModPin]:
    """The latest pinned version of each mod in service/registry.bzl with a GitHub repo."""
    pins = []
    for block in re.split(r"\n    mod\(\n", registry)[1:]:
        name = re.search(r'^\s*name = "([^"]+)"', block, re.M)
        repo = re.search(r'^\s*repo = "([^"]+)"', block, re.M)
        version = re.search(r'versions = \[version\(\s*"([^"]+)"', block)
        if name and repo and version:
            pins.append(ModPin(name.group(1), repo.group(1), version.group(1)))
    return pins


def mod_release_is_newer(tag: str, pinned: str) -> bool:
    """Whether a release tag names a later version than the pin. Mods number their releases
    in many ways, so this compares the numbers in each."""
    return tuple(int(n) for n in re.findall(r"\d+", tag)) > tuple(
        int(n) for n in re.findall(r"\d+", pinned)
    )


def report_mods(root: Path) -> None:
    registry = (root / "service/registry.bzl").read_text(encoding="utf-8")
    for pin in parse_mods(registry):
        try:
            latest = gh_api(f"repos/{pin.repo}/releases/latest")
            assert isinstance(latest, dict)
            tag = latest["tag_name"]
        except FileNotFoundError:
            sys.exit("deps.py mods needs the GitHub CLI, gh")
        except subprocess.CalledProcessError:
            tag = None
        if tag is None:
            state = "no GitHub release"
        elif mod_release_is_newer(tag, pin.version):
            state = f"latest release {tag}"
        else:
            state = "ok"
        print(f"{pin.name:28} {pin.version:16} {state}")


# ----------------------------------------------------------------------------------------


def print_table(rows: list[tuple[str, str, str]]) -> None:
    width = max((len(r[0]) for r in rows), default=0)
    vwidth = max((len(r[1]) for r in rows), default=0)
    for name, version, state in rows:
        print(f"{name:{width}}  {version:{vwidth}}  {state}")


def lookup_order(deps: list[Dep]) -> list[Dep]:
    """The dependencies, each after the ones its sources name with {dep:NAME}."""
    by_name = {dep.name: dep for dep in deps}
    ordered: list[Dep] = []
    seen: set[str] = set()

    def visit(dep: Dep) -> None:
        if dep.name in seen:
            return
        seen.add(dep.name)
        sources = [dep.source or "", *(v.source for v in dep.verify)]
        for name in DEP_REF_RE.findall(" ".join(sources)):
            if name in by_name:
                visit(by_name[name])
        ordered.append(dep)

    for dep in deps:
        visit(dep)
    return ordered


def statuses(
    sources: Sources, deps: list[Dep], moves: Callable[[Dep], bool]
) -> dict[str, Status]:
    """The status of each dependency. A source that names another dependency uses the
    version that dependency updates to, when moves says it is updated in the same run."""
    result = {}
    for dep in lookup_order(deps):
        st = status(sources, dep)
        result[dep.name] = st
        if st.outdated and moves(dep):
            assert st.target is not None
            sources.pins[dep.name] = st.target
    return result


def find_updates(sources: Sources, selected: list[Dep]) -> list[tuple[Dep, str]] | None:
    """The new version of each outdated dependency, or None after a failed lookup."""
    found = statuses(sources, selected, lambda dep: True)
    updates = []
    for dep in selected:
        st = found[dep.name]
        if st.error:
            print(f"{dep.name}: {st.error}", file=sys.stderr)
            return None
        if st.outdated:
            assert st.target is not None
            print(f"{dep.name}: {dep.version} -> {st.target}")
            if dep.note:
                print(f"  note: {dep.note}")
            updates.append((dep, st.target))
    return updates


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "--root", type=Path, default=REPO_ROOT, help="the repository root"
    )
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("check", help="check that each pin agrees with deps.toml")
    outdated = commands.add_parser(
        "outdated", help="list the dependencies with newer releases"
    )
    outdated.add_argument(
        "--all", action="store_true", help="also list the up-to-date dependencies"
    )
    update = commands.add_parser(
        "update", help="update dependencies to their newest accepted releases"
    )
    update.add_argument(
        "names", nargs="*", help="the dependencies to update, including held ones"
    )
    update.add_argument(
        "--all",
        action="store_true",
        help="update every dependency that is not held, and every transitive pin",
    )
    update.add_argument(
        "--no-regen", action="store_true", help="skip regenerating the lock files"
    )
    update.add_argument(
        "--to",
        metavar="VERSION",
        help="the version to update the one named dependency to",
    )
    commands.add_parser("mods", help="list the KSP mods with newer GitHub releases")
    args = parser.parse_args(argv)
    root: Path = args.root.absolute()
    deps = load_manifest(root)

    if args.command == "check":
        errors = check(root, deps) + untracked(root, deps)
        for error in errors:
            print(error)
        if not errors:
            print(f"{len(deps)} dependencies, all pins agree with {MANIFEST}")
        return 1 if errors else 0

    if args.command == "mods":
        report_mods(root)
        return 0

    sources = Sources(pins={dep.name: dep.version for dep in deps})

    if args.command == "outdated":
        found = statuses(sources, deps, lambda dep: dep.hold is None)
        rows = []
        failed = False
        for dep in deps:
            st = found[dep.name]
            failed |= st.error is not None
            if args.all or st.outdated or st.capped or st.error:
                rows.append((dep.name, dep.version, describe(st)))
        print_table(rows)
        return 1 if failed else 0

    by_name = {dep.name: dep for dep in deps}
    unknown = [name for name in args.names if name not in by_name]
    if unknown:
        parser.error(f"unknown dependencies: {', '.join(unknown)}")
    if bool(args.names) == args.all:
        parser.error("name the dependencies to update or pass --all, not both")
    if args.to is not None and len(args.names) != 1:
        parser.error("--to takes exactly one dependency")
    if args.to is not None:
        updates = [(by_name[args.names[0]], args.to)]
    else:
        selected = [by_name[name] for name in args.names] or [
            dep for dep in deps if dep.hold is None
        ]
        updates = find_updates(sources, selected)
        if updates is None:
            return 1
    for path, content in plan_update(root, updates).items():
        path.write_text(content, encoding="utf-8", newline="\n")
    if not updates:
        print("no pins to update")
    if args.no_regen:
        return 0
    if args.all:
        # Every lock file, so their transitive pins move too
        regenerate(root, set(REGENERATORS), upgrade=True)
    else:
        regenerate(
            root, {regen for dep, _ in updates for regen in dep.regen}, upgrade=False
        )
    return 0


if __name__ == "__main__":
    sys.exit(main())
