import contextlib
import math
from typing import TypeVar

from krpctools.game.geometry import dot, norm, normalize
from krpctools.game.ksp import TestCase

T = TypeVar("T")

# Kerbin suborbital trajectory: apoapsis at 100 km, periapsis at 10 km
KERBIN_RADIUS = 600000
KERBIN_SUBORBITAL_SMA = KERBIN_RADIUS + (100000 + 10000) / 2
KERBIN_SUBORBITAL_ECC = (100000 - 10000) / (2 * KERBIN_SUBORBITAL_SMA)

# Mun impact trajectory: apoapsis at 50 km, periapsis 100 km below the surface
MUN_RADIUS = 200000
MUN_IMPACT_SMA = MUN_RADIUS + (50000 - 100000) / 2
MUN_IMPACT_ECC = (50000 + 100000) / (2 * MUN_IMPACT_SMA)


class TrajectoriesTestCase(TestCase):
    mods = ["Trajectories"]
    always_load = False

    @classmethod
    def setUpClass(cls):
        cls.new_save(always_load=cls.always_load)
        cls.remove_other_vessels()
        conn = cls.connect()
        cls.traj = conn.trajectories
        cls.krpc = conn.krpc
        cls.sc = conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.kerbin = cls.sc.bodies["Kerbin"]
        cls.mun = cls.sc.bodies["Mun"]
        cls.traj.always_update = True

    @classmethod
    def tearDownClass(cls):
        cls.traj.clear_target()
        cls.traj.DescentProfile.reset()
        cls.traj.always_update = False

    @classmethod
    def place_at_apoapsis(cls, body, sma, ecc):
        """Put the vessel at the apoapsis of the given equatorial, prograde orbit,
        then wait for a trajectory computed from there."""
        cls.set_orbit(body, sma, ecc, 0, 0, 0, math.pi, cls.sc.ut)
        cls.wait_for_trajectory(cls.sc.ut)

    @classmethod
    def wait_for_trajectory(cls, ut):
        """Wait for patches computed after the given time. Each update_trajectory
        call runs a step of the mod's computation, so this works while the patches
        are held."""

        # Strictly after, as a computation from before a move can start on the same tick
        def computed():
            cls.traj.update_trajectory()
            return cls.start_ut() > ut

        cls.wait_until(computed, message="a trajectory computed after %f" % ut)

    @classmethod
    def start_ut(cls):
        """The start of the trajectory, or -inf without one."""
        patches = cls.traj.patches()
        return patches[0].start_ut if patches else -math.inf

    def hold_patches(self):
        """Stop the mod computing the trajectory again, so a test reads one
        computation across calls. The mod computes the trajectory for a target
        whatever always_update says, so this holds only while no target is set."""
        self.traj.always_update = False
        self.addCleanup(setattr, self.traj, "always_update", True)

    @contextlib.contextmanager
    def held_tick(self):
        """Make the calls inside on one physics tick, for a frame that moves with the
        vessel."""
        self.krpc.hold_tick()
        try:
            yield
        finally:
            self.krpc.release_tick()

    def some(self, value: T | None) -> T:
        """Check that a nullable result has a value, and return it."""
        self.assertIsNotNone(value)
        assert value is not None
        return value

    def surface_velocity(self, body):
        return self.vessel.flight(body.reference_frame).velocity

    def up(self, body):
        return normalize(self.vessel.position(body.reference_frame))

    def check_surface_velocity(self, body, position, inertial_velocity, velocity):
        """Check a velocity relative to the surface against the inertial velocity at
        an equatorial position. The surface moves east at the body's rotation speed."""
        up = normalize(position)
        vertical = dot(inertial_velocity, up)
        horizontal = norm(
            tuple(v - vertical * u for v, u in zip(inertial_velocity, up))
        )
        rotation_speed = 2 * math.pi * norm(position) / body.rotational_period
        expected = math.sqrt(vertical**2 + (horizontal - rotation_speed) ** 2)
        self.assertAlmostEqual(expected, norm(velocity), delta=1)

    def quickload(self):
        self.sc.quickload()
        self.wait_until(lambda: self.traj.available, message="Trajectories to load")


class KerbinOrbitCase(TrajectoriesTestCase):
    """A circular 200 km orbit of Kerbin."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.set_circular_orbit("Kerbin", 200000)
        cls.wait_for_trajectory(cls.sc.ut)

    def tearDown(self):
        self.traj.clear_target()
        self.traj.DescentProfile.reset()
        self.traj.always_update = True


class KerbinDescentCase(TrajectoriesTestCase):
    """A suborbital trajectory into Kerbin's atmosphere, placed again for each test."""

    def setUp(self):
        self.traj.clear_target()
        self.traj.DescentProfile.reset()
        self.place_at_apoapsis("Kerbin", KERBIN_SUBORBITAL_SMA, KERBIN_SUBORBITAL_ECC)
        self.wait_until(
            lambda: self.traj.impact_patch() is not None, message="an impact patch"
        )
        self.hold_patches()
        self.patches = self.traj.patches()

    def expected_longitude(self, inertial_position, dt):
        """The longitude, in the body's rotating frame, of the body-fixed point under
        the given inertial position dt seconds from now. The body turns east, so
        the point is that far west of where the position is now."""
        longitude = self.kerbin.longitude_at_position(
            inertial_position, self.kerbin.non_rotating_reference_frame
        )
        return longitude - 360 * dt / self.kerbin.rotational_period


class MunImpactCase(TrajectoriesTestCase):
    """A Mun orbit with a periapsis below the surface."""

    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.traj.clear_target()
        cls.place_at_apoapsis("Mun", MUN_IMPACT_SMA, MUN_IMPACT_ECC)
        cls.wait_until(
            lambda: cls.traj.impact_patch() is not None, message="an impact patch"
        )

    def setUp(self):
        self.hold_patches()

    def tearDown(self):
        self.traj.clear_target()
        self.traj.DescentProfile.reset()


class LandedCase(TrajectoriesTestCase):
    # The other classes move the vessel into space
    always_load = True
