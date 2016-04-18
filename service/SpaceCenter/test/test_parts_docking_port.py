import unittest
import testingtools
import krpc
import time
from mathtools import norm

class TestPartsDockingPort(testingtools.TestCase):

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.remove_other_vessels()
        testingtools.launch_vessel_from_vab('PartsDockingPort')
        cls.conn = testingtools.connect(name='TestPartsDockingPort')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.state = cls.conn.space_center.DockingPortState

        # Get the three undocked ports
        ports = cls.parts.docking_ports
        cls.port1 = next(iter(filter(lambda p: p.part.title == 'Clamp-O-Tron Docking Port Jr.', ports)))
        cls.port2 = next(iter(filter(lambda p: p.part.title == 'Clamp-O-Tron Shielded Docking Port', ports)))
        cls.port3 = next(iter(filter(lambda p: p.part.title == 'Mk2 Clamp-O-Tron', ports)))
        cls.port4 = next(iter(filter(lambda p: p.part.title == 'Inline Clamp-O-Tron', ports)))

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_docking_port(self):
        self.assertEqual(self.state.ready, self.port1.state)
        self.assertEqual(self.port1.docked_part, None)
        self.assertEqual(1, self.port1.reengage_distance)
        self.assertFalse(self.port1.has_shield)
        self.assertFalse(self.port1.shielded)

    def test_name(self):
        self.assertEqual(self.port1.name, 'Clamp-O-Tron Docking Port Jr.')
        try:
            self.port1.name = 'Named docking port'
            self.assertEqual(self.port1.name, 'Named docking port')
            self.port1.name = 'Clamp-O-Tron Docking Port Jr.'
            self.assertEqual(self.port1.name, 'Clamp-O-Tron Docking Port Jr.')
        except krpc.client.RPCError:
            # TODO: Docking Port Alignment Indicator mod probably not installed
            pass

    def check_shielded(self, port):
        self.assertTrue(port.has_shield)
        self.assertTrue(port.shielded)
        self.assertEqual(self.state.shielded, port.state)
        self.assertEqual(port.docked_part, None)

    def open_and_close_shield(self, port):
        port.shielded = False
        time.sleep(0.1)
        self.assertEqual(self.state.moving, port.state)
        while port.state == self.state.moving:
            pass
        time.sleep(0.1)
        self.assertEqual(self.state.ready, port.state)
        port.shielded = True
        time.sleep(0.1)
        self.assertEqual(self.state.moving, port.state)
        while port.state == self.state.moving:
            pass
        time.sleep(0.1)
        self.assertEqual(self.state.shielded, port.state)

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
        bottom_port = next(iter(filter(lambda p: p.part.parent.title == 'Clamp-O-Tron Docking Port', self.parts.docking_ports)))
        top_port = bottom_port.part.parent.docking_port
        launch_clamp = bottom_port.part.children[0].launch_clamp

        self.assertEqual(self.state.docked, top_port.state)
        self.assertEqual(self.state.docked, bottom_port.state)
        self.assertEqual(top_port.part, bottom_port.docked_part)
        self.assertEqual(bottom_port.part, top_port.docked_part)

        # Undock
        mass_before = self.vessel.mass
        undocked = top_port.undock()
        mass_after = self.vessel.mass

        self.assertEqual(bottom_port.docked_part, None)
        self.assertEqual(top_port.docked_part, None)

        # Undocking
        self.assertNotEqual(self.vessel, undocked)
        self.assertLess(mass_after, mass_before)
        self.assertClose(mass_after, mass_before - undocked.mass)
        self.assertEqual(self.state.undocking, top_port.state)
        self.assertEqual(self.state.undocking, bottom_port.state)
        self.assertEqual(bottom_port.docked_part, None)
        self.assertEqual(top_port.docked_part, None)

        # Drop the port
        launch_clamp.release()
        while top_port.state == self.state.undocking:
            pass

        # Undocked
        self.assertEqual(self.state.ready, top_port.state)
        self.assertEqual(self.state.ready, bottom_port.state)
        self.assertEqual(bottom_port.docked_part, None)
        self.assertEqual(top_port.docked_part, None)
        distance = norm(top_port.position(bottom_port.reference_frame))
        self.assertGreater(distance, top_port.reengage_distance)
        self.assertLess(distance, top_port.reengage_distance*2)

    def test_pre_attached_port_and_part(self):
        """ Test a port and part that were pre-attached in the VAB """
        port = next(iter(filter(lambda p: p.part.title == 'Clamp-O-Tron Docking Port Sr.', self.parts.docking_ports)))
        part = port.part.children[0]
        launch_clamp = part.children[0].launch_clamp

        self.assertEqual(self.state.docked, port.state)
        self.assertEqual(part, port.docked_part)

        # Undock
        mass_before = self.vessel.mass
        undocked = port.undock()
        mass_after = self.vessel.mass

        self.assertEqual(port.docked_part, None)

        # Undocked (there is no undocking state when undocking from a part)
        self.assertNotEqual(self.vessel, undocked)
        self.assertLess(mass_after, mass_before)
        self.assertClose(mass_after, mass_before - undocked.mass)
        self.assertEqual(self.state.ready, port.state)
        self.assertEqual(port.docked_part, None)

    def test_direction(self):
        self.assertClose((0,0,-1), self.port1.direction(self.vessel.reference_frame))
        self.assertClose((0,1,0), self.port2.direction(self.vessel.reference_frame))
        self.assertClose((1,0,0), self.port3.direction(self.vessel.reference_frame))

class TestPartsDockingPortInFlight(testingtools.TestCase):
    """ Test docking and undocking of ports that have been docked
        in flight (as opposed to pre-attached in the VAB)"""

    def setUp(self):
        testingtools.new_save()
        testingtools.launch_vessel_from_vab('PartsDockingPortInFlight')
        testingtools.remove_other_vessels()
        testingtools.set_circular_orbit('Kerbin', 100000)
        self.conn = testingtools.connect(name='TestPartsDockingPortInFlight')
        self.sc = self.conn.space_center
        self.state = self.sc.DockingPortState

    def tearDown(self):
        self.conn.close()

    def test_docking_port1(self):
        port1, port2 = self.sc.active_vessel.parts.docking_ports
        self.undock_and_dock(port1, port2)

    def test_docking_port2(self):
        port1, port2 = self.sc.active_vessel.parts.docking_ports
        self.undock_and_dock(port2, port1)

    def undock_and_dock(self, port1, port2):
        vessel = self.sc.active_vessel

        # Do it twice - once to undock pre-attached ports, once to undock ports docked in flight
        for i in range(2):

            # Kill rotation
            vessel.control.sas = True
            time.sleep(3)
            vessel.control.sas = False
            time.sleep(1)

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
            self.assertEqual(None, port2.docked_part)
            self.assertEqual(None, port1.docked_part)
            mass_after = vessel.mass
            self.assertLess(mass_after, mass_before)
            self.assertClose(mass_after, mass_before - undocked.mass)

            # Move backwards to reengage distance
            vessel.control.rcs = True
            vessel.control.forward = -0.5
            time.sleep(0.5)
            vessel.control.forward = 0
            while port1.state == self.state.undocking:
                pass
            self.assertEqual(self.state.ready, port1.state)
            self.assertEqual(self.state.ready, port2.state)
            self.assertEqual(None, port2.docked_part)
            self.assertEqual(None, port1.docked_part)
            distance = norm(port1.position(port2.reference_frame))
            self.assertGreater(distance, port1.reengage_distance)
            self.assertLess(distance, port1.reengage_distance*1.1)
            time.sleep(0.5)

            # Check undocking when not docked
            with self.assertRaises(krpc.error.RPCError) as cm:
                port1.undock()
            self.assertTrue('The docking port is not docked' in str(cm.exception))
            with self.assertRaises(krpc.error.RPCError) as cm:
                port2.undock()
            self.assertTrue('The docking port is not docked' in str(cm.exception))

            # Move forward
            vessel.control.forward = 0.5
            time.sleep(1)
            vessel.control.forward = 0
            vessel.control.rcs = False
            while port1.state == self.state.ready:
                pass

            # Docking
            self.assertEqual(self.state.docking, port1.state)
            self.assertEqual(self.state.docking, port2.state)
            while port1.state == self.state.docking:
                pass

            # Docked
            self.assertEqual(self.state.docked, port1.state)
            self.assertEqual(self.state.docked, port2.state)
            self.assertEqual(port1.part, port2.docked_part)
            self.assertEqual(port2.part, port1.docked_part)

            # Get the new vessel
            vessel = self.sc.active_vessel


class TestPartsDockingPortPreAttachedTo(testingtools.TestCase):
    """ Test ports that are pre-attached (connected in VAB) """

    @classmethod
    def setUpClass(cls):
        testingtools.new_save()
        testingtools.remove_other_vessels()
        testingtools.launch_vessel_from_vab('PartsDockingPortPreAttachedTo')
        cls.conn = testingtools.connect(name='TestPartsDockingPortPreAttachedTo')
        cls.vessel = cls.conn.space_center.active_vessel
        cls.state = cls.conn.space_center.DockingPortState

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
        cls.parts.append(next(iter(filter(lambda p: p.docking_port != None, cls.parts[0].children))))
        part = cls.parts[-1]
        while len(part.children) == 1:
            part = part.children[0]
            cls.parts.append(part)

        cls.port1 = cls.parts[1].docking_port
        cls.port2 = cls.parts[2].docking_port
        cls.port4 = cls.parts[4].docking_port
        cls.port6 = cls.parts[6].docking_port

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_facing_parent_port(self):
        self.assertEqual(self.port2.docked_part, self.parts[1])
        self.assertEqual(self.port2.state, self.state.docked)

    def test_facing_child_port(self):
        self.assertEqual(self.port1.docked_part, self.parts[2])
        self.assertEqual(self.port1.state, self.state.docked)

    def test_facing_parent_part(self):
        self.assertEqual(self.port4.docked_part, self.parts[3])
        self.assertEqual(self.port4.state, self.state.docked)

    def test_facing_child_part(self):
        self.assertEqual(self.port6.docked_part, self.parts[7])
        self.assertEqual(self.port6.state, self.state.docked)

if __name__ == "__main__":
    unittest.main()
