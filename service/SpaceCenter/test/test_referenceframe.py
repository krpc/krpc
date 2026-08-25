# Accuracy note: the game advances one physics frame between consecutive RPCs.
# Tests that compare values from separate RPCs (e.g. fetch a position, then
# transform it) are subject to a timing race: the vessel moves ~2245 m/s in LKO,
# so even a single-frame gap (~20 ms) introduces tens of metres of error.
# Affected tests use delta=200 (metres) rather than the default 7-place tolerance.
# Tests that check a single value from one RPC (e.g. direction in own frame) are
# not affected by the race, but are limited to places=6 by float to double precision
# loss when Unity float Vector3 values are promoted for double-precision arithmetic.

import math
import unittest

import krpctest
from krpctest.geometry import (
    compute_position,
    cross,
    dot,
    norm,
    normalize,
    quaternion_conjugate,
    quaternion_mult,
    quaternion_vector_mult,
    vector,
)


class TestReferenceFrame(krpctest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Vessel")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.bodies = cls.space_center.bodies
        cls.kerbin = cls.bodies["Kerbin"]
        cls.mun = cls.bodies["Mun"]
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
        for nm in norms[1:]:
            self.assertAlmostEqual(norms[0], nm, delta=delta)

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
        for dp in dots[1:]:
            self.assertAlmostEqual(dots[0], dp, delta=delta)

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
            (0, 1, 0), self.vessel.direction(self.vessel.reference_frame), places=6
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
            (0, 0, 0, 1), self.vessel.rotation(self.vessel.reference_frame), places=6
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
                self.vessel.direction(ref),
                quaternion_vector_mult(rot, (0, 1, 0)),
                delta=0.01,
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
            self.root_part.center_of_mass(
                self.root_part.center_of_mass_reference_frame
            ),
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
            places=6,
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
            places=6,
        )

    def test_part_rotation_is_unit_quaternion(self):
        """Part rotation is a unit quaternion in every vessel-centered frame."""
        self.check_unit_quaternion(self.root_part.rotation, self._vessel_frames())

    def test_part_rotation_consistent_with_direction(self):
        """Rotating (0,1,0) by the part quaternion recovers the part direction."""
        for ref in self._vessel_frames():
            rot = self.root_part.rotation(ref)
            self.assertAlmostEqual(
                self.root_part.direction(ref),
                quaternion_vector_mult(rot, (0, 1, 0)),
                delta=0.01,
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
        """Docking port distance from Kerbin center equals orbital radius in all Kerbin frames."""
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
            places=6,
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
            places=6,
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
            self.assertAlmostEqual(
                r, norm(self.thruster.thrust_position(ref)), delta=20
            )

    # -------------------------------------------------------------------------
    # Thruster direction tests
    # -------------------------------------------------------------------------

    def test_thrust_direction_in_own_frame(self):
        """Thrust direction is (0,1,0) in the thrust reference frame."""
        self.assertAlmostEqual(
            (0, 1, 0),
            self.thruster.thrust_direction(self.thruster.thrust_reference_frame),
            places=5,
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
        # delta=200: node.UT is fixed at query-start time; vessel moves ~2245 m/s
        # between RPCs, so the prograde error can reach ~100m under normal load.
        self.assertAlmostEqual((0, 0, 0), pos, delta=200)

    def test_node_orbital_position(self):
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        pos = self.vessel.position(node.orbital_reference_frame)
        self.assertAlmostEqual((0, 0, 0), pos, delta=200)

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
            (0, 0, 0, 1), node.rotation(node.reference_frame), places=6
        )

    def test_node_rotation_is_unit_quaternion(self):
        """Node rotation is a unit quaternion in both node frames."""
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        self.assertAlmostEqual(
            1.0, norm(node.rotation(node.reference_frame)), delta=0.01
        )
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

    def test_vessel_speed_same_in_kerbin_non_rotating_and_orbital(self):
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
        self.assertAlmostEqual(
            (0, 0, 0), self.vessel.velocity(hybrid_default), delta=0.5
        )
        speed = norm(self.vessel.velocity(hybrid_kerbin_vel))
        self.assertAlmostEqual(self.vessel.orbit.speed, speed, delta=1)

    def _expected_surface_speed(self):
        """Surface speed computed independently of ReferenceFrame's velocity
        machinery: orbital velocity minus the co-rotation velocity ω×r, all in
        Kerbin's inertial (non-rotating) frame.  This is the ground truth the
        rotating-frame surface velocity must reproduce."""
        nonrot = self.kerbin.non_rotating_reference_frame
        v = vector(self.vessel.velocity(nonrot))  # orbital velocity rel. Kerbin
        r = vector(self.vessel.position(nonrot))  # position rel. Kerbin centre
        w = vector(self.kerbin.angular_velocity(nonrot))  # Kerbin spin vector
        return norm(v - cross(w, r))

    def test_surface_speed_matches_manual_off_equator(self):
        """Surface speed in the body-rotating frame and in the body-position +
        surface-rotation hybrid (the frame the reference-frames tutorial builds)
        both equal the independently computed orbital − ω×r speed, at ~45°
        latitude in an inclined orbit.

        Regression test for #454: the ω×r correction in AngularVelocityAt used
        to combine a world-space angular velocity with a frame-space position,
        which is only valid at the equator.  Off the equator it produced a
        surface velocity error of ~52 m/s.
        """
        # Inclination 45 degrees, observed a quarter orbit past the ascending node
        # (mean anomaly pi/2) so the vessel sits near its peak latitude.
        self.addCleanup(self.set_circular_orbit, "Kerbin", 100000)
        self.set_orbit("Kerbin", 700000, 0, 45, 0, 0, math.pi / 2, 0)
        expected = self._expected_surface_speed()
        hybrid = self.space_center.ReferenceFrame.create_hybrid(
            position=self.kerbin.reference_frame,
            rotation=self.vessel.surface_reference_frame,
        )
        self.assertAlmostEqual(
            expected, norm(self.vessel.velocity(self.kerbin.reference_frame)), delta=1
        )
        self.assertAlmostEqual(expected, norm(self.vessel.velocity(hybrid)), delta=1)

    def test_hybrid_rotation_preserves_speed_off_equator(self):
        """The rotation sub-frame only changes the basis the velocity is
        expressed in, never its magnitude.  In an inclined orbit, observed at
        ~45° latitude, the vessel's surface speed must therefore be identical
        whether measured in Kerbin's rotating frame or in a hybrid that swaps in
        the vessel surface rotation (same position/velocity/angular-velocity
        sub-frames — only the rotation differs).

        Before the #454 fix the buggy ω×r correction was rotation-frame
        dependent, so the two speeds diverged away from the equator.
        """
        # Inclination 45 degrees, observed a quarter orbit past the ascending node
        # (mean anomaly pi/2) so the vessel sits near its peak latitude.
        self.addCleanup(self.set_circular_orbit, "Kerbin", 100000)
        self.set_orbit("Kerbin", 700000, 0, 45, 0, 0, math.pi / 2, 0)
        hybrid = self.space_center.ReferenceFrame.create_hybrid(
            position=self.kerbin.reference_frame,
            rotation=self.vessel.surface_reference_frame,
        )
        speed_body = norm(self.vessel.velocity(self.kerbin.reference_frame))
        speed_hybrid = norm(self.vessel.velocity(hybrid))
        self.assertAlmostEqual(speed_body, speed_hybrid, delta=1)

    def test_node_velocity_zero_at_current_ut(self):
        """Vessel velocity is near zero in the node frame when node.UT equals current time.

        The node frame moves at the orbital velocity at node.UT.  When the node
        is placed at the current UT, that equals the vessel's current orbital
        velocity, so the relative velocity is (0, 0, 0).
        Before the fix this returned ~2245 m/s (the raw orbital speed).
        """
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        self.assertAlmostEqual(
            (0, 0, 0), self.vessel.velocity(node.reference_frame), delta=1
        )

    def test_node_orbital_velocity_zero_at_current_ut(self):
        """Vessel velocity is near zero in the node's orbital frame when node.UT equals
        current time.

        Same reasoning as test_node_velocity_zero_at_current_ut — both node frames
        use the orbital velocity at node.UT as their own frame velocity.
        """
        for node in self.vessel.control.nodes:
            node.remove()
        node = self.vessel.control.add_node(self.space_center.ut, 100, 0, 0)
        self.assertAlmostEqual(
            (0, 0, 0), self.vessel.velocity(node.orbital_reference_frame), delta=1
        )

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
    # Navball speed mode frame tests
    #
    # One frame per navball speed mode. Each is centered on the vessel, oriented
    # with the prograde/normal/radial directions of the motion that mode measures
    # (the directions the navball marks in it) and moving with what that motion is
    # measured against. The vessel's velocity in the frame is therefore the
    # velocity the navball shows, pointing along the frame's y-axis. The target
    # mode tests set a target and clear it again, since the class-wide setup
    # deliberately leaves no target set.
    # -------------------------------------------------------------------------

    def _speed_mode_frames(self):
        """The orbit and surface speed frames. The target frame is excluded as it
        needs a target set."""
        return [
            self.vessel.orbit_speed_reference_frame,
            self.vessel.surface_speed_reference_frame,
        ]

    def _axes_in_non_rotating_frame(self, ref):
        """The x, y and z axes of the given frame, as directions in Kerbin's
        non-rotating reference frame."""
        nonrot = self.kerbin.non_rotating_reference_frame
        return [
            vector(self.space_center.transform_direction(axis, ref, nonrot))
            for axis in ((1, 0, 0), (0, 1, 0), (0, 0, 1))
        ]

    def _velocity_in_non_rotating_frame(self, ref):
        """The vessel's velocity in the given frame, as a vector in Kerbin's
        non-rotating reference frame."""
        return vector(
            self.space_center.transform_direction(
                self.vessel.velocity(ref), ref, self.kerbin.non_rotating_reference_frame
            )
        )

    def _check_velocity_frame_axes(self, ref, velocity):
        """Check that the given frame is oriented by the given velocity, which is a
        direction in Kerbin's non-rotating reference frame.

        The y-axis points along the velocity, the z-axis is normal to the plane the
        motion sweeps out (velocity crossed with the zenith) and the x-axis is
        anti-radial, completing the frame the same way the orbital frame does.
        """
        nonrot = self.kerbin.non_rotating_reference_frame
        zenith = self.vessel.position(nonrot)
        x_axis, y_axis, z_axis = self._axes_in_non_rotating_frame(ref)
        self.assertAlmostEqual(1, dot(y_axis, normalize(velocity)), places=3)
        self.assertAlmostEqual(
            1, dot(z_axis, normalize(cross(velocity, zenith))), places=3
        )
        self.assertAlmostEqual(tuple(cross(y_axis, z_axis)), tuple(x_axis), places=3)
        # The x-axis points back towards the body, as the orbital frame's does
        self.assertLess(dot(x_axis, normalize(zenith)), -0.9)

    def test_speed_mode_frames_centered_on_vessel(self):
        """The vessel is at the origin of every speed mode frame."""
        for ref in self._speed_mode_frames():
            self.assertAlmostEqual((0, 0, 0), self.vessel.position(ref), delta=1)

    def test_speed_mode_frames_velocity_along_prograde_axis(self):
        """The frames are oriented by the velocity they measure, so that velocity has
        no x or z component -- it points along the y-axis, at the navball's prograde
        marker."""
        for ref in self._speed_mode_frames():
            velocity = self.vessel.velocity(ref)
            self.assertAlmostEqual(0, velocity[0], delta=1)
            self.assertAlmostEqual(0, velocity[2], delta=1)
            self.assertGreater(velocity[1], 0)

    def test_orbit_speed_frame_uses_orbital_axes(self):
        """The navball marks the orbital prograde/normal/radial directions in 'orbit'
        mode, so the orbit speed frame is oriented like the orbital reference frame.
        Only the velocity of the frame differs between the two."""
        self.assertQuaternionsAlmostEqual(
            self.vessel.rotation(self.vessel.orbital_reference_frame),
            self.vessel.rotation(self.vessel.orbit_speed_reference_frame),
            places=4,
        )

    def test_orbit_speed_frame_axes_are_navball_directions(self):
        """The frame's axes point where the navball's markers do in 'orbit' mode: the
        y-axis at prograde, the z-axis at normal and the x-axis at anti-radial."""
        ref = self.vessel.orbit_speed_reference_frame
        flight = self.vessel.flight(self.kerbin.non_rotating_reference_frame)
        x_axis, y_axis, z_axis = self._axes_in_non_rotating_frame(ref)
        self.assertAlmostEqual(1, dot(y_axis, vector(flight.prograde)), places=3)
        self.assertAlmostEqual(1, dot(z_axis, vector(flight.normal)), places=3)
        self.assertAlmostEqual(1, dot(x_axis, vector(flight.anti_radial)), places=3)

    def test_surface_speed_frame_axes(self):
        """The surface speed frame is oriented by the vessel's velocity relative to
        the surface, which is where the navball's prograde marker points in 'surface'
        mode."""
        body_frame = self.kerbin.reference_frame
        velocity = vector(
            self.space_center.transform_direction(
                self.vessel.velocity(body_frame),
                body_frame,
                self.kerbin.non_rotating_reference_frame,
            )
        )
        self._check_velocity_frame_axes(
            self.vessel.surface_speed_reference_frame, velocity
        )

    def test_orbit_speed_frame_gives_orbital_speed(self):
        """The orbit speed frame moves with Kerbin without rotating with it, so the
        vessel's speed in it is its orbital speed -- the navball's 'orbit' mode."""
        speed = norm(self.vessel.velocity(self.vessel.orbit_speed_reference_frame))
        self.assertAlmostEqual(self.vessel.orbit.speed, speed, delta=1)

    def test_surface_speed_frame_gives_surface_speed(self):
        """The surface speed frame moves with the point of the rotating body below the
        vessel, so the vessel's speed in it is its surface speed -- the navball's
        'surface' mode. Checked against Kerbin's rotating frame and against the
        independently computed orbital − ω×r speed."""
        speed = norm(self.vessel.velocity(self.vessel.surface_speed_reference_frame))
        speed_body = norm(self.vessel.velocity(self.kerbin.reference_frame))
        self.assertAlmostEqual(speed_body, speed, delta=1)
        self.assertAlmostEqual(self._expected_surface_speed(), speed, delta=1)

    def test_surface_speed_frame_velocity_is_the_navball_velocity(self):
        """The velocity in the surface speed frame is the surface velocity, the vector
        the navball's prograde marker follows in 'surface' mode.

        Compared against transforming the velocity from Kerbin's rotating frame into
        the speed frame, which is the same vector computed the long way round.
        """
        ref = self.vessel.surface_speed_reference_frame
        body_frame = self.kerbin.reference_frame
        expected = self.space_center.transform_direction(
            self.vessel.velocity(body_frame), body_frame, ref
        )
        velocity = self.vessel.velocity(ref)
        self.assertAlmostEqual(expected, velocity, delta=1)

    def test_surface_speed_frame_off_equator(self):
        """The body rotation term is taken at the vessel, in world space, so the
        surface speed frame is correct away from the equator too. Checked at ~45°
        latitude in an inclined orbit, where the co-rotation velocity is neither
        aligned with nor perpendicular to the orbital velocity."""
        # Inclination 45 degrees, observed a quarter orbit past the ascending node
        # (mean anomaly pi/2) so the vessel sits near its peak latitude.
        self.addCleanup(self.set_circular_orbit, "Kerbin", 100000)
        self.set_orbit("Kerbin", 700000, 0, 45, 0, 0, math.pi / 2, 0)
        expected = self._expected_surface_speed()
        speed = norm(self.vessel.velocity(self.vessel.surface_speed_reference_frame))
        self.assertAlmostEqual(expected, speed, delta=1)

    def test_speed_mode_frames_differ_by_body_rotation(self):
        """The orbit and surface speed frames differ by the velocity of the ground
        beneath the vessel, so their velocities differ by ω×r. The two frames are
        oriented differently, so the comparison is made in Kerbin's non-rotating
        frame."""
        nonrot = self.kerbin.non_rotating_reference_frame
        r = vector(self.vessel.position(nonrot))
        w = vector(self.kerbin.angular_velocity(nonrot))
        orbit_velocity = self._velocity_in_non_rotating_frame(
            self.vessel.orbit_speed_reference_frame
        )
        surface_velocity = self._velocity_in_non_rotating_frame(
            self.vessel.surface_speed_reference_frame
        )
        self.assertAlmostEqual(
            tuple(orbit_velocity - vector(cross(w, r))),
            tuple(surface_velocity),
            delta=1,
        )

    def test_target_speed_frame_gives_relative_speed(self):
        """The target speed frame moves with the target, so the vessel's speed in it is
        its speed relative to the target -- the navball's 'target' mode."""
        self.space_center.target_body = self.mun
        try:
            nonrot = self.kerbin.non_rotating_reference_frame
            expected = norm(
                vector(self.vessel.velocity(nonrot)) - vector(self.mun.velocity(nonrot))
            )
            speed = norm(self.vessel.velocity(self.vessel.target_speed_reference_frame))
            self.assertAlmostEqual(expected, speed, delta=1)
        finally:
            self.space_center.target_body = None

    def test_target_speed_frame_axes(self):
        """The target speed frame is oriented by the vessel's velocity relative to the
        target, which is where the navball's prograde marker points in 'target' mode,
        and the velocity in it points along the y-axis."""
        self.space_center.target_body = self.mun
        try:
            nonrot = self.kerbin.non_rotating_reference_frame
            velocity = vector(self.vessel.velocity(nonrot)) - vector(
                self.mun.velocity(nonrot)
            )
            ref = self.vessel.target_speed_reference_frame
            self._check_velocity_frame_axes(ref, velocity)
            in_frame = self.vessel.velocity(ref)
            self.assertAlmostEqual(0, in_frame[0], delta=1)
            self.assertAlmostEqual(0, in_frame[2], delta=1)
            self.assertGreater(in_frame[1], 0)
        finally:
            self.space_center.target_body = None

    def test_target_speed_frame_requires_a_target(self):
        """With no target set, using the target speed reference frame raises."""
        self.space_center.target_body = None
        ref = self.vessel.target_speed_reference_frame
        self.assertRaises(RuntimeError, self.vessel.velocity, ref)

    # -------------------------------------------------------------------------
    # Angular velocity tests
    # -------------------------------------------------------------------------

    def test_vessel_angular_velocity_zero_in_vessel_frame(self):
        """Vessel co-rotates with its own body frame, so its angular velocity is zero there."""
        self.assertAlmostEqual(
            (0, 0, 0),
            self.vessel.angular_velocity(self.vessel.reference_frame),
            delta=0.01,
        )

    def test_kerbin_angular_velocity_zero_in_rotating_frame(self):
        """Kerbin co-rotates with its rotating frame, so it appears stationary in that frame."""
        self.assertAlmostEqual(
            (0, 0, 0),
            self.kerbin.angular_velocity(self.kerbin.reference_frame),
            delta=1e-4,
        )

    def test_kerbin_angular_velocity_magnitude_in_non_rotating_frame(self):
        """In the inertial (non-rotating) frame, Kerbin spins at its rotational speed."""
        ang_vel = self.kerbin.angular_velocity(self.kerbin.non_rotating_reference_frame)
        self.assertAlmostEqual(self.kerbin.rotational_speed, norm(ang_vel), delta=1e-5)

    def test_kerbin_angular_velocity_in_vessel_surface_frame(self):
        """The vessel surface frame does NOT co-rotate with Kerbin: its zenith axis
        sweeps with the vessel's orbit, so its angular velocity is the orbital rate,
        not the planetary rotation rate. Kerbin (spinning at its rotational speed)
        therefore appears to rotate at the difference of the two (both along the spin
        axis for an equatorial orbit).
        """
        ang_vel = self.kerbin.angular_velocity(self.vessel.surface_reference_frame)
        orbital_angular_speed = 2 * math.pi / self.vessel.orbit.period
        expected = orbital_angular_speed - self.kerbin.rotational_speed
        self.assertAlmostEqual(expected, norm(ang_vel), delta=1e-4)

    def _frame_angular_velocity(self, ref):
        """The angular velocity of the given frame itself, as a vector in Kerbin's
        non-rotating reference frame.

        Taken from how fast Kerbin appears to rotate in the frame: that is Kerbin's
        angular velocity less the frame's own, so the frame's is the difference.
        """
        nonrot = self.kerbin.non_rotating_reference_frame
        relative = self.space_center.transform_direction(
            self.kerbin.angular_velocity(ref), ref, nonrot
        )
        return vector(self.kerbin.angular_velocity(nonrot)) - vector(relative)

    def _measured_angular_velocity(self, ref, dt=1):
        """The angular velocity of the given frame, measured by how far its axes turn
        over dt seconds, as a vector in Kerbin's non-rotating reference frame.

        For a basis whose axes obey de/dt = ω × e, ω = ½ Σ e × de/dt.
        """
        before = self._axes_in_non_rotating_frame(ref)
        start = self.space_center.ut
        self.wait(dt)
        after = self._axes_in_non_rotating_frame(ref)
        elapsed = self.space_center.ut - start
        angular_velocity = vector((0, 0, 0))
        for axis, moved in zip(before, after):
            rate = [(b - a) / elapsed for a, b in zip(axis, moved)]
            angular_velocity = angular_velocity + vector(cross(axis, rate))
        return 0.5 * angular_velocity

    def test_orbit_speed_frame_angular_velocity(self):
        """The orbit speed frame shares the orbital frame's axes, so it turns with it."""
        self.assertAlmostEqual(
            self.kerbin.angular_velocity(self.vessel.orbital_reference_frame),
            self.kerbin.angular_velocity(self.vessel.orbit_speed_reference_frame),
            delta=1e-5,
        )

    def test_surface_speed_frame_angular_velocity(self):
        """The surface speed frame's angular velocity is the rate its own axes turn:
        the swing of the surface velocity direction plus the roll about it as the plane
        through that velocity and the zenith turns."""
        ref = self.vessel.surface_speed_reference_frame
        self.assertAlmostEqual(
            tuple(self._measured_angular_velocity(ref)),
            tuple(self._frame_angular_velocity(ref)),
            delta=1e-4,
        )

    def test_target_speed_frame_angular_velocity(self):
        """The target speed frame's axes turn with the velocity relative to the target,
        which changes as the two fall through different gravity."""
        self.space_center.target_body = self.mun
        try:
            ref = self.vessel.target_speed_reference_frame
            self.assertAlmostEqual(
                tuple(self._measured_angular_velocity(ref)),
                tuple(self._frame_angular_velocity(ref)),
                delta=1e-4,
            )
        finally:
            self.space_center.target_body = None

    def test_kerbin_angular_velocity_in_surface_velocity_frame(self):
        """Kerbin's angular velocity in the surface velocity frame is dominated by the
        centripetal term (orbital angular speed ≈ v/r), not body rotation.

        The surface velocity frame rotates at body.ω + centripetal_term, so
        Kerbin (rotating at body.ω) appears to rotate backward at ≈ -centripetal_term.
        For a 100 km circular orbit: |centripetal| ≈ v_orb/r ≈ 3.2e-3 rad/s.
        """
        ang_vel = self.kerbin.angular_velocity(
            self.vessel.surface_velocity_reference_frame
        )
        # Expected magnitude: approximately the orbital angular speed
        orbital_angular_speed = 2 * math.pi / self.vessel.orbit.period
        self.assertAlmostEqual(orbital_angular_speed, norm(ang_vel), delta=5e-4)

    def test_vessel_angular_velocity_surface_frame_low_noise(self):
        """Regression for #351: a torque-free vessel's angular velocity in the
        surface reference frame must not be noisier than in the non-rotating frame.

        #351 reported sign-alternating ~0.1 deg/s (~1.7e-3 rad/s) jitter in the
        surface frame that was absent from the non-rotating frame. Both are derived
        from the same rigidbody angular velocity and differ only by a rotation and a
        frame-angular-velocity subtraction, so the surface frame cannot inject
        frame-specific noise. With the vessel de-spun the per-tick spread is inherent
        PhysX float noise (~1e-5 rad/s) and is the same in both frames.
        """
        self.set_circular_orbit("Kerbin", 100000)
        self.vessel.control.sas = False
        self.vessel.control.rcs = False
        self.set_pitch_heading_roll(0, 90, 0)  # de-spin, level, facing east
        self.wait(1)
        surf = self.vessel.surface_reference_frame
        nonrot = self.kerbin.non_rotating_reference_frame
        surf_samples = []
        nonrot_samples = []
        for _ in range(30):
            surf_samples.append(self.vessel.angular_velocity(surf))
            nonrot_samples.append(self.vessel.angular_velocity(nonrot))
            self.wait(0.05)

        def peak_to_peak(samples):
            # Largest per-component spread across the samples (rad/s).
            return max(max(c) - min(c) for c in zip(*samples))

        surf_noise = peak_to_peak(surf_samples)
        nonrot_noise = peak_to_peak(nonrot_samples)
        # Comfortably below the ~1.7e-3 rad/s (~0.1 deg/s) reported in #351.
        self.assertLess(surf_noise, 5e-4)
        # The surface frame must not be materially noisier than the non-rotating
        # frame, since both come from the same rigidbody angular velocity.
        self.assertLess(surf_noise, nonrot_noise + 2e-4)

    def test_vessel_angular_velocity_surface_frame_equatorial(self):
        """The surface frame's zenith axis sweeps with the vessel's orbit, so a
        torque-free (inertially-fixed) vessel's angular velocity in that frame equals
        the orbital angular speed -- NOT the body's rotation rate, which is ~11x
        smaller (the frame previously returned body.angularVelocity). On an equatorial
        orbit there is no twist about the zenith, so the up (x) component is ~zero.
        """
        self.set_circular_orbit("Kerbin", 100000)
        self.vessel.control.sas = False
        self.vessel.control.rcs = False
        self.set_pitch_heading_roll(0, 90, 0)  # de-spin (inertially fixed)
        self.wait(1)
        ang_vel = self.vessel.angular_velocity(self.vessel.surface_reference_frame)
        orbital_angular_speed = 2 * math.pi / self.vessel.orbit.period
        self.assertAlmostEqual(orbital_angular_speed, norm(ang_vel), delta=1e-4)
        # Locks out the body.angularVelocity reading, at one times rotational speed.
        self.assertGreater(norm(ang_vel), 5 * self.kerbin.rotational_speed)
        # No twist about the zenith (x-axis) on an equatorial orbit.
        self.assertAlmostEqual(0.0, ang_vel[0], delta=5e-5)

    def test_vessel_angular_velocity_surface_frame_inclined_twist(self):
        """On an inclined orbit at high latitude the surface frame also twists about
        its zenith axis as the vessel's latitude changes, so an inertially-fixed
        vessel's angular velocity has a large zenith (x-axis) component. A sweep-only
        model (Cross(r, v) / r^2) would miss this term and report ~zero there.
        """
        # 60 deg inclination, argument of periapsis 90 deg, and an epoch of now, so the
        # vessel is teleported to periapsis, its highest latitude, independent of orbital
        # phase.
        self.addCleanup(self.set_circular_orbit, "Kerbin", 100000)
        self.set_orbit(
            "Kerbin", 700000, 0.0, 60.0, 0.0, 90.0, 0.0, self.space_center.ut
        )
        self.vessel.control.sas = False
        self.vessel.control.rcs = False
        self.set_pitch_heading_roll(0, 90, 0)  # de-spin (inertially fixed)
        self.wait(1)
        # The twist term is large only away from the equator; confirm we are there.
        self.assertGreater(abs(self.vessel.flight().latitude), 30)
        ang_vel = self.vessel.angular_velocity(self.vessel.surface_reference_frame)
        # Large twist about the zenith (x-axis); a sweep-only model gives ~zero here.
        self.assertGreater(abs(ang_vel[0]), 1e-3)

    def test_vessel_velocity_zero_in_own_orbital_frame(self):
        """A vessel's velocity in its own orbital reference frame is zero.

        The orbital frame rotates with the vessel's orbit, so the vessel's own
        velocity vector is always the y-axis of that frame — the residual should
        be zero (just the Coriolis correction for CoM, which has zero offset).
        """
        vel = self.vessel.velocity(self.vessel.orbital_reference_frame)
        self.assertAlmostEqual((0, 0, 0), vel, delta=0.5)

    def test_celestial_body_orbital_angular_velocity_nonzero(self):
        """CelestialBodyOrbital frame has a non-zero angular velocity (Kerbin's orbital ω
        around Kerbol ≈ 6.8e-7 rad/s).

        angular_velocity() subtracts the frame's ω from Kerbin's world spin, so the measured
        speed ≈ kerbin.rotational_speed (dominant at 2.9e-4 rad/s) to within 1e-3.
        """
        kerbin = self.space_center.bodies["Kerbin"]
        ang_vel = kerbin.angular_velocity(kerbin.orbital_reference_frame)
        self.assertAlmostEqual(kerbin.rotational_speed, norm(ang_vel), delta=1e-3)

    def test_part_angular_velocity_zero_in_vessel_frame(self):
        """A part co-rotates with the vessel, so the vessel appears stationary in the
        part frame — same angular velocity as the vessel body frame."""
        self.assertAlmostEqual(
            (0, 0, 0),
            self.vessel.angular_velocity(self.root_part.reference_frame),
            delta=0.01,
        )

    def test_docking_port_angular_velocity_matches_vessel(self):
        """DockingPort frame angular velocity equals the vessel's angular velocity.

        The vessel's angular velocity as seen from its own body frame is zero, so
        the vessel's angular velocity in the docking port frame (which shares the
        same ω) should also be zero.
        """
        self.assertAlmostEqual(
            (0, 0, 0),
            self.vessel.angular_velocity(self.docking_port.reference_frame),
            delta=0.01,
        )

    def test_relative_frame_angular_velocity_offset(self):
        """An angular velocity offset on a relative frame shifts the measured angular velocity.

        Adding (0, 1, 0) rad/s (about the vessel y-axis) to the vessel body frame
        makes that frame spin faster than the vessel.  The vessel therefore appears
        to rotate at (0, -1, 0) in the new frame — opposite sign because the frame
        rotates rather than the vessel.
        """
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, angular_velocity=(0, 1, 0)
        )
        self.assertAlmostEqual(
            (0, -1, 0), self.vessel.angular_velocity(ref), delta=0.01
        )

    def test_hybrid_angular_velocity_source_respected(self):
        """create_hybrid respects the angular_velocity= sub-frame argument.

        A default hybrid (no angular_velocity override) inherits angular velocity
        from the vessel frame, so Kerbin's spin is visible.  A hybrid that takes
        its angular velocity from Kerbin's rotating frame subtracts Kerbin's own
        spin, making Kerbin appear stationary.
        """
        hybrid_default = self.space_center.ReferenceFrame.create_hybrid(
            position=self.vessel.reference_frame
        )
        hybrid_kerbin_rot = self.space_center.ReferenceFrame.create_hybrid(
            position=self.vessel.reference_frame,
            angular_velocity=self.kerbin.reference_frame,
        )
        # Default hybrid uses vessel angular velocity (~0 for a non-spinning vessel);
        # Kerbin's spin is visible with magnitude equal to its rotational speed.
        ang_speed_default = norm(self.kerbin.angular_velocity(hybrid_default))
        self.assertAlmostEqual(
            self.kerbin.rotational_speed, ang_speed_default, delta=1e-3
        )
        # Hybrid with Kerbin's rotating frame subtracts Kerbin's angular velocity,
        # making Kerbin appear stationary.
        self.assertAlmostEqual(
            (0, 0, 0), self.kerbin.angular_velocity(hybrid_kerbin_rot), delta=1e-4
        )

    # -------------------------------------------------------------------------
    # SpaceCenter.transform_position tests
    # -------------------------------------------------------------------------

    def test_transform_position_round_trip(self):
        """transform_position A→B→A returns the original position."""
        ref_a = self.kerbin.non_rotating_reference_frame
        pos = self.vessel.position(ref_a)
        for ref_b in self._vessel_frames():
            via = self.space_center.transform_position(pos, ref_a, ref_b)
            roundtrip = self.space_center.transform_position(via, ref_b, ref_a)
            self.assertAlmostEqual(pos, roundtrip, delta=200)

    def test_transform_position_same_frame_is_identity(self):
        """transform_position from a frame to itself returns the input unchanged."""
        ref = self.kerbin.non_rotating_reference_frame
        pos = self.vessel.position(ref)
        result = self.space_center.transform_position(pos, ref, ref)
        self.assertAlmostEqual(pos, result, delta=0.01)

    def test_transform_position_vessel_origin_gives_orbital_radius(self):
        """Transforming the vessel origin (0,0,0) to the Kerbin frame gives the orbital radius.

        The vessel is always at (0,0,0) in its own frame.  Transforming that to
        a Kerbin-centered frame expresses the vessel's world position relative to
        Kerbin, whose magnitude must equal the orbital radius.
        """
        origin = self.vessel.position(self.vessel.reference_frame)  # (0, 0, 0)
        pos_in_kerbin = self.space_center.transform_position(
            origin,
            self.vessel.reference_frame,
            self.kerbin.non_rotating_reference_frame,
        )
        self.assertAlmostEqual(self.vessel.orbit.radius, norm(pos_in_kerbin), delta=20)

    # -------------------------------------------------------------------------
    # SpaceCenter.transform_direction tests
    # -------------------------------------------------------------------------

    def test_transform_direction_round_trip(self):
        """transform_direction A→B→A returns the original direction."""
        ref_a = self.vessel.reference_frame
        direction = self.vessel.direction(ref_a)  # (0, 1, 0)
        for ref_b in self._kerbin_frames():
            via = self.space_center.transform_direction(direction, ref_a, ref_b)
            roundtrip = self.space_center.transform_direction(via, ref_b, ref_a)
            self.assertAlmostEqual(direction, roundtrip, delta=0.01)

    def test_transform_direction_same_frame_is_identity(self):
        """transform_direction from a frame to itself returns the input unchanged."""
        ref = self.vessel.reference_frame
        result = self.space_center.transform_direction((0, 1, 0), ref, ref)
        self.assertAlmostEqual((0, 1, 0), result, delta=0.01)

    def test_transform_direction_preserves_magnitude(self):
        """transform_direction does not change the vector magnitude."""
        ref_a = self.vessel.reference_frame
        direction = self.vessel.direction(ref_a)  # unit vector
        for ref_b in self._kerbin_frames():
            transformed = self.space_center.transform_direction(direction, ref_a, ref_b)
            self.assertAlmostEqual(norm(direction), norm(transformed), delta=0.001)

    def test_transform_direction_consistent_with_vessel_direction(self):
        """transform_direction matches querying vessel direction directly in the target frame.

        The vessel nose is always (0,1,0) in the vessel frame.  Transforming
        that to any other frame must equal vessel.direction(that frame).  Vessel
        orientation changes slowly enough that back-to-back RPC calls agree
        within 0.01.
        """
        ref_a = self.vessel.reference_frame
        direction = self.vessel.direction(ref_a)  # (0, 1, 0)
        for ref_b in self._vessel_frames() + self._kerbin_frames():
            transformed = self.space_center.transform_direction(direction, ref_a, ref_b)
            direct = self.vessel.direction(ref_b)
            self.assertAlmostEqual(transformed, direct, delta=0.01)

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
        self.assertAlmostEqual((0, 1, 0), self.vessel.direction(ref), places=6)

    def test_relative_rotation(self):
        """Rotation is unaffected by a position-only offset in a relative frame."""
        ref = self.space_center.ReferenceFrame.create_relative(
            self.vessel.reference_frame, position=(1, 2, 3)
        )
        self.assertQuaternionsAlmostEqual(
            (0, 0, 0, 1), self.vessel.rotation(ref), places=6
        )

    def test_hybrid_missing_components_come_from_position(self):
        # Passing a component explicitly as null is how a client asks for the default,
        # and it has to give the same frame as leaving the argument out.
        frames = self.space_center.ReferenceFrame
        given = frames.create_hybrid(self.vessel.reference_frame, None, None, None)
        left_out = frames.create_hybrid(self.vessel.reference_frame)
        self.assertAlmostEqual(
            self.vessel.direction(left_out), self.vessel.direction(given), places=6
        )
        self.assertAlmostEqual(
            self.vessel.position(left_out), self.vessel.position(given), places=6
        )
        given.remove()
        left_out.remove()

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
        self.assertAlmostEqual((0, 1, 0), self.vessel.direction(ref), places=6)

    def test_hybrid_rotation(self):
        """Vessel rotation is identity in a hybrid frame using vessel rotation."""
        ref = self.space_center.ReferenceFrame.create_hybrid(
            position=self.vessel.reference_frame
        )
        self.assertQuaternionsAlmostEqual(
            (0, 0, 0, 1), self.vessel.rotation(ref), places=6
        )


class TestOrbitReferenceFrame(krpctest.TestCase):
    """The frames centered on the point an orbit has reached.

    These are placed by evaluating the orbit rather than by reading a transform,
    so they work for an orbit that no object in the game is following."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("Vessel")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 100000)
        cls.space_center = cls.connect().space_center
        cls.vessel = cls.space_center.active_vessel
        cls.kerbin = cls.space_center.bodies["Kerbin"]
        cls.nonrot = cls.kerbin.non_rotating_reference_frame

    def constructed_orbit(self, radius=1000000):
        """An orbit no object in the game is following: circular, polar, and well
        clear of the vessel's own orbit."""
        speed = math.sqrt(self.kerbin.gravitational_parameter / radius)
        return self.space_center.Orbit.create_from_position_and_velocity(
            self.kerbin,
            (0, 0, radius),
            (0, speed, 0),
            self.space_center.ut,
            self.nonrot,
        )

    def test_origin_is_on_the_orbit(self):
        """The frame origin is where the orbit has reached, at its orbital radius
        from the body being orbited."""
        orbit = self.constructed_orbit()
        for ref in (orbit.reference_frame, orbit.orbital_reference_frame):
            self.assertAlmostEqual(
                (0, 0, 0), orbit.position_at(self.space_center.ut, ref), delta=200
            )
            self.assertAlmostEqual(
                orbit.radius, norm(self.kerbin.position(ref)), delta=200
            )

    def test_vessel_orbit_frame_tracks_the_vessel(self):
        """For a vessel's own orbit, the origin is where that vessel's orbit has
        reached. It is compared against the orbit rather than against the vessel
        itself, which is simulated inside the physics bubble and sits a few
        meters off the orbit it is on."""
        ref = self.vessel.orbit.reference_frame
        self.assertAlmostEqual(
            (0, 0, 0),
            self.vessel.orbit.position_at(self.space_center.ut, ref),
            delta=200,
        )
        self.assertAlmostEqual((0, 0, 0), self.vessel.position(ref), delta=200)

    def test_non_rotating_frame_axes_are_fixed(self):
        """The axes point in the same fixed directions as the body's non-rotating
        frame, so a direction has the same components in both."""
        ref = self.constructed_orbit().reference_frame
        for direction in ((1, 0, 0), (0, 1, 0), (0, 0, 1)):
            self.assertAlmostEqual(
                direction,
                self.space_center.transform_direction(direction, self.nonrot, ref),
                places=6,
            )

    def test_non_rotating_frame_has_no_angular_velocity(self):
        """Kerbin's spin measured in the frame is what it is in Kerbin's own
        non-rotating frame: neither frame turns, and they share their axes.

        Angular velocity can only be observed through some rotating object, and
        Kerbin's spin is the one to hand -- so this is the way to ask whether the
        frame itself contributes any rotation."""
        ref = self.constructed_orbit().reference_frame
        self.assertAlmostEqual(
            self.kerbin.angular_velocity(self.nonrot),
            self.kerbin.angular_velocity(ref),
            places=9,
        )

    def test_frame_velocity_is_the_orbital_velocity(self):
        """The frame moves along the orbit, so the body being orbited recedes at
        the orbit's own speed.

        Only the non-rotating frame shows this. In the orbital frame the origin
        turns with the orbit, and for a circular one the omega-cross-r term at
        the body's center is equal and opposite to the frame's own motion, so
        the body sits still there."""
        radius = 1000000
        expected = math.sqrt(self.kerbin.gravitational_parameter / radius)
        orbit = self.constructed_orbit(radius)
        speed = norm(self.kerbin.velocity(orbit.reference_frame))
        self.assertAlmostEqual(expected, speed, delta=1)

    def test_orbital_frame_corotates_with_a_circular_orbit(self):
        """The body being orbited is at rest in the orbital frame of a circular
        orbit: the frame's motion along the orbit and its rotation about the body
        cancel at the body's center."""
        orbit = self.constructed_orbit()
        speed = norm(self.kerbin.velocity(orbit.orbital_reference_frame))
        self.assertAlmostEqual(0, speed, delta=1)

    def test_orbital_frame_axes(self):
        """The y-axis is prograde and the z-axis is the orbit normal, so the
        orbit's own velocity points along +y and its position is along -x."""
        orbit = self.constructed_orbit()
        ref = orbit.orbital_reference_frame
        ut = self.space_center.ut
        velocity = orbit.velocity_at(ut, ref)
        # The frame moves with the orbit, so the orbit is at rest in it. Take the
        # direction from the non-rotating frame and rotate it in instead.
        prograde = normalize(vector(orbit.velocity_at(ut, self.nonrot)))
        self.assertAlmostEqual(
            (0, 1, 0),
            self.space_center.transform_direction(tuple(prograde), self.nonrot, ref),
            places=4,
        )
        self.assertAlmostEqual((0, 0, 0), velocity, delta=5)
        # The body being orbited is in the anti-radial direction, along +x.
        position = normalize(vector(self.kerbin.position(ref)))
        self.assertAlmostEqual(1, dot(position, vector((1, 0, 0))), places=4)

    def test_orbital_frame_angular_velocity_is_the_orbital_rate(self):
        """The frame turns once per orbit.

        Kerbin's spin read in the frame is that spin minus the frame's own
        rotation. The orbit is polar, so the frame turns about an axis lying in
        the equatorial plane, at right angles to Kerbin's spin axis, and the two
        rates combine in quadrature."""
        orbit = self.constructed_orbit()
        spin = norm(self.kerbin.angular_velocity(orbit.reference_frame))
        combined = norm(self.kerbin.angular_velocity(orbit.orbital_reference_frame))
        rate = math.sqrt(combined**2 - spin**2)
        self.assertAlmostEqual(2 * math.pi / orbit.period, rate, delta=1e-6)

    def test_composes_with_relative_and_hybrid(self):
        """The frames are ordinary reference frames, so they compose.

        The offset is large compared to the distance the orbit covers between
        the two RPCs this compares, so it is the offset being measured."""
        orbit = self.constructed_orbit()
        offset = self.space_center.ReferenceFrame.create_relative(
            orbit.reference_frame, position=(0, 0, 100000)
        )
        self.assertAlmostEqual(
            (0, 0, -100000), orbit.position_at(self.space_center.ut, offset), delta=200
        )
        hybrid = self.space_center.ReferenceFrame.create_hybrid(
            position=orbit.reference_frame,
            rotation=self.vessel.reference_frame,
        )
        self.assertAlmostEqual(
            (0, 0, 0), orbit.position_at(self.space_center.ut, hybrid), delta=200
        )

    def test_distance_to_a_constructed_orbit(self):
        """The use the frames exist for: measure a vessel against a point that
        nothing in the game is at."""
        orbit = self.constructed_orbit()
        position = self.vessel.position(orbit.reference_frame)
        expected = norm(
            vector(self.vessel.position(self.nonrot))
            - vector(orbit.position_at(self.space_center.ut, self.nonrot))
        )
        self.assertAlmostEqual(expected, norm(position), delta=200)


if __name__ == "__main__":
    unittest.main()
