"""The ``TestCase`` base class that integration tests subclass.

Provides the test-authoring surface: connecting to the running game, loading saves,
staging and launching craft, placing a vessel in orbit / on the ground / in flight, and
timed waiting. KSP process lifecycle lives in ``krpctest.game``; tolerance-based
assertions come from ``krpctest.assertions.AssertionsMixin``.
"""

import math
import os
import shutil
import sys
import time
import unittest

from krpctest import game
from krpctest.assertions import AssertionsMixin
from krpctest.env import get_ksp_dir


class TestCase(AssertionsMixin):
    # Location of the KSC launch sites, used as the default point for set_flight.
    KSC_LATITUDE = -0.0972
    KSC_LONGITUDE = -74.5577

    # Third-party mods this test class requires (e.g. ["RemoteTech"]). The framework
    # guarantees the running game has exactly this managed mod set before the class runs.
    mods = []

    # KSP expansions this test class requires (e.g. ["Serenity"] for Breaking Ground).
    # Unlike mods, expansions cannot be installed by the harness, so a class requiring one
    # that is not present is skipped rather than relaunched.
    expansions = []

    # Whether the class needs a running game. The library's own unit tests (which only
    # exercise TestCase's geometry/assertion helpers) set this False so they run headless
    # without launching KSP.
    game_required = True

    def __init_subclass__(cls, **kwargs):
        # Ensure ensure_game() runs before a subclass's setUpClass, even when that
        # subclass overrides setUpClass without calling super() (as most tests do).
        super().__init_subclass__(**kwargs)
        if not cls.game_required:
            return
        setup = cls.__dict__.get("setUpClass")
        if setup is None or getattr(setup.__func__, "_krpctest_wrapped", False):
            return
        raw = setup.__func__

        @classmethod
        def setUpClass(cls, _raw=raw):  # pylint: disable=invalid-name
            cls.ensure_game(cls.mods, cls.expansions)
            _raw(cls)

        setUpClass.__func__._krpctest_wrapped = True
        cls.setUpClass = setUpClass

    @classmethod
    def connect(cls, use_cached=True):
        return game.connect(use_cached)

    @classmethod
    def ensure_game(cls, mods=None, expansions=None):
        """Ensure a KSP server is running with exactly the required managed mods, then
        skip the test class if any required KSP expansion is missing. Expansions can't be
        installed by the harness, so they gate via unittest.SkipTest rather than relaunch.
        """
        game.ensure_game(mods)
        if expansions:
            installed = set(cls.connect().space_center.expansions)
            missing = [e for e in expansions if e not in installed]
            if missing:
                raise unittest.SkipTest(
                    "Required KSP expansion(s) not installed: " + ", ".join(missing)
                )

    @classmethod
    def setUpClass(cls):
        if cls.game_required:
            cls.ensure_game(cls.mods, cls.expansions)

    @classmethod
    def new_save(cls, name="krpctest", always_load=False):
        conn = cls.connect()

        # Load a new save if:
        #  - always_load is True
        #  - the save name is not as expected
        #  - we are not in the flight scene
        #  - the current vessel is not as expected
        #  - there is more than one vessel in the game

        def one_vessel():
            return len(conn.space_center.vessels) == 1

        def is_mk1_pod():
            return (
                conn.space_center.active_vessel is not None
                and conn.space_center.active_vessel.name == "Basic"
                and len(conn.space_center.active_vessel.parts.all) == 1
                and conn.space_center.active_vessel.parts.all[0].name == "mk1pod.v2"
            )

        if (
            not always_load
            and conn.testing_tools.current_save == name
            and conn.krpc.game_scene == conn.krpc.GameScene.flight
            and one_vessel()
            and is_mk1_pod()
        ):
            return

        # Load a blank save with the given name
        game.copy_blank_save(name)
        conn.testing_tools.load_save(name, "persistent")

    @classmethod
    def remove_other_vessels(cls):
        cls.connect().testing_tools.remove_other_vessels()

    @classmethod
    def _stage_craft(cls, name, editor, directory):
        # Copy the named craft file (from the given directory, else the test craft
        # directory, else KSP's stock craft for that editor) into the current save's
        # Ships/<editor> directory so it can be launched. Returns nothing; raises if the
        # craft cannot be found.
        if directory is None:
            # Resolve the craft directory relative to the test file, not the working
            # directory, so tests work regardless of where they are run from.
            test_file = sys.modules[cls.__module__].__file__
            directory = os.path.join(
                os.path.dirname(os.path.abspath(test_file)), "craft"
            )
        fixtures_paths = [
            os.path.abspath(directory),
            os.path.join(get_ksp_dir(), "Ships", editor),
        ]
        save_path = os.path.join(
            get_ksp_dir(), "saves", cls.connect().testing_tools.current_save
        )
        if not os.path.exists(save_path):
            os.makedirs(save_path)
        ships_path = os.path.join(save_path, "Ships", editor)
        if not os.path.exists(ships_path):
            os.makedirs(ships_path)
        for fixtures_path in fixtures_paths:
            craft = os.path.join(fixtures_path, name + ".craft")
            if os.path.exists(craft):
                shutil.copy(craft, os.path.join(ships_path, name + ".craft"))
                # Not every craft ships a loadmeta; copy it only when present.
                loadmeta = os.path.join(fixtures_path, name + ".loadmeta")
                if os.path.exists(loadmeta):
                    shutil.copy(loadmeta, os.path.join(ships_path, name + ".loadmeta"))
                return
        raise RuntimeError(
            "Failed to find craft in:\n" + "".join(f"  {x}\n" for x in fixtures_paths)
        )

    @classmethod
    def launch_vessel_from_vab(cls, name, directory=None, launch_site=None):
        cls._stage_craft(name, "VAB", directory)
        space_center = cls.connect().space_center
        if launch_site is None:
            space_center.launch_vessel_from_vab(name)
        else:
            space_center.launch_vessel("VAB", name, launch_site)
        # Ensure the crew are all pilots, for full control
        cls.set_crew_to_pilot()

    @classmethod
    def launch_vessel_from_sph(cls, name, directory=None, launch_site=None):
        # Launch an aircraft from the SPH (onto the runway by default). Stock aircraft
        # (e.g. "Aeris 3A", "Stearwing A300") live in KSP's Ships/SPH directory.
        cls._stage_craft(name, "SPH", directory)
        space_center = cls.connect().space_center
        if launch_site is None:
            space_center.launch_vessel_from_sph(name)
        else:
            space_center.launch_vessel("SPH", name, launch_site)
        # Ensure the crew are all pilots, for full control
        cls.set_crew_to_pilot()

    @classmethod
    def _set_game_scene(cls, scene, timeout=120):
        # Switch the game to the given krpc.GameScene and wait for it to arrive.
        # Scene changes are asynchronous, so the property has to be polled.
        conn = cls.connect()
        if conn.krpc.game_scene == scene:
            return
        conn.krpc.game_scene = scene
        deadline = time.time() + timeout
        while conn.krpc.game_scene != scene:
            if time.time() > deadline:
                raise RuntimeError(
                    "Timed out after %gs waiting for the %s game scene"
                    % (timeout, scene)
                )
            time.sleep(0.1)

    @classmethod
    def enter_editor(cls, facility="VAB", craft=None, directory=None):
        """Open the given editor ("VAB" or "SPH") and, when craft is given, load that
        vessel into it. The craft file is staged into the current save's Ships
        directory first, taken from the test's craft directory, else the given
        directory, else KSP's stock craft for that editor."""
        if facility not in ("VAB", "SPH"):
            raise ValueError("facility must be VAB or SPH, got %r" % facility)
        if craft is not None:
            cls._stage_craft(craft, facility, directory)
        conn = cls.connect()
        scenes = conn.krpc.GameScene
        target = scenes.editor_vab if facility == "VAB" else scenes.editor_sph
        # Go via the space center, so the editor starts out empty whatever was being
        # constructed beforehand. Switching straight from the other editor would carry
        # that vessel across.
        if conn.krpc.game_scene != target:
            cls._set_game_scene(scenes.space_center)
        cls._set_game_scene(target)
        cls.wait_for_editor()
        if craft is not None:
            conn.space_center.editor.load_vessel(facility, craft)

    @classmethod
    def wait_for_editor(cls, timeout=60):
        """Wait for the open editor to finish starting up. The scene reports itself as
        an editor some way before that; reading the vessel is the first thing that
        works once it has."""
        conn = cls.connect()
        deadline = time.time() + timeout
        while True:
            try:
                _ = conn.space_center.editor.vessel.name
                return
            except RuntimeError:
                if time.time() > deadline:
                    raise
                time.sleep(0.1)

    @classmethod
    def leave_editor(cls):
        """Return to the space center from the editor."""
        cls._set_game_scene(cls.connect().krpc.GameScene.space_center)

    @classmethod
    def set_orbit(
        cls,
        body,
        semi_major_axis,
        eccentricity,
        inclination,
        longitude_of_ascending_node,
        argument_of_periapsis,
        mean_anomaly_at_epoch,
        epoch,
    ):
        """Move the active vessel onto the orbit with the given elements. The three
        orientation angles are in degrees here, for readability at the call sites; the
        Debug service takes them in radians, as SpaceCenter.Orbit reports them."""
        conn = cls.connect()
        conn.debug.set_orbit(
            conn.space_center.bodies[body],
            semi_major_axis,
            eccentricity,
            math.radians(inclination),
            math.radians(longitude_of_ascending_node),
            math.radians(argument_of_periapsis),
            mean_anomaly_at_epoch,
            epoch,
        )

    @classmethod
    def set_circular_orbit(cls, body, altitude):
        conn = cls.connect()
        conn.debug.set_circular_orbit(conn.space_center.bodies[body], altitude)

    @classmethod
    def set_landed(cls, body, latitude, longitude, altitude=0):
        conn = cls.connect()
        conn.debug.set_landed(
            conn.space_center.bodies[body], latitude, longitude, altitude
        )

    @classmethod
    def set_pitch_heading_roll(cls, pitch, heading, roll):
        """Point the active vessel at the given pitch, heading and roll (degrees)
        in its surface reference frame, and zero its rotational velocity."""
        cls.connect().debug.set_pitch_heading_roll(pitch, heading, roll)

    @classmethod
    def set_flight(
        cls,
        altitude=5000,
        speed=75,
        heading=90,
        pitch=0,
        roll=0,
        angle_of_attack=0,
        body="Kerbin",
        latitude=None,
        longitude=None,
    ):
        """Place the active vessel in atmospheric flight over the given point (the KSC by
        default): at altitude (m above MSL) and airspeed (m/s), pointing along heading
        (degrees, 90 = east) at the given pitch and roll (degrees), then let physics
        resume so it is flying. angle_of_attack (degrees) puts the airspeed that far below
        the nose in the pitch plane; 0 (the default) is a nose-aligned airspeed (level
        flight), a positive value a nose-up attitude relative to the flight path (e.g. a
        re-entry hold), so the flight-path angle is pitch minus angle_of_attack."""
        if latitude is None:
            latitude = cls.KSC_LATITUDE
        if longitude is None:
            longitude = cls.KSC_LONGITUDE
        conn = cls.connect()
        conn.debug.set_flight(
            conn.space_center.bodies[body],
            latitude,
            longitude,
            altitude,
            speed,
            heading,
            pitch,
            roll,
            angle_of_attack,
        )

    @classmethod
    def fill_all_resources(cls):
        cls.connect().debug.fill_all_resources()

    @classmethod
    def fill_resources(cls, resource_name):
        cls.connect().debug.fill_resources(resource_name)

    @classmethod
    def set_crew_to_pilot(cls):
        cls.connect().testing_tools.set_crew_to_pilot()

    @classmethod
    def wait(cls, timeout=0.1):
        time.sleep(timeout)

    def wait_until(self, predicate, timeout=30, message=None, interval=0.1):
        """Repeatedly call wait() until predicate() returns a truthy value, and
        return that value. Fail the test if timeout seconds elapse first.
        interval is the poll period, in seconds, passed to wait().

        Use this instead of an unbounded ``while not predicate(): self.wait()``
        loop so a condition that never holds fails the test with a clear
        message rather than hanging the suite forever."""
        deadline = time.time() + timeout
        while True:
            value = predicate()
            if value:
                return value
            if time.time() > deadline:
                self.fail(
                    "Timed out after %gs waiting for %s"
                    % (timeout, message or "condition")
                )
            self.wait(interval)

    def wait_while(self, predicate, timeout=30, message=None, interval=0.1):
        """Repeatedly call wait() while predicate() returns a truthy value. Fail
        the test if timeout seconds elapse first. interval is the poll period,
        in seconds, passed to wait(). message should describe the state being
        waited for (i.e. the condition that ends the wait)."""
        return self.wait_until(
            lambda: not predicate(),
            timeout=timeout,
            message=message,
            interval=interval,
        )
