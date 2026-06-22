import unittest

import krpctest
from krpctest.geometry import (
    compute_position,
    dot,
    norm,
    quaternion_conjugate,
    quaternion_mult,
    quaternion_vector_mult,
)


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

    def check_unit_quaternion(self, rot_fn, frames):
        """Verify rot_fn(frame) returns a unit quaternion in every frame."""
        for ref in frames:
            self.assertAlmostEqual(1.0, norm(rot_fn(ref)), delta=0.01)

    def check_relative_rotation_invariant(self, rot_a_fn, rot_b_fn, frames, delta=0.01):
        """conj(rot_A(frame)) * rot_B(frame) is the same for every frame.

        Changing the frame multiplies both rotations by the same left factor,
        which cancels in the product and leaves the fixed relative orientation.
        """
        rel_rots = [
            quaternion_mult(quaternion_conjugate(rot_a_fn(ref)), rot_b_fn(ref))
            for ref in frames
        ]
        for r in rel_rots[1:]:
            self.assertQuaternionsAlmostEqual(rel_rots[0], r, delta=delta)

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
    # Vessel rotation tests
    # -------------------------------------------------------------------------

    def test_vessel_rotation_in_own_frame(self):
        """Vessel rotation is the identity quaternion in the vessel's own frame."""
        self.assertQuaternionsAlmostEqual(
            (0, 0, 0, 1), self.vessel.rotation(self.vessel.reference_frame), places=3
        )

    def test_vessel_rotation_is_unit_quaternion(self):
        """Vessel rotation is a unit quaternion in every frame."""
        self.check_unit_quaternion(
            self.vessel.rotation, self._vessel_frames() + self._kerbin_frames()
        )

    def test_vessel_rotation_consistent_with_direction(self):
        """Rotating (0,1,0) by the vessel quaternion recovers the vessel direction."""
        for ref in self._vessel_frames():
            rot = self.vessel.rotation(ref)
            self.assertAlmostEqual(
                self.vessel.direction(ref), quaternion_vector_mult(rot, (0, 1, 0)), delta=0.01
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
    # Root part rotation tests
    # -------------------------------------------------------------------------

    def test_part_rotation_in_own_frame(self):
        """Part rotation is the identity quaternion in the part's own frame."""
        self.assertQuaternionsAlmostEqual(
            (0, 0, 0, 1),
            self.root_part.rotation(self.root_part.reference_frame),
            places=3,
        )

    def test_part_rotation_is_unit_quaternion(self):
        """Part rotation is a unit quaternion in every vessel-centered frame."""
        self.check_unit_quaternion(self.root_part.rotation, self._vessel_frames())

    def test_part_rotation_consistent_with_direction(self):
        """Rotating (0,1,0) by the part quaternion recovers the part direction."""
        for ref in self._vessel_frames():
            rot = self.root_part.rotation(ref)
            self.assertAlmostEqual(
                self.root_part.direction(ref), quaternion_vector_mult(rot, (0, 1, 0)), delta=0.01
            )

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
    # Docking port rotation tests
    # -------------------------------------------------------------------------

    def test_docking_port_rotation_in_own_frame(self):
        """Docking port rotation is the identity quaternion in the port's own frame."""
        self.assertQuaternionsAlmostEqual(
            (0, 0, 0, 1),
            self.docking_port.rotation(self.docking_port.reference_frame),
            places=3,
        )

    def test_docking_port_rotation_is_unit_quaternion(self):
        """Docking port rotation is a unit quaternion in every vessel-centered frame."""
        self.check_unit_quaternion(self.docking_port.rotation, self._vessel_frames())

    def test_docking_port_rotation_consistent_with_direction(self):
        """Rotating (0,1,0) by the port quaternion recovers the port direction."""
        for ref in self._vessel_frames():
            rot = self.docking_port.rotation(ref)
            self.assertAlmostEqual(
                self.docking_port.direction(ref),
                quaternion_vector_mult(rot, (0, 1, 0)),
                delta=0.01,
            )

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
    # Cross-object rotation consistency
    # -------------------------------------------------------------------------

    def test_rotation_relative_orientation_frame_invariant(self):
        """The relative orientation between two on-vessel objects is the same
        regardless of which frame they are expressed in.

        conj(rot_A(frame)) * rot_B(frame) cancels the shared frame factor and
        leaves the fixed rigid-body relative orientation between A and B.
        """
        pairs = [
            (self.vessel.rotation, self.root_part.rotation),
            (self.vessel.rotation, self.docking_port.rotation),
            (self.root_part.rotation, self.docking_port.rotation),
        ]
        for rot_a, rot_b in pairs:
            self.check_relative_rotation_invariant(rot_a, rot_b, self._vessel_frames())

    def test_transform_rotation_round_trip(self):
        """transform_rotation A→B→A returns the original quaternion."""
        rot = self.vessel.rotation(self.vessel.reference_frame)
        for ref in self._kerbin_frames():
            via = self.space_center.transform_rotation(
                rot, self.vessel.reference_frame, ref
            )
            roundtrip = self.space_center.transform_rotation(
                via, ref, self.vessel.reference_frame
            )
            self.assertQuaternionsAlmostEqual(rot, roundtrip, delta=0.01)

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

    def test_node_rotation_in_own_frame(self):
        """Node rotation is the identity quaternion in the node's own frame."""
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        self.assertQuaternionsAlmostEqual(
            (0, 0, 0, 1), node.rotation(node.reference_frame), places=3
        )

    def test_node_rotation_is_unit_quaternion(self):
        """Node rotation is a unit quaternion in both node frames."""
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        self.assertAlmostEqual(1.0, norm(node.rotation(node.reference_frame)), delta=0.01)
        self.assertAlmostEqual(
            1.0, norm(node.rotation(node.orbital_reference_frame)), delta=0.01
        )

    def test_node_rotation_consistent_with_direction(self):
        """Rotating (0,1,0) by the node quaternion recovers the node burn direction."""
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        for ref in [node.reference_frame, node.orbital_reference_frame]:
            rot = node.rotation(ref)
            self.assertAlmostEqual(
                node.direction(ref), quaternion_vector_mult(rot, (0, 1, 0)), delta=0.01
            )

    # -------------------------------------------------------------------------
    # Linear velocity tests
    # -------------------------------------------------------------------------

    def test_vessel_velocity_zero_in_vessel_frames(self):
        """Vessel velocity is zero in all of its own frames.

        All vessel frames move at the vessel's orbital velocity and have the
        vessel at their origin, so the ω×r correction vanishes and the
        relative velocity is (0, 0, 0).
        """
        for ref in self._vessel_frames():
            self.assertAlmostEqual((0, 0, 0), self.vessel.velocity(ref), delta=0.5)

    def test_body_velocity_zero_in_own_frames(self):
        """A body's velocity is zero in its own rotating and non-rotating frames.

        Both frames move at the body's orbital velocity; the body is at the
        origin, so the ω×r correction vanishes and the relative velocity is
        (0, 0, 0).
        """
        for ref in [
            self.kerbin.reference_frame,
            self.kerbin.non_rotating_reference_frame,
        ]:
            self.assertAlmostEqual((0, 0, 0), self.kerbin.velocity(ref), delta=0.5)

    def test_vessel_speed_consistent_in_kerbin_non_rotating_and_orbital_frames(self):
        """Vessel speed is the same in Kerbin's non-rotating and orbital frames.

        Both frames move at Kerbin's orbital velocity and have zero angular
        velocity, so they produce the same speed — only the direction of the
        reported velocity vector differs.
        """
        speed_nr = norm(self.vessel.velocity(self.kerbin.non_rotating_reference_frame))
        speed_orb = norm(self.vessel.velocity(self.kerbin.orbital_reference_frame))
        self.assertAlmostEqual(speed_nr, speed_orb, delta=1)

    def test_vessel_orbital_speed_in_kerbin_non_rotating_frame(self):
        """In Kerbin's non-rotating frame the vessel's speed equals its orbital speed.

        The non-rotating frame is inertial relative to Kerbin (ω=0, frame
        velocity = Kerbin world velocity), so the measured speed is the
        vessel's velocity relative to Kerbin — i.e., orbit.speed.
        """
        speed = norm(self.vessel.velocity(self.kerbin.non_rotating_reference_frame))
        self.assertAlmostEqual(self.vessel.orbit.speed, speed, delta=1)

    def test_relative_frame_velocity_offset(self):
        """A velocity offset on a relative frame shifts the measured velocity by its negation.

        Adding (10, 0, 0) m/s to the parent vessel frame makes that frame
        move 10 m/s faster along the vessel y-axis.  The vessel sits at the
        frame origin (ω×r = 0) and therefore appears to move at (−10, 0, 0).
        """
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, velocity=(10, 0, 0)
        )
        self.assertAlmostEqual((-10, 0, 0), self.vessel.velocity(ref), delta=0.5)

    def test_hybrid_velocity_source_respected(self):
        """create_hybrid respects the velocity= sub-frame argument.

        A hybrid that omits velocity= inherits it from the position frame
        (vessel orbital velocity → vessel at rest), giving speed zero.
        A hybrid with velocity=kerbin_non_rotating uses Kerbin's velocity
        as the frame velocity, so the measured speed equals orbit.speed.
        """
        hybrid_default = self.space_center.ReferenceFrame.create_hybrid(
            position=self.vessel.reference_frame
        )
        hybrid_kerbin_vel = self.space_center.ReferenceFrame.create_hybrid(
            position=self.vessel.reference_frame,
            velocity=self.kerbin.non_rotating_reference_frame,
        )
        self.assertAlmostEqual((0, 0, 0), self.vessel.velocity(hybrid_default), delta=0.5)
        speed = norm(self.vessel.velocity(hybrid_kerbin_vel))
        self.assertAlmostEqual(self.vessel.orbit.speed, speed, delta=1)

    def test_transform_velocity_round_trip(self):
        """transform_velocity A→B→A returns the original velocity."""
        ref_a = self.kerbin.non_rotating_reference_frame
        pos = self.vessel.position(ref_a)
        vel = self.vessel.velocity(ref_a)
        for ref_b in self._vessel_frames():
            via = self.space_center.transform_velocity(pos, vel, ref_a, ref_b)
            roundtrip = self.space_center.transform_velocity(
                self.vessel.position(ref_b), via, ref_b, ref_a
            )
            self.assertAlmostEqual(vel, roundtrip, delta=0.5)

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

    def test_relative_rotation(self):
        """Rotation is unaffected by a position-only offset in a relative frame."""
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, position=(1, 2, 3)
        )
        self.assertQuaternionsAlmostEqual(
            (0, 0, 0, 1), self.vessel.rotation(ref), places=3
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

    def test_hybrid_rotation(self):
        """Vessel rotation is identity in a hybrid frame using vessel rotation."""
        ref = self.space_center.ReferenceFrame.create_hybrid(
            position=self.vessel.reference_frame
        )
        self.assertQuaternionsAlmostEqual((0, 0, 0, 1), self.vessel.rotation(ref))


if __name__ == "__main__":
    unittest.main()
