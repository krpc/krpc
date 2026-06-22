import unittest

import krpctest
from krpctest.geometry import compute_position, dot, norm


class TestReferenceFrame(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        if cls.connect().space_center.active_vessel.name != "Vessel":
            cls.launch_vessel_from_vab("Vessel")
            cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.bodies = cls.space_center.bodies
        cls.kerbin = cls.bodies["Kerbin"]
        cls.root_part = cls.vessel.parts.root
        cls.docking_port = cls.vessel.parts.docking_ports[0]
        cls.thruster = cls.vessel.parts.engines[0].thrusters[0]

    # -------------------------------------------------------------------------
    # Helpers
    # -------------------------------------------------------------------------

    def check_object_position(self, obj, ref):
        """Check that obj is at the origin of ref, and its parent body is at the
        expected distance (orbital radius)."""
        self.assertAlmostEqual((0, 0, 0), obj.position(ref))
        if obj.orbit is not None:
            pos = obj.orbit.body.position(ref)
            self.assertAlmostEqual(obj.orbit.radius, norm(pos), delta=20)
        if obj.name in ("Kerbin", "Mun", "Minmus", "Test"):
            ref = obj.orbit.body.reference_frame
            expected_pos = compute_position(obj, ref)
            actual_pos = tuple(x / 1000000 for x in obj.position(ref))
            self.assertAlmostEqual(expected_pos, actual_pos, delta=1)

    def _vessel_frames(self):
        """All vessel-centered frames — share origin at vessel CoM."""
        return [
            self.vessel.reference_frame,
            self.vessel.orbital_reference_frame,
            self.vessel.surface_reference_frame,
            self.vessel.surface_velocity_reference_frame,
        ]

    def _kerbin_frames(self):
        """All Kerbin-centered frames — share origin at Kerbin center."""
        return [
            self.kerbin.reference_frame,
            self.kerbin.non_rotating_reference_frame,
            self.kerbin.orbital_reference_frame,
        ]

    def check_magnitude_consistent(self, pos_fn, frames, delta=1):
        """Verify pos_fn(frame) has the same norm in every frame.

        This holds whenever all frames share the same origin: rotating a frame
        never changes the distance from the origin to any point.
        """
        norms = [norm(pos_fn(ref)) for ref in frames]
        for n in norms[1:]:
            self.assertAlmostEqual(norms[0], n, delta=delta)

    def check_cross_distance_symmetry(self, pos_a, frame_a, pos_b, frame_b, delta=0.01):
        """Verify that the distance from A to B equals the distance from B to A.

        norm(B.position(frame_A)) == norm(A.position(frame_B))

        Both expressions measure the same physical separation between the two
        points, just expressed in different frames. All four objects here are
        on the same rigid vessel, so the distance is constant between RPC calls.
        """
        d_ab = norm(pos_b(frame_a))
        d_ba = norm(pos_a(frame_b))
        self.assertAlmostEqual(d_ab, d_ba, delta=delta)

    def check_unit_direction(self, dir_fn, frames):
        """Verify dir_fn(frame) returns a unit vector in every frame."""
        for ref in frames:
            self.assertAlmostEqual(1.0, norm(dir_fn(ref)), delta=0.01)

    def check_dot_product_invariant(self, dir_a_fn, dir_b_fn, frames, delta=0.01):
        """Dot product between two directions is the same regardless of frame.

        Rotating the basis doesn't change the angle between directions.
        """
        dots = [dot(dir_a_fn(ref), dir_b_fn(ref)) for ref in frames]
        for d in dots[1:]:
            self.assertAlmostEqual(dots[0], d, delta=delta)

    # -------------------------------------------------------------------------
    # Celestial body tests
    # -------------------------------------------------------------------------

    def test_celestial_body_position(self):
        for body in self.bodies.values():
            self.check_object_position(body, body.reference_frame)

    def test_celestial_body_non_rotating_position(self):
        for body in self.bodies.values():
            self.check_object_position(body, body.non_rotating_reference_frame)

    def test_celestial_body_orbital_position(self):
        for body in self.bodies.values():
            if body.orbit is not None:
                self.check_object_position(body, body.orbital_reference_frame)
            else:
                self.assertRaises(ValueError, getattr, body, "orbital_reference_frame")

    # -------------------------------------------------------------------------
    # Vessel position tests
    # -------------------------------------------------------------------------

    def test_vessel_position_in_vessel_frames(self):
        """Vessel is at the origin of each of its own frames."""
        for ref in self._vessel_frames():
            self.check_object_position(self.vessel, ref)

    def test_vessel_position_in_body_frames(self):
        """Vessel distance from Kerbin center equals orbital radius in every Kerbin frame."""
        r = self.vessel.orbit.radius
        for ref in self._kerbin_frames():
            self.assertAlmostEqual(r, norm(self.vessel.position(ref)), delta=20)

    # -------------------------------------------------------------------------
    # Vessel direction tests
    # -------------------------------------------------------------------------

    def test_vessel_direction_in_own_frame(self):
        """Vessel nose points along the y-axis of the vessel's own frame."""
        self.assertAlmostEqual(
            (0, 1, 0), self.vessel.direction(self.vessel.reference_frame), places=3
        )

    def test_vessel_direction_is_unit_vector(self):
        """Vessel direction has magnitude 1 in every frame."""
        self.check_unit_direction(
            self.vessel.direction, self._vessel_frames() + self._kerbin_frames()
        )

    # -------------------------------------------------------------------------
    # Root part position tests
    # -------------------------------------------------------------------------

    def test_part_position_in_own_frame(self):
        """Part transform origin is at the origin of the part's own reference frame."""
        self.assertAlmostEqual(
            (0, 0, 0), self.root_part.position(self.root_part.reference_frame)
        )

    def test_part_center_of_mass_in_own_frame(self):
        """Part CoM is at the origin of the part CoM reference frame."""
        self.assertAlmostEqual(
            (0, 0, 0),
            self.root_part.center_of_mass(self.root_part.center_of_mass_reference_frame),
        )

    def test_part_position_in_vessel_frames(self):
        """Part's distance from vessel CoM is the same in all vessel-centered frames.

        All vessel frames share the same origin (vessel CoM); only their
        orientation differs, so the distance from origin to any fixed point
        is invariant across them.
        """
        self.check_magnitude_consistent(self.root_part.position, self._vessel_frames())

    def test_part_position_in_body_frames(self):
        """Part's distance from Kerbin center equals orbital radius in every Kerbin frame."""
        r = self.vessel.orbit.radius
        for ref in self._kerbin_frames():
            self.assertAlmostEqual(r, norm(self.root_part.position(ref)), delta=20)

    # -------------------------------------------------------------------------
    # Root part direction tests
    # -------------------------------------------------------------------------

    def test_part_direction_in_own_frame(self):
        """Part y-axis is (0,1,0) when expressed in the part's own frame."""
        self.assertAlmostEqual(
            (0, 1, 0),
            self.root_part.direction(self.root_part.reference_frame),
            places=3,
        )

    def test_part_direction_is_unit_vector(self):
        """Part direction has magnitude 1 in every vessel-centered frame."""
        self.check_unit_direction(self.root_part.direction, self._vessel_frames())

    # -------------------------------------------------------------------------
    # Docking port position tests
    # -------------------------------------------------------------------------

    def test_docking_port_position_in_own_frame(self):
        """Docking port node origin is at the origin of the port's own reference frame."""
        self.assertAlmostEqual(
            (0, 0, 0), self.docking_port.position(self.docking_port.reference_frame)
        )

    def test_docking_port_position_in_vessel_frames(self):
        """Docking port's distance from vessel CoM is the same in all vessel-centered frames."""
        self.check_magnitude_consistent(
            self.docking_port.position, self._vessel_frames()
        )

    def test_docking_port_position_in_body_frames(self):
        """Docking port's distance from Kerbin center equals orbital radius in every Kerbin frame."""
        r = self.vessel.orbit.radius
        for ref in self._kerbin_frames():
            self.assertAlmostEqual(r, norm(self.docking_port.position(ref)), delta=20)

    # -------------------------------------------------------------------------
    # Docking port direction tests
    # -------------------------------------------------------------------------

    def test_docking_port_direction_in_own_frame(self):
        """Docking port outward direction is (0,1,0) in the port's own frame."""
        self.assertAlmostEqual(
            (0, 1, 0),
            self.docking_port.direction(self.docking_port.reference_frame),
            places=3,
        )

    def test_docking_port_direction_is_unit_vector(self):
        """Docking port direction has magnitude 1 in every vessel-centered frame."""
        self.check_unit_direction(self.docking_port.direction, self._vessel_frames())

    # -------------------------------------------------------------------------
    # Thruster position tests
    # -------------------------------------------------------------------------

    def test_thrust_reference_frame_position(self):
        """Thruster nozzle is at the origin of the thrust reference frame."""
        self.assertAlmostEqual(
            (0, 0, 0),
            self.thruster.thrust_position(self.thruster.thrust_reference_frame),
        )

    def test_thruster_position_in_vessel_frames(self):
        """Thruster's distance from vessel CoM is the same in all vessel-centered frames."""
        self.check_magnitude_consistent(
            self.thruster.thrust_position, self._vessel_frames()
        )

    def test_thruster_position_in_body_frames(self):
        """Thruster's distance from Kerbin center equals orbital radius in every Kerbin frame."""
        r = self.vessel.orbit.radius
        for ref in self._kerbin_frames():
            self.assertAlmostEqual(r, norm(self.thruster.thrust_position(ref)), delta=20)

    # -------------------------------------------------------------------------
    # Thruster direction tests
    # -------------------------------------------------------------------------

    def test_thrust_direction_in_own_frame(self):
        """Thrust direction is (0,1,0) in the thrust reference frame."""
        self.assertAlmostEqual(
            (0, 1, 0),
            self.thruster.thrust_direction(self.thruster.thrust_reference_frame),
            places=3,
        )

    def test_thrust_direction_is_unit_vector(self):
        """Thrust direction has magnitude 1 in every vessel-centered frame."""
        self.check_unit_direction(self.thruster.thrust_direction, self._vessel_frames())

    # -------------------------------------------------------------------------
    # Cross-object distance symmetry
    # -------------------------------------------------------------------------

    def test_cross_object_distance_symmetry(self):
        """Physical distance between any two on-vessel objects is the same whether
        measured from A's frame or from B's frame.

        Covers positions of vessel/part/port/thruster in each other's frames —
        the combinations not reached by the per-object tests above.
        """
        objects = [
            (self.vessel.position, self.vessel.reference_frame),
            (self.root_part.position, self.root_part.reference_frame),
            (self.docking_port.position, self.docking_port.reference_frame),
            (self.thruster.thrust_position, self.thruster.thrust_reference_frame),
        ]
        for i, (pos_a, frame_a) in enumerate(objects):
            for pos_b, frame_b in objects[i + 1 :]:
                self.check_cross_distance_symmetry(pos_a, frame_a, pos_b, frame_b)

    def test_part_com_frame_offset_symmetry(self):
        """The distance from the part transform origin to the part CoM is the same
        measured in either direction.

        part.center_of_mass(part.reference_frame) and
        part.position(part.center_of_mass_reference_frame) measure the same
        physical gap between the two frame origins.
        """
        self.check_cross_distance_symmetry(
            self.root_part.position,
            self.root_part.reference_frame,
            self.root_part.center_of_mass,
            self.root_part.center_of_mass_reference_frame,
        )

    # -------------------------------------------------------------------------
    # Cross-object direction consistency
    # -------------------------------------------------------------------------

    def test_direction_dot_product_frame_invariant(self):
        """The angle between any two directions on the same rigid vessel is the
        same regardless of which frame they are expressed in.

        Rotating the basis does not change dot products.
        """
        pairs = [
            (self.vessel.direction, self.root_part.direction),
            (self.vessel.direction, self.docking_port.direction),
            (self.vessel.direction, self.thruster.thrust_direction),
            (self.root_part.direction, self.docking_port.direction),
        ]
        for dir_a, dir_b in pairs:
            self.check_dot_product_invariant(dir_a, dir_b, self._vessel_frames())

    # -------------------------------------------------------------------------
    # Maneuver node tests
    # -------------------------------------------------------------------------

    def test_node_position(self):
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        pos = self.vessel.position(node.reference_frame)
        self.assertAlmostEqual((0, 0, 0), pos)

    def test_node_orbital_position(self):
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        pos = self.vessel.position(node.orbital_reference_frame)
        self.assertAlmostEqual((0, 0, 0), pos)

    def test_node_direction(self):
        """Node burn direction is (0,1,0) in the node's own frame."""
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        self.assertAlmostEqual((0, 1, 0), node.direction(node.reference_frame))

    def test_node_direction_is_unit_vector_in_orbital_frame(self):
        """Node burn direction has magnitude 1 in the node's orbital frame."""
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        self.assertAlmostEqual(
            1.0, norm(node.direction(node.orbital_reference_frame)), delta=0.01
        )

    # -------------------------------------------------------------------------
    # Relative and hybrid reference frame tests
    # -------------------------------------------------------------------------

    def test_relative_position(self):
        position = (1, 2, 3)
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, position=position
        )
        self.assertAlmostEqual(tuple(-x for x in position), self.vessel.position(ref))

    def test_relative_direction(self):
        """Direction is unaffected by a position-only offset in a relative frame."""
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, position=(1, 2, 3)
        )
        self.assertAlmostEqual(
            (0, 1, 0), self.vessel.direction(ref), places=3
        )

    def test_hybrid_position(self):
        ref = self.space_center.ReferenceFrame.create_hybrid(
            position=self.vessel.reference_frame
        )
        self.assertAlmostEqual((0, 0, 0), self.vessel.position(ref))

    def test_hybrid_direction(self):
        """Vessel direction is (0,1,0) in a hybrid frame using vessel rotation."""
        ref = self.space_center.ReferenceFrame.create_hybrid(
            position=self.vessel.reference_frame
        )
        self.assertAlmostEqual((0, 1, 0), self.vessel.direction(ref))


if __name__ == "__main__":
    unittest.main()
