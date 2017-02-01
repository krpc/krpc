import unittest
import math
import krpctest
from krpctest.geometry import compute_position, norm, dot
import krpc


class TestReferenceFrame(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.set_circular_orbit('Kerbin', 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.bodies = cls.space_center.bodies

    def check_object_position(self, obj, ref):
        # Check (0, 0, 0) position is at object position
        self.assertAlmostEqual((0, 0, 0), obj.position(ref))
        # Check norm of object position is same as objects orbital radius
        if obj.orbit is not None:
            pos = obj.orbit.body.position(ref)
            self.assertAlmostEqual(obj.orbit.radius, norm(pos), delta=20)
        # Check position agrees with that calculated from bodies orbit
        if obj.name in ('Kerbin', 'Mun', 'Minmus', 'Test'):
            ref = obj.orbit.body.reference_frame
            expected_pos = compute_position(obj, ref)
            actual_pos = tuple(x / 1000000 for x in obj.position(ref))
            self.assertAlmostEqual(expected_pos, actual_pos, delta=1)

    def test_celestial_body_position(self):
        for body in self.bodies.values():
            self.check_object_position(body, body.reference_frame)

    def test_celestial_body_orbital_position(self):
        for body in self.bodies.values():
            if body.orbit is not None:
                self.check_object_position(body, body.orbital_reference_frame)
            else:
                self.assertRaises(krpc.client.RPCError, getattr,
                                  body, 'orbital_reference_frame')

    def test_vessel_position(self):
        self.check_object_position(
            self.vessel, self.vessel.reference_frame)

    def test_vessel_orbital_position(self):
        self.check_object_position(
            self.vessel, self.vessel.orbital_reference_frame)

    def test_vessel_surface_position(self):
        self.check_object_position(
            self.vessel, self.vessel.surface_reference_frame)

    def test_vessel_surface_velocity_position(self):
        self.check_object_position(
            self.vessel, self.vessel.surface_velocity_reference_frame)

    def test_node_position(self):
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        pos = self.vessel.position(node.reference_frame)
        # TODO: large error
        self.assertAlmostEqual((0, 0, 0), pos, delta=100)

    def test_node_orbital_position(self):
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        pos = self.vessel.position(node.orbital_reference_frame)
        # TODO: large error
        self.assertAlmostEqual((0, 0, 0), pos, delta=100)

    def check_object_velocity(self, obj, ref):
        # Check velocity vectors are unchanged by
        # converting between the same reference frame
        self.assertAlmostEqual(
            (1, 2, 3),
            self.space_center.transform_velocity(
                (0, 0, 0), (1, 2, 3), ref, ref))
        if obj.orbit is not None:
            # Check velocity of reference frame is same as orbital speed
            # in reference frame of body being orbited
            v = self.space_center.transform_velocity(
                (0, 0, 0), (0, 0, 0), ref,
                obj.orbit.body.non_rotating_reference_frame)
            self.assertAlmostEqual(norm(v), obj.orbit.speed, delta=0.5)

    def check_object_surface_velocity(self, obj, ref):
        if obj.orbit is not None:
            # Check rotational component of velocity same as orbital speed
            v = self.space_center.transform_velocity(
                (0, 0, 0), (0, 0, 0), ref,
                obj.orbit.body.reference_frame)
            # if obj.orbit.inclination == 0:
            #     self.assertAlmostEqual(0, v[1])
            # else:
            #     self.assertNotAlmostEqual(0, v[1])
            angular_velocity = obj.orbit.body.angular_velocity(
                obj.orbit.body.non_rotating_reference_frame)
            self.assertAlmostEqual(0, angular_velocity[0])
            self.assertAlmostEqual(0, angular_velocity[2])
            rotational_speed = dot((0, 1, 0), angular_velocity)
            position = list(obj.position(obj.orbit.body.reference_frame))
            position[1] = 0
            radius = norm(position)
            rotational_speed *= radius
            # TODO: large error
            self.assertAlmostEqual(
                abs(rotational_speed + obj.orbit.speed), norm(v), delta=200)

    def test_celestial_body_velocity(self):
        for body in self.bodies.values():
            self.check_object_velocity(body, body.reference_frame)
            self.check_object_surface_velocity(body, body.reference_frame)

    def test_celestial_body_orbital_velocity(self):
        for body in self.bodies.values():
            if body.orbit is not None:
                self.check_object_velocity(body, body.orbital_reference_frame)
                self.check_object_surface_velocity(
                    body, body.orbital_reference_frame)

    def test_vessel_velocity(self):
        self.check_object_velocity(self.vessel, self.vessel.reference_frame)
        self.check_object_surface_velocity(
            self.vessel, self.vessel.reference_frame)

    def test_vessel_orbital_velocity(self):
        self.check_object_velocity(
            self.vessel, self.vessel.orbital_reference_frame)
        self.check_object_surface_velocity(
            self.vessel, self.vessel.orbital_reference_frame)

    def test_vessel_surface_velocity(self):
        self.check_object_velocity(
            self.vessel, self.vessel.surface_reference_frame)
        self.check_object_surface_velocity(
            self.vessel, self.vessel.surface_reference_frame)

    def test_vessel_surface_velocity_velocity(self):
        self.check_object_velocity(
            self.vessel, self.vessel.surface_velocity_reference_frame)
        self.check_object_surface_velocity(
            self.vessel, self.vessel.surface_velocity_reference_frame)

    def test_node_velocity(self):
        # TODO: implement
        pass

    def test_node_orbital_velocity(self):
        # TODO: implement
        pass

    def test_celestial_body_direction(self):
        # Check (0, 1, 0) direction same as body direction
        for body in self.bodies.values():
            self.assertAlmostEqual(
                (0, 1, 0), body.direction(body.reference_frame))

    def test_celestial_body_orbital_direction(self):
        # TODO: implement
        pass

    def test_celestial_body_surface_direction(self):
        # TODO: implement
        pass

    def test_vessel_direction(self):
        # Check (0, 1, 0) direction same as vessel direction
        self.assertAlmostEqual(
            (0, 1, 0),
            self.vessel.direction(self.vessel.reference_frame), places=3)

    def test_vessel_orbital_direction(self):
        # TODO: implement
        pass

    def test_vessel_surface_direction(self):
        # TODO: implement
        pass

    def test_vessel_surface_velocity_direction(self):
        # TODO: implement
        pass

    def test_node_direction(self):
        # TODO: implement
        pass

    def test_node_orbital_direction(self):
        # TODO: implement
        pass

    def test_relative_position(self):
        position = (1, 2, 3)
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, position=position)
        self.assertAlmostEqual(
            tuple(-x for x in position), self.vessel.position(ref))

    def test_relative_rotation(self):
        cases = [
            # 90 degrees rotation around x
            # vessel points along -z
            {'rot': (math.sin(math.pi/4), 0, 0, math.cos(math.pi/4)),
             'dir': (0, 0, -1)},
            # 90 degrees rotation around y
            # vessel points along y
            {'rot': (0, math.sin(math.pi/4), 0, math.cos(math.pi/4)),
             'dir': (0, 1, 0)},
            # 90 degrees rotation around z
            # vessel points along x
            {'rot': (0, 0, math.sin(math.pi/4), math.cos(math.pi/4)),
             'dir': (1, 0, 0)}
        ]
        self.assertAlmostEqual(
            (0, 1, 0),
            self.vessel.direction(self.vessel.reference_frame), places=4)
        for case in cases:
            ref = self.space_center.ReferenceFrame.create_relative(
                self.vessel.reference_frame, rotation=case['rot'])
            self.assertAlmostEqual(
                case['dir'], self.vessel.direction(ref), places=4)

    def test_relative_velocity(self):
        velocity = (1, 2, 3)
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, velocity=velocity)
        self.assertAlmostEqual(
            tuple(-x for x in velocity), self.vessel.velocity(ref), places=4)

    def test_relative_angular_velocity(self):
        angular_velocity = (1, 2, 3)
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, angular_velocity=angular_velocity)
        self.assertAlmostEqual(
            tuple(-x for x in angular_velocity),
            self.vessel.angular_velocity(ref), places=4)

    def test_hybrid(self):
        position = self.vessel.reference_frame
        direction = self.vessel.orbital_reference_frame
        velocity = self.vessel.surface_reference_frame
        angular_velocity = self.vessel.parts.root.reference_frame
        ref = self.space_center.ReferenceFrame.create_hybrid(
            position, direction, velocity, angular_velocity)
        self.assertAlmostEqual(
            self.vessel.position(position),
            self.vessel.position(ref), places=3)
        self.assertAlmostEqual(
            self.vessel.direction(direction),
            self.vessel.direction(ref), places=4)
        self.assertAlmostEqual(
            self.vessel.velocity(velocity),
            self.vessel.velocity(ref), places=4)
        self.assertAlmostEqual(
            self.vessel.angular_velocity(angular_velocity),
            self.vessel.angular_velocity(ref), places=4)


if __name__ == '__main__':
    unittest.main()
