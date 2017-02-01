import unittest
import krpctest
from krpctest.geometry import norm
import krpc


class TestPartsDockingPort(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab('PartsDockingPort')
        cls.sc = cls.connect().space_center
        cls.vessel = cls.sc.active_vessel
        cls.parts = cls.vessel.parts
        cls.State = cls.sc.DockingPortState
        cls.port1 = cls.parts.with_title(
            'Clamp-O-Tron Docking Port Jr.')[0].docking_port
        cls.port2 = cls.parts.with_title(
            'Clamp-O-Tron Shielded Docking Port')[0].docking_port
        cls.port3 = cls.parts.with_title(
            'Mk2 Clamp-O-Tron')[0].docking_port
        cls.port4 = cls.parts.with_title(
            'Inline Clamp-O-Tron')[0].docking_port
        cls.port5 = cls.parts.with_title(
            'Clamp-O-Tron Docking Port Sr.')[0].docking_port

    def test_docking_port(self):
        self.assertEqual(self.State.ready, self.port1.state)
        self.assertIsNone(self.port1.docked_part)
        self.assertEqual(1, self.port1.reengage_distance)
        self.assertFalse(self.port1.has_shield)
        self.assertFalse(self.port1.shielded)

    def check_shielded(self, port):
        self.assertTrue(port.has_shield)
        self.assertTrue(port.shielded)
        self.assertEqual(self.State.shielded, port.state)
        self.assertIsNone(port.docked_part)

    def open_and_close_shield(self, port):
        port.shielded = False
        self.wait()
        self.assertEqual(self.State.moving, port.state)
        while port.state == self.State.moving:
            self.wait()
        self.assertEqual(self.State.ready, port.state)
        port.shielded = True
        self.wait()
        self.assertEqual(self.State.moving, port.state)
        while port.state == self.State.moving:
            self.wait()
        self.assertEqual(self.State.shielded, port.state)

    def test_shielded_docking_port1(self):
        self.check_shielded(self.port2)
        self.open_and_close_shield(self.port2)

    def test_shielded_docking_port2(self):
        self.check_shielded(self.port3)
        self.open_and_close_shield(self.port3)

    def test_shielded_docking_port3(self):
        self.check_shielded(self.port4)
        self.open_and_close_shield(self.port4)

    def test_pre_attached_ports(self):
        """ Test ports that were pre-attached in the VAB """
        bottom_port = next(
            p for p in self.parts.docking_ports
            if p.part.parent.title == 'Clamp-O-Tron Docking Port')
        top_port = bottom_port.part.parent.docking_port
        launch_clamp = bottom_port.part.children[0].launch_clamp

        self.assertEqual(self.State.docked, top_port.state)
        self.assertEqual(self.State.docked, bottom_port.state)
        self.assertEqual(top_port.part, bottom_port.docked_part)
        self.assertEqual(bottom_port.part, top_port.docked_part)

        # Undock
        mass_before = self.vessel.mass
        undocked = top_port.undock()
        mass_after = self.vessel.mass

        self.assertIsNone(bottom_port.docked_part)
        self.assertIsNone(top_port.docked_part)

        # Undocking
        self.assertNotEqual(self.vessel, undocked)
        self.assertLess(mass_after, mass_before)
        self.assertAlmostEqual(
            mass_after, mass_before - undocked.mass, places=2)
        self.assertEqual(self.State.undocking, top_port.state)
        self.assertEqual(self.State.undocking, bottom_port.state)
        self.assertIsNone(bottom_port.docked_part)
        self.assertIsNone(top_port.docked_part)

        # Drop the port
        launch_clamp.release()
        while (top_port.state == self.State.undocking or
               bottom_port.state == self.State.undocking):
            pass

        # Undocked
        self.assertEqual(self.State.ready, top_port.state)
        self.assertEqual(self.State.ready, bottom_port.state)
        self.assertIsNone(bottom_port.docked_part)
        self.assertIsNone(top_port.docked_part)
        distance = norm(top_port.position(bottom_port.reference_frame))
        self.assertGreater(distance, top_port.reengage_distance)
        self.assertLess(distance, top_port.reengage_distance+1)

    def test_pre_attached_port_and_part(self):
        """ Test a port and part that were pre-attached in the VAB """
        port = self.port5
        part = port.part.children[0]

        self.assertEqual(self.State.docked, port.state)
        self.assertEqual(part, port.docked_part)

        # Undock
        mass_before = self.vessel.mass
        undocked = port.undock()
        mass_after = self.vessel.mass

        self.assertIsNone(port.docked_part)

        # Undocked (there is no undocking state when undocking from a part)
        self.assertNotEqual(self.vessel, undocked)
        self.assertLess(mass_after, mass_before)
        self.assertAlmostEqual(
            mass_after, mass_before - undocked.mass, places=2)
        self.assertEqual(self.State.ready, port.state)
        self.assertIsNone(port.docked_part)

    def test_direction(self):
        self.assertAlmostEqual(
            (0, 0, -1),
            self.port1.direction(self.vessel.reference_frame), places=3)
        self.assertAlmostEqual(
            (0, 1, 0),
            self.port2.direction(self.vessel.reference_frame), places=3)
        self.assertAlmostEqual(
            (1, 0, 0),
            self.port3.direction(self.vessel.reference_frame), places=3)

    def test_rotation(self):
        port = self.port1
        for target_frame in [port.reference_frame,
                             self.vessel.reference_frame,
                             self.vessel.orbit.body.reference_frame]:
            expected = self.sc.transform_rotation(
                (0, 0, 0, 1), port.reference_frame, target_frame)
            self.assertQuaternionsAlmostEqual(
                expected, port.rotation(target_frame), places=5)


class TestPartsDockingPortInFlight(krpctest.TestCase):
    """ Test docking and undocking of ports that have been docked
        in flight (as opposed to pre-attached in the VAB)"""

    def setUp(self):
        self.new_save()
        self.launch_vessel_from_vab('PartsDockingPortInFlight')
        self.remove_other_vessels()
        self.set_circular_orbit('Kerbin', 100000)
        self.sc = self.connect().space_center
        self.state = self.sc.DockingPortState

    def test_docking_port1(self):
        port1, port2 = self.sc.active_vessel.parts.docking_ports
        self.undock_and_dock(port1, port2)

    def test_docking_port2(self):
        port1, port2 = self.sc.active_vessel.parts.docking_ports
        self.undock_and_dock(port2, port1)

    def undock_and_dock(self, port1, port2):
        vessel = self.sc.active_vessel

        # Do it twice - once to undock pre-attached ports,
        # once to undock ports docked in flight
        for _ in range(2):

            # Kill rotation
            vessel.control.sas = True
            self.wait(3)
            vessel.control.sas = False
            self.wait()

            self.assertEqual(self.state.docked, port1.state)
            self.assertEqual(self.state.docked, port2.state)
            self.assertEqual(port1.part, port2.docked_part)
            self.assertEqual(port2.part, port1.docked_part)

            # Undock
            mass_before = vessel.mass
            undocked = port1.undock()
            self.assertNotEqual(vessel, undocked)
            self.assertEqual(self.state.undocking, port1.state)
            self.assertEqual(self.state.undocking, port2.state)
            self.assertIsNone(port2.docked_part)
            self.assertIsNone(port1.docked_part)
            mass_after = vessel.mass
            self.assertLess(mass_after, mass_before)
            self.assertAlmostEqual(
                mass_after, mass_before - undocked.mass, places=2)

            # Move backwards to reengage distance
            vessel.control.rcs = True
            vessel.control.forward = -0.5
            self.wait(0.5)
            vessel.control.forward = 0.0
            while (port1.state == self.state.undocking or
                   port2.state == self.state.undocking):
                self.wait()
            self.assertEqual(self.state.ready, port1.state)
            self.assertEqual(self.state.ready, port2.state)
            self.assertIsNone(port2.docked_part)
            self.assertIsNone(port1.docked_part)
            distance = norm(port1.position(port2.reference_frame))
            self.assertGreater(distance, port1.reengage_distance)
            self.assertLess(distance, port1.reengage_distance+1)
            self.wait(0.5)

            # Check undocking when not docked
            with self.assertRaises(krpc.error.RPCError) as cm:
                port1.undock()
            self.assertTrue(
                'The docking port is not docked' in str(cm.exception))
            with self.assertRaises(krpc.error.RPCError) as cm:
                port2.undock()
            self.assertTrue(
                'The docking port is not docked' in str(cm.exception))

            # Move forward
            vessel.control.forward = 0.5
            self.wait(1)
            vessel.control.forward = 0.0
            vessel.control.rcs = False
            while (port1.state == self.state.ready or
                   port2.state == self.state.ready):
                self.wait()

            # Docking
            self.assertEqual(self.state.docking, port1.state)
            self.assertEqual(self.state.docking, port2.state)
            while (port1.state == self.state.docking or
                   port2.state == self.state.docking):
                self.wait()

            # Docked
            self.assertEqual(self.state.docked, port1.state)
            self.assertEqual(self.state.docked, port2.state)
            self.assertEqual(port1.part, port2.docked_part)
            self.assertEqual(port2.part, port1.docked_part)

            # Get the new vessel
            vessel = self.sc.active_vessel


class TestPartsDockingPortPreAttachedTo(krpctest.TestCase):
    """ Test ports that are pre-attached (connected in VAB) """

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.remove_other_vessels()
        cls.launch_vessel_from_vab('PartsDockingPortPreAttachedTo')
        cls.vessel = cls.connect().space_center.active_vessel
        cls.state = cls.connect().space_center.DockingPortState

        # Stack is as follows, from top to bottom:
        # parts[0] - Pod
        # parts[1] - Docking port (facing down)
        # parts[2] - Docking port (facing up)
        # parts[3] - Tank
        # parts[4] - Docking port (facing up)
        # parts[5] - Tank
        # parts[6] - Docking port (facing down)
        # parts[7] - Tank

        cls.parts = [cls.vessel.parts.root]
        cls.parts.append(next(p for p in cls.parts[0].children
                              if p.docking_port is not None))
        part = cls.parts[-1]
        while len(part.children) == 1:
            part = part.children[0]
            cls.parts.append(part)

        cls.port1 = cls.parts[1].docking_port
        cls.port2 = cls.parts[2].docking_port
        cls.port4 = cls.parts[4].docking_port
        cls.port6 = cls.parts[6].docking_port

    def test_facing_parent_port(self):
        self.assertEqual(self.port2.docked_part, self.parts[1])
        self.assertEqual(self.state.docked, self.port2.state)

    def test_facing_child_port(self):
        self.assertEqual(self.port1.docked_part, self.parts[2])
        self.assertEqual(self.state.docked, self.port1.state)

    def test_facing_parent_part(self):
        self.assertEqual(self.port4.docked_part, self.parts[3])
        self.assertEqual(self.state.docked, self.port4.state)

    def test_facing_child_part(self):
        self.assertEqual(self.port6.docked_part, self.parts[7])
        self.assertEqual(self.state.docked, self.port6.state)


if __name__ == '__main__':
    unittest.main()
