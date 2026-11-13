"""Checks the registry, service/registry.bzl, and the hand-written lists of services against it.

The builds outside Bazel need the .csproj files and KRPC.sln as written. GitHub renders the root
CHANGELOG.md, and the changelog-hints workflow reads COMPONENTS in tools/release/changelog.py
without a build. The rest of the repository generates its lists from the registry.
"""

import json
import re
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

import changelog

REGISTRY = json.loads(Path("service/registry.json").read_text(encoding="utf-8"))
SERVICES = REGISTRY["services"]
MODS = {mod["name"]: mod for mod in REGISTRY["mods"]}
CORE_SERVICES = {"KRPC", "StdLib"}
BY_NAME = {service["name"]: service for service in SERVICES}

# A KRPCService attribute and its Id argument
SERVICE_ATTRIBUTE = re.compile(r"\[KRPCService\s*\(([^)]*)\)\]")


def csproj(service):
    return Path(
        "service/%s/src/KRPC.%s.csproj" % (service["package"], service["package"])
    )


def items(path, tag) -> list[str]:
    """The Include attribute of each item of a kind in a .csproj."""
    return [
        element.get("Include", "")
        for element in ET.parse(path).iter()
        if element.tag.rsplit("}", 1)[-1] == tag
    ]


class TestRegistry(unittest.TestCase):
    def test_csproj_compiles_service_info(self):
        for service in SERVICES:
            with self.subTest(service=service["name"]):
                generated = (
                    "$(bazel-bin)\\service\\%s\\ServiceInfo.cs" % service["package"]
                )
                self.assertIn(generated, items(csproj(service), "Compile"))

    def test_csproj_references_dependencies(self):
        for service in SERVICES:
            with self.subTest(service=service["name"]):
                references = {
                    Path(include.replace("\\", "/")).name
                    for include in items(csproj(service), "ProjectReference")
                    if include.startswith("..\\..\\")
                    and not include.startswith("..\\..\\..\\")
                }
                expected = {
                    "KRPC.%s.csproj" % BY_NAME[dep]["package"]
                    for dep in service["deps"]
                    if dep not in CORE_SERVICES
                }
                self.assertEqual(expected, references)

    def test_service_attribute_takes_generated_id(self):
        for service in SERVICES:
            with self.subTest(service=service["name"]):
                attributes = [
                    match.group(1)
                    for path in sorted(
                        Path("service", service["package"], "src").rglob("*.cs")
                    )
                    for match in SERVICE_ATTRIBUTE.finditer(
                        path.read_text(encoding="utf-8")
                    )
                ]
                self.assertEqual(1, len(attributes))
                self.assertRegex(attributes[0], r"\bId = ServiceInfo\.Id\b")

    def test_solution_lists_projects(self):
        solution = Path("KRPC.sln").read_text(encoding="utf-8")
        for service in SERVICES:
            with self.subTest(service=service["name"]):
                path = str(csproj(service)).replace("/", "\\")
                self.assertIn('"%s"' % path, solution)

    def test_changelog_index_links_changelogs(self):
        index = Path("CHANGELOG.md").read_text(encoding="utf-8")
        linked = re.findall(r"\]\((service/[^/]+)/CHANGELOG\.md\)", index)
        self.assertEqual(
            ["service/" + service["package"] for service in SERVICES], linked
        )

    def test_changelog_components(self):
        components = [
            (component, name)
            for component, name in changelog.COMPONENTS.items()
            if component.startswith("service/")
        ]
        expected = [
            ("service/" + service["package"], service["title"] + " service")
            for service in SERVICES
        ]
        self.assertEqual(expected, components)


class TestMods(unittest.TestCase):
    def assert_resolves(self, request):
        """Check that a request, name or name==version, names a version in MODS."""
        name, _, version = request.partition("==")
        self.assertIn(name, MODS)
        if version:
            self.assertIn(version, [v["version"] for v in MODS[name]["versions"]])

    def test_reported_mods_known(self):
        for service in SERVICES:
            for name in service["mods"] + service["supports"]:
                with self.subTest(service=service["name"], mod=name):
                    self.assertIn(name, MODS)
                    self.assertIsNotNone(MODS[name]["assembly"])

    def test_assembly_sha256(self):
        for mod in MODS.values():
            for version in mod["versions"]:
                with self.subTest(mod=mod["name"], version=version["version"]):
                    if mod["assembly"] is None:
                        self.assertIsNone(version["assembly_sha256"])
                    else:
                        self.assertRegex(version["assembly_sha256"], "^[0-9a-f]{64}$")

    def test_one_service_reports_each_mod(self):
        names = [
            name
            for service in SERVICES
            for name in service["mods"] + service["supports"]
        ]
        self.assertEqual(len(names), len(set(names)))

    def test_requires_known(self):
        for mod in MODS.values():
            for version in mod["versions"]:
                with self.subTest(mod=mod["name"], version=version["version"]):
                    for request in version["requires"]:
                        self.assert_resolves(request)

    def test_version_has_one_archive(self):
        for mod in MODS.values():
            for version in mod["versions"]:
                with self.subTest(mod=mod["name"], version=version["version"]):
                    if version["archive"] is None:
                        self.assertIsNotNone(version["url"])
                        self.assertIsNotNone(version["sha256"])
                    else:
                        self.assertIsNone(version["url"])
                        self.assert_resolves(
                            "%s==%s" % (version["archive"], version["version"])
                        )


if __name__ == "__main__":
    unittest.main()
