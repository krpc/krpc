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

from krpctools.game import game, install, launch
from krpctools.game.ksp import KSP
from krpctools.game.ksp.game import copy_file, copy_folder, source_digest

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

    def test_requested_mod_files_allowed(self):
        self._populate([*_CLEAN, "TweakScale", "999_Scale_Redist.dll", "000_Harmony"])
        self.ksp.validate_install(["TweakScaleRescaled", "Harmony"])

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
            [
                "RealChute==1.4.9.5",
                "ToolbarControl==0.1.9.14",
                "ClickThroughBlocker==2.1.10.23",
                "Harmony==2.2.1.0",
            ],
            install.requirements(ksp, ["RealChute"]),
        )

    def test_shared_dependency_listed_once(self):
        ids = install.requirements(KSP(), ["FAR", "KerbalAlarmClock"])
        self.assertEqual(1, ids.count("Harmony==2.2.1.0"))

    def test_name_requests_the_first_version(self):
        self.assertEqual(
            ["KSPCommunityPartModules==0.5.1", "Harmony==2.2.1.0"],
            install.requirements(KSP(), ["KSPCommunityPartModules"]),
        )

    def test_version_request(self):
        self.assertEqual(
            "KSPCommunityPartModules==0.4.0",
            install.requirements(KSP(), ["KSPCommunityPartModules==0.4.0"])[0],
        )

    def test_two_versions_of_a_mod_raise(self):
        with self.assertRaises(ValueError):
            install.requirements(
                KSP(), ["KSPCommunityPartModules", "KSPCommunityPartModules==0.4.0"]
            )

    def test_shared_archive(self):
        ksp = KSP()
        self.assertEqual(
            ksp.mod("FAR").source, ksp.mod("ModularFlightIntegrator").source
        )

    def test_unknown_mod_raises(self):
        with self.assertRaises(ValueError):
            install.requirements(KSP(), ["NoSuchMod"])

    def test_every_dependency_is_known(self):
        ksp = KSP()
        for mod in ksp.mods.values():
            for request in mod.requires:
                ksp.mod(request)


KAC = "KerbalAlarmClock==3.14.2.7"
TOOLBAR_CONTROL = "ToolbarControl==0.1.9.14"


class TestLoadedModsMatch(unittest.TestCase):
    def test_required_mods_match(self):
        far = "FAR==0.16.1.2"
        self.assertTrue(launch._matches(KSP(), {far}, {far}))

    def test_loaded_dependency_matches(self):
        self.assertTrue(launch._matches(KSP(), {KAC}, {KAC, TOOLBAR_CONTROL}))

    def test_missing_mod_does_not_match(self):
        self.assertFalse(launch._matches(KSP(), {KAC}, {TOOLBAR_CONTROL}))

    def test_unrequired_mod_does_not_match(self):
        self.assertFalse(
            launch._matches(KSP(), {TOOLBAR_CONTROL}, {KAC, TOOLBAR_CONTROL})
        )


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

    def test_from_zip_root(self):
        archive = os.path.join(self.dir, "mod.zip")
        with zipfile.ZipFile(archive, "w") as f:
            f.writestr("Mod/Mod.dll", "dll")
            f.writestr("Other/Other.dll", "other")
        dst = os.path.join(self.dir, "out")
        copy_folder(archive, "Mod", dst)
        self.assertTrue(os.path.isfile(os.path.join(dst, "Mod.dll")))
        self.assertFalse(os.path.exists(os.path.join(dst, "Other.dll")))

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


class TestDigests(TempDirTestCase):
    def setUp(self):
        super().setUp()
        self.archive = os.path.join(self.dir, "Mod.zip")
        with zipfile.ZipFile(self.archive, "w") as f:
            f.writestr("Mod-1.0/GameData/Mod/Plugins/Mod.dll", "release")
        self.ksp = KSP(directory=self.dir)
        self.mod = mock.Mock(folders=("Mod",))

    def test_digest_from_archive(self):
        self.assertEqual(
            hashlib.sha256(b"release").hexdigest(),
            source_digest(self.archive, ("Mod",), "Mod.dll"),
        )

    def test_digest_from_directory(self):
        self.write("build/GameData/Mod/Mod.dll", "build")
        self.assertEqual(
            hashlib.sha256(b"build").hexdigest(),
            source_digest(os.path.join(self.dir, "build"), ("Mod",), "Mod.dll"),
        )

    def test_source_without_the_file(self):
        self.assertIsNone(source_digest(self.archive, ("Mod",), "Missing.dll"))

    def test_installed_digest(self):
        self.write("GameData/Mod/Plugins/Mod.dll", "release")
        self.assertEqual(
            hashlib.sha256(b"release").hexdigest(),
            self.ksp.installed_digest(self.mod, "Mod.dll"),
        )

    def test_installed_digest_of_missing_file(self):
        self.write("GameData/Mod/Other.dll", "release")
        self.assertIsNone(self.ksp.installed_digest(self.mod, "Mod.dll"))

    def test_installed_without_a_local_install(self):
        with mock.patch.dict(os.environ, {"KSP_DIR": ""}):
            ksp = KSP()
            self.assertFalse(ksp.has_install())
            self.assertIsNone(ksp.mod("FAR").installed(ksp))


class _Version(game.Mod):
    """A version of a mod whose presence and installed files a test sets."""

    def __init__(self, name, version, loaded, installed):
        super().__init__()
        self.name = name
        self.version = version
        self.loaded = loaded
        self.files_installed = installed
        self.asked = 0

    def present(self, game, conn):
        self.asked += 1
        return self.loaded

    def installed(self, game):
        return self.files_installed


class TestLoadedMods(unittest.TestCase):
    def game(self, *mods):
        the_game = game.Game()
        the_game.mods = {mod.id: mod for mod in mods}
        return the_game

    def test_installed_version(self):
        new = _Version("Mod", "2", True, False)
        old = _Version("Mod", "1", True, True)
        self.assertEqual({"Mod==1"}, launch.loaded_mods(self.game(new, old), None))

    def test_mod_asked_once(self):
        new = _Version("Mod", "2", True, True)
        old = _Version("Mod", "1", True, False)
        launch.loaded_mods(self.game(new, old), None)
        self.assertEqual((1, 0), (new.asked, old.asked))

    def test_required_version_tried_first(self):
        new = _Version("Mod", "2", True, None)
        old = _Version("Mod", "1", True, None)
        self.assertEqual(
            {"Mod==1"}, launch.loaded_mods(self.game(new, old), None, {"Mod==1"})
        )

    def test_unpinned_version(self):
        mod = _Version("Mod", "1", True, False)
        self.assertEqual(
            {"Mod (another version)"}, launch.loaded_mods(self.game(mod), None)
        )

    def test_mod_not_loaded(self):
        mod = _Version("Mod", "1", False, True)
        self.assertEqual(set(), launch.loaded_mods(self.game(mod), None))

    def test_untold_presence_counts_when_required(self):
        mod = _Version("Mod", "1", None, None)
        the_game = self.game(mod)
        self.assertEqual(set(), launch.loaded_mods(the_game, None))
        self.assertEqual({"Mod==1"}, launch.loaded_mods(the_game, None, {"Mod==1"}))


class TestCopyFile(TempDirTestCase):
    def test_from_zip(self):
        archive = os.path.join(self.dir, "mod.zip")
        with zipfile.ZipFile(archive, "w") as f:
            f.writestr("Mod-1.0/GameData/Mod.dll", "dll")
            f.writestr("Mod-1.0/GameData/Mod/Mod.dll", "nested")
        dst = os.path.join(self.dir, "Mod.dll")
        copy_file(archive, "Mod.dll", dst)
        with open(dst, encoding="utf-8") as f:
            self.assertEqual("dll", f.read())

    def test_from_directory(self):
        self.write("build/GameData/Mod.dll", "dll")
        dst = os.path.join(self.dir, "Mod.dll")
        copy_file(os.path.join(self.dir, "build"), "Mod.dll", dst)
        self.assertTrue(os.path.isfile(dst))

    def test_missing_file_raises(self):
        archive = os.path.join(self.dir, "mod.zip")
        with zipfile.ZipFile(archive, "w") as f:
            f.writestr("GameData/Mod/Mod.dll", "nested")
        with self.assertRaises(RuntimeError):
            copy_file(archive, "Mod.dll", os.path.join(self.dir, "Mod.dll"))


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
        self.assertIsInstance(game.load("krpctools.game.ksp:KSP"), KSP)

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
        self.assertEqual("FAR.zip", ksp.mod("FAR").source)
        self.assertIsNone(ksp.mod("FAR").sha256)
        self.assertEqual(("FerramAerospaceResearch",), ksp.mod("FAR").folders)

    def test_unknown_mod_source_raises(self):
        with self.assertRaises(ValueError):
            game.apply(KSP(), {"mod_sources": {"NoSuchMod": "x.zip"}})

    def test_suite_mod_replaces_the_game_mod(self):
        mod = KSP().mod("FAR").with_source("local/FAR.zip")
        self.assertIs(mod, KSP(mods=[mod]).mod("FAR"))

    def test_suite_mod_with_no_version_replaces_every_version(self):
        mod = game.Mod("local/KSPCommunityPartModules.zip")
        mod.name = "KSPCommunityPartModules"
        ksp = KSP(mods=[mod])
        self.assertIs(mod, ksp.mod("KSPCommunityPartModules"))
        self.assertEqual(
            ["KSPCommunityPartModules"],
            [key for key in ksp.mods if key.startswith("KSPCommunityPartModules")],
        )


class TestPackage(unittest.TestCase):
    def test_addon_is_packaged(self):
        names = [os.path.basename(path) for path in KSP().addon_files()]
        self.assertIn("TestingTools.dll", names)
        self.assertIn("KRPC.Benchmark.dll", names)

    def test_blank_saves_are_packaged(self):
        ksp = KSP()
        self.assertIsNotNone(ksp.blank_save("krpctest"))
        self.assertIsNotNone(ksp.blank_save("krpctest_career"))
        self.assertTrue(os.path.isfile(ksp.settings_file()))


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
