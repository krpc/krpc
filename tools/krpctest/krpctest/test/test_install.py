import email.message
import hashlib
import http.client
import os
import pathlib
import shutil
import tempfile
import unittest
import urllib.error
import zipfile
from unittest import mock

from krpctest import game, install
from krpctest.ksp import KSP
from krpctest.ksp.game import copy_folder

# A clean no-mod GameData: the stock game, kRPC, the ModuleManager assembly and its
# runtime-generated cache files.
_CLEAN = [
    "Squad",
    "SquadExpansion",
    "kRPC",
    "ModuleManager.4.2.3.dll",
    "ModuleManager.ConfigCache",
    "ModuleManager.ConfigSHA",
    "ModuleManager.Physics",
    "ModuleManager.TechTree",
]


class TempDirTestCase(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.mkdtemp()

    def tearDown(self):
        shutil.rmtree(self.dir, ignore_errors=True)

    def write(self, path, text=""):
        path = os.path.join(self.dir, *path.split("/"))
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8") as f:
            f.write(text)
        return path


class TestValidateInstall(TempDirTestCase):
    def setUp(self):
        super().setUp()
        self.gamedata = os.path.join(self.dir, "GameData")
        os.mkdir(self.gamedata)
        self.ksp = KSP(directory=self.dir)

    def _populate(self, entries):
        for entry in entries:
            self.write(os.path.join("GameData", entry))

    def test_clean_baseline_passes(self):
        self._populate(_CLEAN)
        self.ksp.validate_install([])

    def test_partial_baseline_passes(self):
        # The ModuleManager cache files do not exist until the first KSP launch, so a
        # fresh install must still validate without them.
        self._populate(["Squad", "SquadExpansion", "kRPC", "ModuleManager.4.2.3.dll"])
        self.ksp.validate_install([])

    def test_any_module_manager_version_passes(self):
        self._populate(["Squad", "SquadExpansion", "kRPC", "ModuleManager.9.9.9.dll"])
        self.ksp.validate_install([])

    def test_unexpected_mod_raises(self):
        self._populate([*_CLEAN, "KSPCommunityPartModules"])
        with self.assertRaises(RuntimeError) as cm:
            self.ksp.validate_install([])
        self.assertIn("KSPCommunityPartModules", str(cm.exception))

    def test_requested_mod_allowed(self):
        self._populate([*_CLEAN, "RealChute", "000_Harmony"])
        self.ksp.validate_install(["RealChute", "Harmony"])

    def test_unrequested_mod_raises(self):
        self._populate([*_CLEAN, "RealChute"])
        with self.assertRaises(RuntimeError) as cm:
            self.ksp.validate_install([])
        self.assertIn("RealChute", str(cm.exception))


class TestGameDir(TempDirTestCase):
    def test_relative_directory_is_made_absolute(self):
        cwd = os.getcwd()
        os.chdir(self.dir)
        self.addCleanup(os.chdir, cwd)
        os.mkdir("KSP")
        self.assertEqual(
            os.path.join(os.path.realpath(self.dir), "KSP"),
            os.path.realpath(KSP(directory="KSP").game_dir()),
        )
        self.assertTrue(os.path.isabs(KSP(directory="KSP").game_dir()))


class TestRequirements(unittest.TestCase):
    def test_dependencies_follow_the_mod(self):
        ksp = KSP()
        self.assertEqual(
            ["RealChute", "ToolbarControl", "ClickThroughBlocker", "Harmony"],
            install.requirements(ksp, ["RealChute"]),
        )

    def test_shared_dependency_listed_once(self):
        names = install.requirements(KSP(), ["FAR", "KerbalAlarmClock"])
        self.assertEqual(1, names.count("Harmony"))

    def test_unknown_mod_raises(self):
        with self.assertRaises(ValueError):
            install.requirements(KSP(), ["NoSuchMod"])

    def test_every_dependency_is_known(self):
        ksp = KSP()
        for mod in ksp.mods.values():
            for name in mod.requires:
                self.assertIn(name, ksp.mods)


class TestCopyFolder(TempDirTestCase):
    def test_from_zip(self):
        archive = os.path.join(self.dir, "mod.zip")
        with zipfile.ZipFile(archive, "w") as f:
            f.writestr("Mod-1.0/GameData/Mod/Mod.dll", "dll")
            f.writestr("Mod-1.0/GameData/Mod/Parts/part.cfg", "cfg")
            f.writestr("Mod-1.0/GameData/Other/Other.dll", "other")
        dst = os.path.join(self.dir, "out")
        copy_folder(archive, "Mod", dst)
        self.assertEqual(
            ["Mod.dll", "Parts/part.cfg"],
            sorted(
                p.relative_to(dst).as_posix() for p in pathlib.Path(dst).rglob("*.*")
            ),
        )

    def test_from_directory(self):
        self.write("build/GameData/Mod/Mod.dll", "dll")
        dst = os.path.join(self.dir, "out")
        copy_folder(os.path.join(self.dir, "build"), "Mod", dst)
        self.assertTrue(os.path.isfile(os.path.join(dst, "Mod.dll")))

    def test_from_the_folder_itself(self):
        self.write("Mod/Mod.dll", "dll")
        dst = os.path.join(self.dir, "out")
        copy_folder(os.path.join(self.dir, "Mod"), "Mod", dst)
        self.assertTrue(os.path.isfile(os.path.join(dst, "Mod.dll")))

    def test_missing_folder_raises(self):
        archive = os.path.join(self.dir, "mod.zip")
        with zipfile.ZipFile(archive, "w") as f:
            f.writestr("GameData/Other/Other.dll", "other")
        with self.assertRaises(RuntimeError):
            copy_folder(archive, "Mod", os.path.join(self.dir, "out"))


class TestFetch(TempDirTestCase):
    def setUp(self):
        super().setUp()
        patcher = mock.patch.object(
            install, "cache_dir", return_value=os.path.join(self.dir, "cache")
        )
        patcher.start()
        self.addCleanup(patcher.stop)
        self.source = self.write("source/mod.zip", "archive")
        self.url = pathlib.Path(self.source).as_uri()
        self.sha256 = hashlib.sha256(b"archive").hexdigest()

    def test_path_is_returned(self):
        self.assertEqual(self.source, install.fetch(self.source))

    def test_missing_path_raises(self):
        with self.assertRaises(RuntimeError):
            install.fetch(os.path.join(self.dir, "missing.zip"))

    def test_url_is_downloaded_once(self):
        path = install.fetch(self.url, self.sha256)
        self.assertEqual(os.path.join(self.dir, "cache", self.sha256, "mod.zip"), path)
        os.remove(self.source)
        self.assertEqual(path, install.fetch(self.url, self.sha256))

    def test_local_url_without_hash_is_read_in_place(self):
        self.assertEqual(self.source, install.fetch(self.url))

    def test_hash_mismatch_raises(self):
        with self.assertRaises(RuntimeError):
            install.fetch(self.url, "0" * 64)
        self.assertEqual([], os.listdir(os.path.join(self.dir, "cache", "0" * 64)))


class TestGame(unittest.TestCase):
    def test_load_by_name(self):
        self.assertIsInstance(game.load("ksp"), KSP)

    def test_load_by_reference(self):
        self.assertIsInstance(game.load("krpctest.ksp:KSP"), KSP)

    def test_load_unknown_raises(self):
        with self.assertRaises(ValueError):
            game.load("nosuchgame")

    def test_options_override_the_game(self):
        ksp = KSP(directory="a")
        game.apply(
            ksp,
            {
                "directory": "b",
                "krpc": None,
                "auto_launch": False,
                "mod_sources": {"FAR": "FAR.zip"},
            },
        )
        self.assertEqual("b", ksp.directory)
        self.assertIsNone(ksp.krpc)
        self.assertFalse(ksp.auto_launch)
        self.assertEqual("FAR.zip", ksp.mods["FAR"].source)
        self.assertIsNone(ksp.mods["FAR"].sha256)
        self.assertEqual(("FerramAerospaceResearch",), ksp.mods["FAR"].folders)

    def test_unknown_mod_source_raises(self):
        with self.assertRaises(ValueError):
            game.apply(KSP(), {"mod_sources": {"NoSuchMod": "x.zip"}})

    def test_suite_mod_replaces_the_game_mod(self):
        far = KSP().mods["FAR"]
        mod = type(far)("local/FAR.zip")
        self.assertIs(mod, KSP(mods=[mod]).mods["FAR"])


def _http_error(code):
    return urllib.error.HTTPError(
        "https://example.com", code, "error", email.message.Message(), None
    )


class TestRetries(unittest.TestCase):
    def setUp(self):
        patcher = mock.patch.object(install.time, "sleep")
        patcher.start()
        self.addCleanup(patcher.stop)

    def run_with(self, *errors):
        calls = []

        def action():
            calls.append(None)
            if len(calls) <= len(errors):
                raise errors[len(calls) - 1]
            return "done"

        return install.with_retries(action, "do it"), len(calls)

    def test_dropped_read_is_retried(self):
        error = http.client.IncompleteRead(b"partial")
        self.assertEqual(("done", 3), self.run_with(error, TimeoutError()))

    def test_server_error_is_retried(self):
        error = _http_error(504)
        self.assertEqual(("done", 2), self.run_with(error))

    def test_client_error_is_not_retried(self):
        error = _http_error(404)
        with self.assertRaises(RuntimeError) as cm:
            self.run_with(error)
        self.assertIn("Failed to do it", str(cm.exception))

    def test_last_failure_raises(self):
        with self.assertRaises(RuntimeError):
            self.run_with(*[ConnectionResetError()] * 4)
