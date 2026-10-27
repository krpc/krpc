"""Generates the requirement lists of the client specification from the conformance tests.

  specgen.py requirements OUTDIR TESTS...
  specgen.py coverage OUT [--client NAME PROFILES EXPECTED_FAILURES UNSUPPORTED]... TESTS...

requirements writes OUTDIR/<area>.txt for each tests/test_<area>.py, for a specification page
to include. coverage writes a table of each requirement against each client, from the client's
profiles file, expected failures file and unsupported file.

The tests are read, not imported. Each test is a requirement: its id is the area and the test's
name, its text is the docstring, and its level and profiles come from its markers. A protocol
is a profile. A module level section("Title", "text") call starts a section.
"""

from __future__ import annotations

import argparse
import ast
import inspect
import os
from dataclasses import dataclass, field

LEVELS = {"should": "SHOULD"}


@dataclass
class Requirement:
    id: str
    text: str
    level: str = "MUST"
    profiles: list[str] = field(default_factory=list)
    untested: str | None = None


@dataclass
class Section:
    title: str | None
    text: str = ""
    requirements: list[Requirement] = field(default_factory=list)


def area_of(path: str) -> str:
    return os.path.basename(path).removeprefix("test_").removesuffix(".py")


def read_area(path: str) -> list[Section]:
    """The sections of an area's tests, in file order."""
    with open(path, encoding="utf-8") as source:
        tree = ast.parse(source.read(), path)
    area = area_of(path)
    sections = [Section(None)]
    for node in tree.body:
        if (
            isinstance(node, ast.Expr)
            and isinstance(node.value, ast.Call)
            and _name(node.value.func) == "section"
        ):
            args = [ast.literal_eval(x) for x in node.value.args]
            kwargs = {
                str(x.arg): ast.literal_eval(x.value) for x in node.value.keywords
            }
            sections.append(Section(*args, **kwargs))
        elif isinstance(node, ast.FunctionDef) and node.name.startswith("test_"):
            sections[-1].requirements.append(_requirement(path, area, node))
    return [x for x in sections if x.requirements]


def _requirement(path: str, area: str, node: ast.FunctionDef) -> Requirement:
    docstring = ast.get_docstring(node)
    if not docstring:
        raise SystemExit("%s: %s has no docstring" % (path, node.name))
    requirement = Requirement(
        "%s-%s" % (area, node.name.removeprefix("test_").replace("_", "-")),
        inspect.cleandoc(docstring),
    )
    for decorator in node.decorator_list:
        name = _name(decorator.func if isinstance(decorator, ast.Call) else decorator)
        if name in LEVELS:
            requirement.level = LEVELS[name]
        elif name in ("profile", "protocol") and isinstance(decorator, ast.Call):
            requirement.profiles += [ast.literal_eval(x) for x in decorator.args]
        elif name == "untested" and isinstance(decorator, ast.Call):
            requirement.untested = ast.literal_eval(decorator.args[0])
    return requirement


def _name(node: ast.expr) -> str | None:
    if isinstance(node, ast.Name):
        return node.id
    if isinstance(node, ast.Attribute):
        return node.attr
    return None


def render(sections: list[Section]) -> str:
    """The rst for an area's requirements."""
    out = []
    for section in sections:
        if section.title:
            out.append("%s\n%s\n" % (section.title, "-" * len(section.title)))
        if section.text:
            out.append(section.text + "\n")
        out.extend(_render_requirement(x) for x in section.requirements)
    return "\n".join(out)


def _render_requirement(requirement: Requirement) -> str:
    """A requirement's id, then the profiles a client needs to run its test, then its level.
    A requirement that names no profile is in the core profile, and a profile named "!<name>"
    excludes the clients that implement it."""
    needed = [x for x in requirement.profiles if not x.startswith("!")]
    excluded = ["without " + x[1:] for x in requirement.profiles if x.startswith("!")]
    profiles = ", ".join((needed or ["core"]) + excluded)
    body = "\n".join(("   " + x) if x else "" for x in requirement.text.split("\n"))
    return ".. _spec-%s:\n\n``%s [%s] [%s]``\n%s\n" % (
        requirement.id,
        requirement.id,
        profiles,
        requirement.level,
        body,
    )


@dataclass
class Client:
    """A client's profiles and protocols, and its listed tests. A listed test maps to the
    protocols its lines name, with "" for a line that names none."""

    name: str
    profiles: set[str]
    expected_failures: dict[str, set[str]]
    unsupported: dict[str, set[str]] = field(default_factory=dict)

    @staticmethod
    def read(
        name: str, profiles: str, expected_failures: str, unsupported: str
    ) -> Client:
        return Client(
            name,
            set(_read_listing(profiles)),
            _read_listing(expected_failures),
            _read_listing(unsupported),
        )

    def status(self, path: str, node: str, requirement: Requirement) -> str:
        """Whether the client passes the test of a requirement."""
        if requirement.untested is not None:
            return "untested"
        needed = {x for x in requirement.profiles if not x.startswith("!")}
        excluded = {x[1:] for x in requirement.profiles if x.startswith("!")}
        if not needed <= self.profiles or excluded & self.profiles:
            return "unsupported"
        test = "%s::%s" % (os.path.basename(path), node)
        if "" in self.unsupported.get(test, set()):
            return "unsupported"
        if "" in self.expected_failures.get(test, set()):
            return "fail"
        # A test that fails over some protocols, or for some of its parameters
        listed = self.expected_failures
        if any(x == test or x.startswith(test + "[") for x in listed):
            return "partial"
        return "pass"


# The symbol for each status, in the coverage roles coverage.rst defines
STATUS_CELLS = {
    "pass": ":coverage-pass:`\u2713`",
    "fail": ":coverage-fail:`\u2717`",
    "partial": ":coverage-partial:`!`",
    "untested": ":coverage-untested:`?`",
    "unsupported": "",
}


def _read_listing(path: str) -> dict[str, set[str]]:
    """The names a file lists, each with the protocols its lines name, without comments,
    flags or blank lines."""
    listed: dict[str, set[str]] = {}
    with open(path, encoding="utf-8") as listing:
        lines = (x.partition("#")[0].strip() for x in listing)
        for line in lines:
            line = line.removesuffix(" @flaky").removesuffix(" @skip")
            if line:
                test, _, protocol = line.partition("@")
                listed.setdefault(test.strip(), set()).add(protocol.strip())
    return listed


def render_coverage(paths: list[str], clients: list[Client]) -> str:
    """The rst for the table of each requirement against each client."""
    out = [
        ".. list-table:: Profiles\n   :header-rows: 1\n\n",
        "   * - Client\n     - Profiles\n",
    ]
    for client in clients:
        names = ", ".join("``%s``" % x for x in sorted(client.profiles))
        out.append("   * - %s\n     - %s\n" % (client.name, names))
    out.append("\n")
    for path in paths:
        area = area_of(path)
        out.append(".. list-table:: %s\n" % area.capitalize())
        out.append("   :header-rows: 1\n   :stub-columns: 1\n   :class: coverage\n\n")
        out.append("   * - Requirement\n")
        out.extend("     - %s\n" % x.name for x in clients)
        for section in read_area(path):
            for requirement in section.requirements:
                node = "test_" + requirement.id.removeprefix(area + "-").replace(
                    "-", "_"
                )
                out.append(
                    "   * - :ref:`%s <spec-%s>`\n" % (requirement.id, requirement.id)
                )
                for client in clients:
                    cell = STATUS_CELLS[client.status(path, node, requirement)]
                    out.append(("     - " + cell).rstrip() + "\n")
        out.append("\n")
    return "".join(out).rstrip("\n") + "\n"


def main() -> None:
    parser = argparse.ArgumentParser()
    commands = parser.add_subparsers(dest="command", required=True)
    requirements = commands.add_parser("requirements")
    requirements.add_argument("outdir")
    requirements.add_argument("tests", nargs="+")
    coverage = commands.add_parser("coverage")
    coverage.add_argument("out")
    coverage.add_argument("--client", nargs=4, action="append", default=[])
    coverage.add_argument("tests", nargs="+")
    args = parser.parse_args()
    if args.command == "requirements":
        os.makedirs(args.outdir, exist_ok=True)
        for path in args.tests:
            out_path = os.path.join(args.outdir, area_of(path) + ".txt")
            with open(out_path, "w", encoding="utf-8") as out:
                out.write(render(read_area(path)))
    else:
        clients = [Client.read(*x) for x in args.client]
        with open(args.out, "w", encoding="utf-8") as out:
            out.write(render_coverage(args.tests, clients))


if __name__ == "__main__":
    main()
