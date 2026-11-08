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

import krpctest
from krpctest import install
from krpctest.install import _validate_gamedata
from krpctest.mods import ARCHIVES, MODS

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


class TestValidateGamedata(krpctest.TestCase):
    game_required = False

    def setUp(self):
        self.gamedata = tempfile.mkdtemp()

    def tearDown(self):
        shutil.rmtree(self.gamedata, ignore_errors=True)

    def _populate(self, entries):
        for entry in entries:
            with open(os.path.join(self.gamedata, entry), "w", encoding="utf-8"):
                pass

    def test_clean_baseline_passes(self):
        self._populate(_CLEAN)
        _validate_gamedata(self.gamedata, set())

    def test_partial_baseline_passes(self):
        # The ModuleManager cache files do not exist until the first KSP launch, so a
        # fresh install must still validate without them.
        self._populate(["Squad", "SquadExpansion", "kRPC", "ModuleManager.4.2.3.dll"])
        _validate_gamedata(self.gamedata, set())

    def test_any_module_manager_version_passes(self):
        self._populate(["Squad", "SquadExpansion", "kRPC", "ModuleManager.9.9.9.dll"])
        _validate_gamedata(self.gamedata, set())

    def test_unexpected_mod_raises(self):
        self._populate(_CLEAN + ["KSPCommunityPartModules"])
        with self.assertRaises(RuntimeError) as cm:
            _validate_gamedata(self.gamedata, set())
        self.assertIn("KSPCommunityPartModules", str(cm.exception))

    def test_requested_mod_allowed(self):
        self._populate(_CLEAN + ["RealChute", "000_Harmony"])
        _validate_gamedata(self.gamedata, {"RealChute", "000_Harmony"})

    def test_requested_mod_missing_from_set_raises(self):
        # A managed mod present in GameData but not among the requested subdirs is still
        # unexpected, as reconcile should have removed it.
        self._populate(_CLEAN + ["RealChute"])
        with self.assertRaises(RuntimeError) as cm:
            _validate_gamedata(self.gamedata, set())
        self.assertIn("RealChute", str(cm.exception))


class TestArchives(unittest.TestCase):
    def test_every_component_is_pinned(self):
        for components in MODS.values():
            for archive, _ in components:
                self.assertIn(archive, ARCHIVES)


class TestDownload(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.mkdtemp()
        self.addCleanup(shutil.rmtree, self.dir, ignore_errors=True)
        patcher = mock.patch.object(
            install, "cache_dir", return_value=os.path.join(self.dir, "cache")
        )
        patcher.start()
        self.addCleanup(patcher.stop)
        self.source = os.path.join(self.dir, "mod.zip")
        with zipfile.ZipFile(self.source, "w") as f:
            f.writestr("Mod-1.0/GameData/Mod/Mod.dll", "dll")
            f.writestr("Mod-1.0/GameData/Mod/Parts/part.cfg", "cfg")
            f.writestr("Mod-1.0/GameData/Other/Other.dll", "other")
        with open(self.source, "rb") as f:
            self.sha256 = hashlib.sha256(f.read()).hexdigest()
        self.url = pathlib.Path(self.source).as_uri()

    def test_downloaded_once(self):
        path = install.download(self.url, self.sha256)
        self.assertEqual(os.path.join(self.dir, "cache", self.sha256, "mod.zip"), path)
        os.remove(self.source)
        self.assertEqual(path, install.download(self.url, self.sha256))

    def test_hash_mismatch_raises(self):
        with self.assertRaises(RuntimeError):
            install.download(self.url, "0" * 64)
        self.assertEqual([], os.listdir(os.path.join(self.dir, "cache", "0" * 64)))

    def test_extract_folder(self):
        dst = os.path.join(self.dir, "out")
        install.extract_folder(self.source, "Mod", dst)
        self.assertEqual(
            ["Mod.dll", "Parts/part.cfg"],
            sorted(
                p.relative_to(dst).as_posix() for p in pathlib.Path(dst).rglob("*.*")
            ),
        )

    def test_extract_missing_folder_raises(self):
        with self.assertRaises(RuntimeError):
            install.extract_folder(self.source, "Missing", os.path.join(self.dir, "x"))


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
        self.assertEqual(("done", 2), self.run_with(_http_error(504)))

    def test_client_error_is_not_retried(self):
        with self.assertRaises(RuntimeError) as cm:
            self.run_with(_http_error(404))
        self.assertIn("Failed to do it", str(cm.exception))

    def test_last_failure_raises(self):
        with self.assertRaises(RuntimeError):
            self.run_with(*[ConnectionResetError()] * 4)
