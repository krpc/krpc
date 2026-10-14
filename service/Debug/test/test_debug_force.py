import unittest

import krpctest
from krpctest.geometry import norm


class TestDebugForce(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.debug_service = cls.connect().debug
        cls.vessel = cls.connect().space_center.active_vessel
        cls.part = cls.vessel.parts.root

    def test_add_force(self):
        force = self.debug_service.add_force(
            self.part, (1, 2, 3), (4, 5, 6), self.part.reference_frame
        )
        self.assertEqual(force.part.name, self.part.name)
        self.assertEqual((1, 2, 3), force.force_vector)
        self.assertEqual((4, 5, 6), force.position)
        self.assertEqual(self.part.reference_frame, force.reference_frame)
        force.remove()


class TestDebugForceInOrbit(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Basic")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.vessel = cls.connect().space_center.active_vessel

    def angular_speed(self):
        ref = self.vessel.orbit.body.non_rotating_reference_frame
        return norm(self.vessel.angular_velocity(ref))

    def test_force_removed_on_disconnect(self):
        self.vessel.control.sas = False
        self.wait()

        # A second client applies a constant torque-producing force; the
        # vessel spins up while that client is connected
        conn = self.connect(use_cached=False)
        part = conn.space_center.active_vessel.parts.root
        conn.debug.add_force(part, (1000, 0, 0), (0, 10, 0), part.reference_frame)
        before = self.angular_speed()
        self.wait(1)
        during = self.angular_speed()
        self.assertGreater(during - before, 0.05)

        # Disconnecting removes the force and the spin-up stops
        conn.close()
        self.wait(1)
        after1 = self.angular_speed()
        self.wait(1)
        after2 = self.angular_speed()
        self.assertLess(abs(after2 - after1), 0.01)

    def test_instantaneous_force(self):
        # A prograde push for one physics update of 0.02 s raises the orbital speed by
        # force * 0.02 / mass
        force = 100000
        expected = force * 0.02 / self.vessel.mass
        before = self.vessel.orbit.speed
        self.connect().debug.instantaneous_force(
            self.vessel.parts.root,
            (0, force, 0),
            (0, 0, 0),
            self.vessel.orbital_reference_frame,
        )
        self.wait(1)
        after = self.vessel.orbit.speed
        self.assertAlmostEqual(expected, after - before, delta=expected / 2)


if __name__ == "__main__":
    unittest.main()
