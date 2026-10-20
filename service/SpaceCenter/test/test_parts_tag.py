import unittest

import krpctest

# Part.Tag reads the name tag part module of whichever mod provides it: ModuleNameTag from
# KSPCommunityPartModules 0.5 or later, or KOSNameTag from kOS. Each class covers one install.
# PartsTag stores its tags in ModuleNameTag nodes, and PartsTagLegacy in the KOSNameTag nodes
# of craft made with earlier versions of kRPC.


class PartsTagTest(krpctest.Mixin):
    craft = "PartsTag"
    module: str | None = "ModuleNameTag"

    @classmethod
    def setUpClass(cls):
        # Inherited by the test classes, so not wrapped by TestCase to ensure the game
        cls.ensure_game(cls.mods, cls.expansions)
        cls.new_save()
        cls.launch_vessel_from_vab(cls.craft)
        cls.remove_other_vessels()
        cls.parts = cls.connect().space_center.active_vessel.parts
        cls.pod = cls.parts.root


class PartsTagAvailableTest(PartsTagTest):
    def test_module(self):
        modules = [x.name for x in self.pod.modules]
        for name in ("ModuleNameTag", "KOSNameTag"):
            if name == self.module:
                self.assertEqual(1, modules.count(name))
            else:
                self.assertNotIn(name, modules)

    def test_tag(self):
        self.assertEqual("pod", self.pod.tag)
        self.assertEqual([self.pod], self.parts.with_tag("pod"))
        self.addCleanup(setattr, self.pod, "tag", "pod")
        self.pod.tag = "capsule"
        self.assertEqual("capsule", self.pod.tag)
        self.assertEqual([self.pod], self.parts.with_tag("capsule"))
        self.assertEqual([], self.parts.with_tag("pod"))
        self.pod.tag = ""
        self.assertEqual("", self.pod.tag)


class PartsTagUnavailableTest(PartsTagTest):
    module = None

    def test_module(self):
        modules = [x.name for x in self.pod.modules]
        self.assertNotIn("ModuleNameTag", modules)
        self.assertNotIn("KOSNameTag", modules)

    def test_tag(self):
        with self.assertRaises(RuntimeError):
            _ = self.pod.tag
        with self.assertRaises(RuntimeError):
            self.pod.tag = "capsule"


class TestPartsTagNoMods(PartsTagUnavailableTest, krpctest.TestCase):
    pass


class TestPartsTagOldCommunityPartModules(PartsTagUnavailableTest, krpctest.TestCase):
    mods = ["KSPCommunityPartModules-0.4"]


class TestPartsTagCommunityPartModules(PartsTagAvailableTest, krpctest.TestCase):
    mods = ["KSPCommunityPartModules"]


class TestPartsTagCommunityPartModulesLegacyCraft(
    PartsTagAvailableTest, krpctest.TestCase
):
    mods = ["KSPCommunityPartModules"]
    craft = "PartsTagLegacy"


class TestPartsTagKOSOldCommunityPartModules(PartsTagAvailableTest, krpctest.TestCase):
    mods = ["KSPCommunityPartModules-0.4", "kOS"]
    craft = "PartsTagLegacy"
    module = "KOSNameTag"


class TestPartsTagKOSCommunityPartModules(PartsTagAvailableTest, krpctest.TestCase):
    mods = ["KSPCommunityPartModules", "kOS"]
    craft = "PartsTagLegacy"
    module = "KOSNameTag"


if __name__ == "__main__":
    unittest.main()
