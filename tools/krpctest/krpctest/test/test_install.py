import os
import shutil
import tempfile
import krpctest
from krpctest.install import _validate_gamedata

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
        # unexpected — reconcile should have removed it.
        self._populate(_CLEAN + ["RealChute"])
        with self.assertRaises(RuntimeError) as cm:
            _validate_gamedata(self.gamedata, set())
        self.assertIn("RealChute", str(cm.exception))
