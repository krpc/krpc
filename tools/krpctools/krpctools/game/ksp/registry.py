"""The third-party KSP mods kRPC supports, read from the registry the package ships.

The registry, ``service/registry.bzl`` in the kRPC repository, pins each version of a mod: the
archive it installs from, the GameData folders it takes from it, the mods it needs alongside
it, and how to tell it loaded in the running game. The overlays for each mod's installs are in
``mods/<name>/``, one subdirectory per GameData folder.
"""

import json

from .game import Mod, _resource


class RegistryMod(Mod):
    """A version of a mod in the registry."""

    def __init__(self, entry, version, url, sha256):
        super().__init__()
        self.name = entry["name"]
        self.version = version["version"]
        self.source = url
        self.sha256 = sha256
        self.folders = tuple(version["folders"])
        self.files = tuple(version["files"])
        self.requires = tuple(version["requires"])
        self.overlays = _resource("mods", self.name)
        self.check = entry["present"]
        self.assembly = entry["assembly"]
        self.assembly_version = version["assembly_version"]

    def present(self, game, conn):
        check = self.check
        if check is None:
            return super().present(game, conn)
        kind = check["kind"]
        if kind == "service_property":
            return getattr(getattr(conn, check["service"]), check["property"])
        if kind == "part":
            return conn.testing_tools.part_available(check["name"])
        if kind == "part_module":
            return conn.testing_tools.part_module_available(check["name"])
        if kind == "assembly_version":
            return (
                conn.testing_tools.assembly_version(self.assembly)
                == self.assembly_version
            )
        raise ValueError("Unknown presence check %r for mod %s" % (kind, self.id))


def mods():
    """A RegistryMod for each version of each mod in the registry, latest first."""
    with open(_resource("registry.json"), encoding="utf-8") as f:
        entries = json.load(f)["mods"]
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
            result.append(RegistryMod(entry, version, source["url"], source["sha256"]))
    return result
