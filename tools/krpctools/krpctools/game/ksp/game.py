"""Kerbal Space Program: installing into GameData, launching, and the TestingTools add-on."""

import glob
import os
import shutil
import subprocess
import sys
import zipfile
from importlib.resources import files

from .. import game
from .krpc_version import KRPC_VERSION

# The executable's name differs per platform
_EXECUTABLE = "KSP_x64.exe" if sys.platform == "win32" else "KSP.x86_64"

# The TestingTools options that load a save, switch vessel or launch a craft, as the
# krpc-game run options they are given by. Each one shows up in --help. Only those given
# reach the game, as any one of them makes the add-on load a save.
_LOAD_OPTIONS = (
    ("load_game", "--krpctest-load-game"),
    ("load_save", "--krpctest-load-save"),
    ("load_vessel", "--krpctest-load-vessel"),
    ("load_craft", "--krpctest-load-craft"),
    ("load_craft_directory", "--krpctest-load-craft-directory"),
    ("load_craft_fixture_dir", "--krpctest-load-craft-fixture-dir"),
    ("load_launch_site", "--krpctest-load-launch-site"),
)

# GameData entries a test install always permits, on top of the mods a test requests. Stock
# KSP ships Squad and SquadExpansion, kRPC ships the ModuleManager assembly, and ModuleManager
# writes these cache files at run time. The versioned ModuleManager.<ver>.dll is matched by
# _is_module_manager_dll.
_BASELINE_GAMEDATA = frozenset(
    {
        "Squad",
        "SquadExpansion",
        "kRPC",
        "ModuleManager.ConfigCache",
        "ModuleManager.ConfigSHA",
        "ModuleManager.Physics",
        "ModuleManager.TechTree",
    }
)


def _is_module_manager_dll(entry):
    """Whether entry is the ModuleManager assembly, whose filename embeds its version."""
    return entry.startswith("ModuleManager") and entry.endswith(".dll")


def _resource(*path):
    return str(files("krpctools.game.ksp").joinpath(*path))


def normalize_permissions(path):
    """Set files to 0o644 and directories to 0o755 under path, path included."""
    os.chmod(path, 0o755)
    for root, dirs, filenames in os.walk(path):
        for name in dirs:
            os.chmod(os.path.join(root, name), 0o755)
        for name in filenames:
            os.chmod(os.path.join(root, name), 0o644)


def _folder_members(archive, folder):
    """The members of a zip archive under GameData/<folder>/, at any depth, as (member,
    path relative to the folder) pairs. An archive with no GameData folder gives the
    members under <folder>/ at its root."""
    members = []
    root = []
    for member in archive.infolist():
        parts = member.filename.split("/")
        if ".." in parts or member.is_dir():
            continue
        if len(parts) > 1 and parts[0] == folder:
            root.append((member, "/".join(parts[1:])))
        for i in range(len(parts) - 2):
            if parts[i] == "GameData" and parts[i + 1] == folder:
                members.append((member, "/".join(parts[i + 2 :])))
                break
    return members or root


def copy_file(source, name, dst):
    """Copy the file GameData/<name> from source, a zip archive or a directory, to dst."""
    if zipfile.is_zipfile(source):
        with zipfile.ZipFile(source) as archive:
            for member in archive.infolist():
                parts = member.filename.split("/")
                if parts[-2:] == ["GameData", name] and not member.is_dir():
                    with archive.open(member) as src, open(dst, "wb") as f:
                        shutil.copyfileobj(src, f)
                    return
        raise RuntimeError("%s has no GameData/%s" % (source, name))
    path = os.path.join(source, "GameData", name)
    if not os.path.isfile(path):
        raise RuntimeError("%s has no GameData/%s" % (source, name))
    shutil.copy(path, dst)


def copy_folder(source, folder, dst):
    """Copy GameData/<folder>, or <folder> at the root, from source, a zip archive or a
    directory, to dst."""
    if zipfile.is_zipfile(source):
        with zipfile.ZipFile(source) as archive:
            members = _folder_members(archive, folder)
            if not members:
                raise RuntimeError("%s has no GameData/%s" % (source, folder))
            for member, path in members:
                target = os.path.join(dst, *path.split("/"))
                os.makedirs(os.path.dirname(target), exist_ok=True)
                with archive.open(member) as src, open(target, "wb") as f:
                    shutil.copyfileobj(src, f)
        return
    candidates = [
        os.path.join(source, "GameData", folder),
        os.path.join(source, folder),
    ]
    if os.path.basename(os.path.normpath(source)) == folder:
        candidates.append(source)
    for candidate in candidates:
        if os.path.isdir(candidate):
            shutil.copytree(candidate, dst)
            return
    raise RuntimeError("%s has no GameData/%s" % (source, folder))


class Mod(game.Mod):
    """A KSP mod, installed as the folders and files it puts in GameData.

    The source holds each folder as GameData/<folder>, and each file as GameData/<file>: an
    archive as released, or a directory. After a folder is installed, any files under the mod's
    overlay directory for that folder are copied over it.
    """

    # The GameData folders the mod installs
    folders = ()

    # The files the mod installs at the top of GameData
    files = ()

    # The directory of config overlays, one subdirectory per folder. The supported mods keep
    # theirs beside their definitions.
    overlays = None

    def __init__(self, source=None, sha256=None):
        super().__init__(source, sha256)
        if self.overlays is None:
            self.overlays = _resource("mods")


class KSP(game.Game):
    """Kerbal Space Program, from the install named by KSP_DIR or --game-dir."""

    name = "ksp"
    directory_variable = "KSP_DIR"
    # Releases before the trains split are tagged v<version>
    release_prefixes = ("ksp-v", "v")
    # The release built with this package, which the bundled add-on is built against
    krpc_release = KRPC_VERSION

    def title(self):
        return "KSP"

    def default_mods(self):
        from .mods import MODS

        return [mod() for mod in MODS]

    # Locations

    def executable(self):
        """The game's executable within the install."""
        path = os.path.join(self.game_dir(), _EXECUTABLE)
        if not os.path.exists(path):
            raise RuntimeError(
                "No %s in %s. That is what the game's executable is called on this "
                "platform, so the install is either for another one or not a KSP install."
                % (_EXECUTABLE, self.game_dir())
            )
        return path

    def log_path(self):
        return os.path.join(self.game_dir(), "KSP.log")

    def gamedata(self):
        return os.path.join(self.game_dir(), "GameData")

    def blank_save(self, name):
        """The path of the blank save of the given name, or None if there is none."""
        path = _resource("saves", name + ".sfs")
        return path if os.path.isfile(path) else None

    def copy_blank_save(self, name):
        """Copy the blank save of the given name to saves/<name>/persistent.sfs."""
        blank_save = self.blank_save(name)
        if blank_save is None:
            raise ValueError("No blank save named %s" % name)
        save_path = os.path.join(self.game_dir(), "saves", name)
        os.makedirs(save_path, exist_ok=True)
        shutil.copy(blank_save, os.path.join(save_path, "persistent.sfs"))

    # Installing

    def release_assets(self, version):
        return ["krpc-ksp-%s.zip" % version, "krpc-%s.zip" % version]

    def addon_files(self):
        """The TestingTools add-on's assemblies, and the XML documentation the server reads
        beside each one."""
        addon = _resource("addon")
        paths = sorted(glob.glob(os.path.join(addon, "*.dll")))
        paths += sorted(glob.glob(os.path.join(addon, "*.xml")))
        if not paths:
            raise RuntimeError("This copy of krpctools has no TestingTools add-on")
        return paths

    def settings_file(self):
        """The server settings the tests run with, which start the server on load."""
        return _resource("settings.cfg")

    def install_krpc(self, path):
        # Replace kRPC and ModuleManager with the archive's GameData, as a user unzips a
        # release. Other mods are left as they are.
        gamedata = self.gamedata()
        krpc = os.path.join(gamedata, "kRPC")
        if os.path.exists(krpc):
            shutil.rmtree(krpc)
        for stale in glob.glob(os.path.join(gamedata, "ModuleManager*.dll")):
            os.remove(stale)
        with zipfile.ZipFile(path) as archive:
            members = [n for n in archive.namelist() if n.startswith("GameData/")]
            archive.extractall(self.game_dir(), members)
        normalize_permissions(krpc)
        for module_manager in glob.glob(os.path.join(gamedata, "ModuleManager*.dll")):
            os.chmod(module_manager, 0o644)

    def install_addon(self):
        krpc = os.path.join(self.gamedata(), "kRPC")
        for path in self.addon_files():
            shutil.copy(path, krpc)
        shutil.copy(
            self.settings_file(), os.path.join(krpc, "PluginData", "settings.cfg")
        )
        normalize_permissions(krpc)

    def install_mod(self, mod, path):
        for folder in mod.folders:
            dst = os.path.join(self.gamedata(), folder)
            if os.path.exists(dst):
                shutil.rmtree(dst)
            copy_folder(path, folder, dst)
            # Mod archives ship no settings, so some mods open a first-run window that
            # blocks an unattended launch. An overlay marks those already dismissed, or adds
            # a part for the tests.
            overlay = os.path.join(mod.overlays, folder)
            if os.path.isdir(overlay):
                shutil.copytree(overlay, dst, dirs_exist_ok=True)
            normalize_permissions(dst)
        for name in mod.files:
            dst = os.path.join(self.gamedata(), name)
            copy_file(path, name, dst)
            os.chmod(dst, 0o644)

    def remove_mod(self, mod):
        for folder in mod.folders:
            shutil.rmtree(os.path.join(self.gamedata(), folder), ignore_errors=True)
        for name in mod.files:
            path = os.path.join(self.gamedata(), name)
            if os.path.exists(path):
                os.remove(path)

    def validate_install(self, mods):
        """Fail if GameData holds anything beyond the stock game, kRPC, ModuleManager and
        the given mods. An unexpected assembly that fails to load, such as one that needs a
        Harmony build that is not installed, stops the kRPC server from starting. The test
        would then wait for a server that never comes up."""
        allowed = set(_BASELINE_GAMEDATA)
        for name in mods:
            allowed.update(self.mods[name].folders)
            allowed.update(self.mods[name].files)
        gamedata = self.gamedata()
        unexpected = [
            entry
            for entry in sorted(os.listdir(gamedata))
            if entry not in allowed and not _is_module_manager_dll(entry)
        ]
        if unexpected:
            raise RuntimeError(
                "Unexpected entries in %s: %s. The tests need GameData to hold the stock "
                "game plus kRPC, ModuleManager and only the mods a test requests. Remove "
                "the listed entries and re-run, or pass --skip-install-check."
                % (gamedata, ", ".join(unexpected))
            )

    # Launching

    def launch_command(self, args=()):
        return [self.executable(), *args]

    def test_arguments(self):
        # Without the save to load, the game stays at the main menu and the server never
        # starts
        self.copy_blank_save("krpctest")
        return ["--krpctest-load-game=krpctest"]

    def run_command(self, mods):
        return "krpc-game run --load-game=krpctest" + self._mods_option(mods)

    def kill_all(self):
        if sys.platform == "win32":
            command = ["taskkill", "/f", "/im", _EXECUTABLE]
        else:
            command = ["pkill", "-f", "KSP[.]x86_64"]
        try:
            subprocess.call(command)
        except OSError:
            pass

    def add_run_arguments(self, parser):
        group = parser.add_argument_group(
            "auto-load options",
            "Configure the TestingTools auto-load. Passing any one of these makes KSP "
            "load a save on reaching the main menu; with none, KSP stays at the main menu.",
        )
        group.add_argument(
            "--load-game",
            metavar="FOLDER",
            help="save folder under saves/ to load (TestingTools default: default)",
        )
        group.add_argument(
            "--load-save",
            metavar="NAME",
            help="save file name without .sfs (TestingTools default: persistent)",
        )
        group.add_argument(
            "--load-vessel",
            metavar="INDEX",
            type=int,
            help="vessel index to focus after loading the save",
        )
        group.add_argument(
            "--load-craft",
            metavar="NAME",
            help="craft to launch after loading the save (e.g. Parts or Parts.craft)",
        )
        group.add_argument(
            "--load-craft-directory",
            metavar="VAB|SPH",
            choices=["VAB", "SPH"],
            help="KSP craft directory and launch facility (default: inferred)",
        )
        group.add_argument(
            "--load-craft-fixture-dir",
            metavar="PATH",
            help="source directory of fixture .craft files to stage into the save",
        )
        group.add_argument(
            "--load-launch-site",
            metavar="SITE",
            help="launch site, for example LaunchPad or Runway "
            "(default: by craft directory)",
        )

    def run_arguments(self, args):
        # A blank save the install does not have yet is created, so a run against a fresh
        # install has a save to load. An existing save is the state being iterated on, and
        # is left alone.
        if (
            args.load_game is not None
            and self.blank_save(args.load_game) is not None
            and not os.path.exists(
                os.path.join(self.game_dir(), "saves", args.load_game)
            )
        ):
            self.copy_blank_save(args.load_game)
        return [
            "%s=%s" % (flag, getattr(args, dest))
            for dest, flag in _LOAD_OPTIONS
            if getattr(args, dest) is not None
        ]

    def log_filter(self, line):
        return "krpc" in line.lower()

    # The running game

    def client_type(self):
        from krpc.ksp import KSPClient

        return KSPClient
