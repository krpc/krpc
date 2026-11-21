"""The test case the KSP examples run in: it brings the game to an example's setup, and runs
the example with tools.doc_examples."""

from __future__ import annotations

import os
import shutil
from typing import ClassVar

from doc.test.spec import Flight, Orbit, Pad, Setup, Splashed
from krpctools.game.ksp import TestCase
from tools.doc_examples.harness import ExampleRunner
from tools.doc_examples.languages import REPO, Languages

# The languages of the examples in doc/src/scripts
LANGUAGES = Languages("//doc", "//doc/test:run-example-js", cnano="run-scripts-cnano")

# Points east of the space center, the first with deep water used for a splashed vessel
OCEAN = [(0, -60), (0, -40), (0, -20), (10, -50)]


class ExampleTestCase(TestCase, ExampleRunner):
    languages = LANGUAGES
    setup: ClassVar[Setup]
    # What the setup leaves, to tell whether an example changed it
    _vessel: ClassVar[object] = None
    _part_count: ClassVar[int] = 0

    @classmethod
    def setUpClass(cls):
        cls.establish()

    def setUp(self):
        if self.setup.fresh or self.changed():
            self.establish()
        else:
            self.restore()

    # Setups

    @classmethod
    def craft_directory(cls) -> str | None:
        # None makes the launch fall back to the game's own craft
        return str(REPO / cls.setup.directory) if cls.setup.directory else None

    @classmethod
    def establish(cls):
        setup = cls.setup
        if setup.scene == "editor":
            cls.new_save()
            cls.enter_editor(
                setup.editor, craft=setup.craft, directory=cls.craft_directory()
            )
            return
        vessel = cls.launch(
            setup.craft, directory=cls.craft_directory(), editor=setup.editor
        )
        cls.place(setup.placement)
        for _ in range(setup.vessels - 1):
            vessel = cls.launch(
                setup.craft,
                directory=cls.craft_directory(),
                editor="SPH",
                save=None,
                keep_other_vessels=True,
            )
        for name, craft in setup.ships:
            cls.stage_ship(name, craft)
        if setup.quicksave or setup.reload:
            cls.connect().space_center.quicksave()
        if setup.reload:
            vessel = cls.quickload()
        if setup.scene == "space_center":
            cls._set_game_scene(cls.connect().krpc.GameScene.space_center)
        cls._vessel = vessel
        cls._part_count = len(vessel.parts.all)

    @classmethod
    def quickload(cls):
        """Loads the quicksave, and returns the active vessel once the game has loaded it."""
        conn = cls.connect()
        loads = conn.krpc.game_state_loads
        conn.space_center.quickload()
        cls.wait_until(
            lambda: (
                conn.krpc.game_state_loads > loads
                and conn.krpc.game_scene == conn.krpc.GameScene.flight
                and conn.space_center.active_vessel is not None
            ),
            timeout=120,
            message="the quicksave to load",
        )
        return conn.space_center.active_vessel

    @classmethod
    def place(cls, placement):
        if isinstance(placement, Orbit):
            cls.set_circular_orbit(placement.body, placement.altitude)
        elif isinstance(placement, Flight):
            cls.set_flight(altitude=placement.altitude, speed=placement.speed)
        elif isinstance(placement, Splashed):
            cls.splash()
        else:
            assert isinstance(placement, Pad)

    @classmethod
    def splash(cls):
        space_center = cls.connect().space_center
        kerbin = space_center.bodies["Kerbin"]
        latitude, longitude = next(p for p in OCEAN if kerbin.bedrock_height(*p) < -100)
        cls.set_landed("Kerbin", latitude, longitude)
        vessel = space_center.active_vessel
        cls.wait_until(
            lambda: vessel.situation == space_center.VesselSituation.splashed,
            timeout=60,
            message="the vessel to reach the water",
        )

    @classmethod
    def stage_ship(cls, name: str, craft: str):
        """Stages craft in the save's VAB under name, for an example to launch."""
        cls._stage_craft(craft, "VAB", cls.craft_directory())
        ships = os.path.join(
            cls.game().game_dir(),
            "saves",
            cls.connect().testing_tools.current_save,
            "Ships",
            "VAB",
        )
        if name != craft:
            shutil.copy(
                os.path.join(ships, craft + ".craft"),
                os.path.join(ships, name + ".craft"),
            )

    def relaunch(self):
        """Launches the setup's craft again in the current save, which removes the old
        vessel and its parts."""
        setup = self.setup
        self.launch(
            setup.craft,
            directory=self.craft_directory(),
            editor=setup.editor,
            save=None,
        )

    def changed(self) -> bool:
        """Whether an example left the game in a state the restore cannot undo."""
        conn = self.connect()
        scenes = conn.krpc.GameScene
        if self.setup.scene == "editor":
            return conn.krpc.game_scene != scenes.editor_vab
        if conn.krpc.game_scene != scenes.flight:
            return True
        space_center = conn.space_center
        vessel = space_center.active_vessel
        return (
            vessel != self._vessel
            or len(space_center.vessels) != self.setup.vessels
            or len(vessel.parts.all) != self._part_count
        )

    def restore(self):
        """Undoes the changes an example can make without changing the vessel."""
        if self.setup.scene == "editor":
            return
        conn = self.connect()
        space_center = conn.space_center
        space_center.target_vessel = None
        space_center.rails_warp_factor = 0
        space_center.physics_warp_factor = 0
        vessel = space_center.active_vessel
        vessel.auto_pilot.reset()
        control = vessel.control
        control.throttle = 0
        control.sas = False
        control.rcs = False
        control.abort = False
        if not isinstance(self.setup.placement, Pad):
            self.place(self.setup.placement)
