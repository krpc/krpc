"""The test case the examples run in: it brings the game to an example's setup, runs the
example and checks how it ended."""

from __future__ import annotations

import os
import re
import shutil
import tempfile
from typing import ClassVar

from doc.test import languages, runner
from doc.test.languages import REPO
from doc.test.monitor import Monitor
from doc.test.spec import (
    Example,
    Exit,
    Flight,
    Interactive,
    Loop,
    Orbit,
    Pad,
    Setup,
    Splashed,
    example_path,
)
from krpctools.game.ksp import TestCase

# Points east of the space center, the first with deep water used for a splashed vessel
OCEAN = [(0, -60), (0, -40), (0, -20), (10, -50)]

# The time an example has to connect before a hook acts on the game, in seconds
CONNECT_TIMEOUT = 60


class ExampleTestCase(TestCase):
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
        for name, craft in setup.ships:
            cls.stage_ship(name, craft)
        if setup.quicksave:
            cls.connect().space_center.quicksave()
        if setup.scene == "space_center":
            cls._set_game_scene(cls.connect().krpc.GameScene.space_center)
        cls._vessel = vessel
        cls._part_count = len(vessel.parts.all)

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
            or len(space_center.vessels) != 1
            or len(vessel.parts.all) != self._part_count
        )

    def restore(self):
        """Undoes the changes an example can make without changing the vessel."""
        if self.setup.scene == "editor":
            return
        conn = self.connect()
        space_center = conn.space_center
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

    # Running an example

    def run_example(self, example: Example, language: str):
        example = example.for_language(language)
        if example.prepare:
            example.prepare(self)
        command = languages.command(language, example_path(example.key, language))
        monitor = example.monitor(self) if example.monitor else None
        if monitor:
            monitor.start()
        try:
            with tempfile.TemporaryDirectory(prefix="krpc-example-") as cwd:
                result = self.run_command(
                    example, command, str(command.cwd or cwd), monitor
                )
            if monitor and result.exited and result.returncode == 0:
                monitor.settle()
        finally:
            if monitor:
                monitor.stop()
        message = result.describe()
        run = example.run.then if isinstance(example.run, Interactive) else example.run
        if isinstance(run, Exit):
            self.assertTrue(result.exited, "Timed out. " + message)
            self.assertEqual(0, result.returncode, message)
        else:
            assert isinstance(run, Loop)
            self.assertFalse(result.exited, "Exited before being stopped. " + message)
            if run.prints:
                self.assertTrue(result.stdout.strip(), "Printed nothing. " + message)
        self.assertEqual("", result.stderr, message)
        for pattern in example.expect_output:
            self.assertTrue(
                re.search(pattern, result.stdout),
                "Output does not match %r. %s" % (pattern, message),
            )
        if example.check:
            example.check(self)
        if monitor:
            print(monitor.describe())
            monitor.check()

    def run_command(
        self,
        example: Example,
        command: languages.Command,
        cwd: str,
        monitor: Monitor | None = None,
    ) -> runner.Result:
        run = example.run
        mode = run.then if isinstance(run, Interactive) else run
        # The clients connected before the example starts, by id. The last example's client
        # stays in the list until the server drops its connection.
        clients = {client.id for client in self.connect().krpc.clients}
        # Whether the example runs to its end, for a runner that has to stop the program then
        env = dict(
            command.env, KRPC_EXAMPLE_MODE="exit" if isinstance(mode, Exit) else "loop"
        )
        process = runner.Process(command.argv, env, cwd)
        connected = True
        try:
            if isinstance(run, Interactive):
                ready = run.ready
                if ready is None:
                    connected = runner.wait_for(
                        lambda: any(
                            client.id not in clients
                            for client in self.connect().krpc.clients
                        ),
                        process,
                        CONNECT_TIMEOUT,
                    )
                else:
                    connected = runner.wait_for(
                        lambda: re.search(ready, process.stdout()) is not None,
                        process,
                        CONNECT_TIMEOUT,
                    )
                if connected:
                    run.during(self)
            if connected:
                timeout = mode.timeout if isinstance(mode, Exit) else mode.seconds
                if monitor:
                    # Stop the example early when the monitor fails
                    runner.wait_for(monitor.failed.is_set, process, timeout)
                else:
                    process.wait(timeout)
        finally:
            result = process.finish()
        if not connected:
            self.fail("Not ready for the hook. " + result.describe())
        return result
