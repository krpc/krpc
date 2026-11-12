"""The third-party KSP mods kRPC supports, read from the registry the package ships.

The registry, ``service/registry.bzl`` in the kRPC repository, pins each version of a mod: the
archive it installs from, the GameData folders it takes from it and the mods it needs alongside
it. The overlays for each mod's installs are in ``mods/<name>/``, one subdirectory per GameData
folder.

A service reports whether each mod it wraps is installed through its ``available`` property,
and SpaceCenter each mod it supports through ``<mod>_available``. A mod that no service reports
is a dependency of another. The SHA-256 of a mod's main assembly in GameData tells which
version is installed.
"""

import json

from krpc.utils import snake_case

from .. import install
from .game import Mod, _resource, source_digest


class RegistryMod(Mod):
    """A version of a mod in the registry."""

    def __init__(self, entry, version, url, sha256, reporter):
        super().__init__()
        self.name = entry["name"]
        self.version = version["version"]
        self.source = url
        self.sha256 = sha256
        self.folders = tuple(version["folders"])
        self.files = tuple(version["files"])
        self.requires = tuple(version["requires"])
        self.overlays = _resource("mods", self.name)
        # The service that reports the mod and the name of its property, or None
        self.reporter = reporter
        self.assembly = entry["assembly"]
        # The SHA-256 of <assembly>.dll in the pinned archive, or None for another source
        self.assembly_sha256 = version["assembly_sha256"]

    def with_source(self, source):
        mod = super().with_source(source)
        mod.assembly_sha256 = None
        return mod

    def install(self, game, path):
        super().install(game, path)
        if self.assembly_sha256 is not None and not self.installed(game):
            raise RuntimeError(
                "The installed %s.dll of %s does not have the assembly_sha256 the registry "
                "gives" % (self.assembly, self.id)
            )

    def present(self, game, conn):
        if self.reporter is None:
            return super().present(game, conn)
        service, available = self.reporter
        return bool(getattr(getattr(conn, service), available))

    def installed(self, game):
        if self.assembly is None or not game.has_install():
            return None
        name = self.assembly + ".dll"
        expected = self.assembly_sha256
        if expected is None:
            path = install.fetch(self.source, self.sha256)
            expected = source_digest(path, self.folders, name)
            if expected is None:
                raise RuntimeError("%s has no %s" % (path, name))
        return game.installed_digest(self, name) == expected


def _reporters(services):
    """The service and property that report each mod, by the mod's name."""
    reporters = {}
    for service in services:
        attribute = snake_case(service["name"])
        for name in service["mods"]:
            reporters[name] = (attribute, "available")
        for name in service["supports"]:
            prefix = snake_case(name[0].upper() + name[1:])
            reporters[name] = (attribute, prefix + "_available")
    return reporters


def mods():
    """A RegistryMod for each version of each mod in the registry, latest first."""
    with open(_resource("registry.json"), encoding="utf-8") as f:
        registry = json.load(f)
    entries = registry["mods"]
    reporters = _reporters(registry["services"])
    by_name = {entry["name"]: entry for entry in entries}
    result = []
    for entry in entries:
        for version in entry["versions"]:
            # A version with no archive of its own takes the archive of another mod's
            # version of the same number
            source = version
            if version["archive"] is not None:
                source = next(
                    other
                    for other in by_name[version["archive"]]["versions"]
                    if other["version"] == version["version"]
                )
            result.append(
                RegistryMod(
                    entry,
                    version,
                    source["url"],
                    source["sha256"],
                    reporters.get(entry["name"]),
                )
            )
    return result
