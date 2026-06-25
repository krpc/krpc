import krpctest


class TestPartsDestroyed(krpctest.TestCase):
    """Accessing a part, or one of its part modules, after the underlying part has been
    destroyed raises PartDestroyedException, rather than a server-side
    NullReferenceException or returning stale data (issue #885)."""

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.sc = cls.connect().space_center

    def test_destroyed_part(self):
        # Launch a vessel and take proxies to a part and one of its part modules.
        self.launch_vessel_from_vab("PartsParachute")
        self.remove_other_vessels()
        vessel = self.sc.active_vessel
        part = vessel.parts.root
        parachute = vessel.parts.parachutes[0]
        # A reference frame that outlives the vessel, for the method call below.
        body_frame = self.sc.bodies["Kerbin"].reference_frame

        # Sanity check: while the part exists the proxies resolve normally. Without this
        # the assertions below could pass even if nothing had actually been destroyed.
        self.assertIsNotNone(part.name)
        self.assertIsNotNone(parachute.state)

        # Destroy the part by recovering its vessel: launching another vessel from the VAB
        # recovers the one currently on the pad (recover defaults to True), removing its
        # parts. This is a new launch rather than a save load, so it does not trigger the
        # object-store sweep and the cached proxies stay registered while the parts they
        # refer to no longer exist. The vessel is gone for good, so the destroyed state is
        # stable (unlike a crash, where it is transient).
        self.launch_vessel_from_vab("PartsParachute")
        self.remove_other_vessels()

        # Accessing the now-destroyed part raises PartDestroyedException, for a property...
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = part.name
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = part.title
        # ...for a method...
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = part.position(body_frame)
        # ...and for a part module property (the case reported in issue #885).
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = parachute.state
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = parachute.deployed


class TestPartsDestroyedByCrash(krpctest.TestCase):
    """Parts and part modules raise PartDestroyedException once the underlying part is
    destroyed by a crash -- the realistic scenario from issue #885 where a vessel impacts
    the ground without its parachutes deployed. The PartsParachute craft is destroyed on
    landing unless its parachutes are opened."""

    def setUp(self):
        self.new_save()
        self.launch_vessel_from_vab("PartsParachute")
        self.remove_other_vessels()
        self.sc = self.connect().space_center
        self.vessel = self.sc.active_vessel

    def test_crash_destroys_parts(self):
        parachute = self.vessel.parts.parachutes[0]
        part = parachute.part
        body_frame = self.vessel.orbit.body.reference_frame
        flight = self.vessel.flight(body_frame)

        # The proxies resolve while the vessel is intact.
        self.assertIsNotNone(part.name)
        self.assertEqual(parachute.state, self.sc.ParachuteState.stowed)

        # Launch; the parachutes are never armed or deployed, so there is nothing to slow
        # the descent. As soon as the vessel starts falling, hold anti-radial so it impacts
        # parachute-first and the parachute part is destroyed on the crash. Nothing reloads
        # a save, so the cached proxies remain registered in the object store while their
        # parts are gone.
        self.vessel.control.activate_next_stage()
        while flight.vertical_speed > 0:
            self.wait()
        self.vessel.control.sas = True
        self.wait()
        self.vessel.control.sas_mode = self.sc.SASMode.anti_radial

        self.wait_until_destroyed(part)

        # Accessing the destroyed part / parachute raises PartDestroyedException, for a
        # property, a method, and the parachute module (the case reported in issue #885).
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = part.name
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = part.position(body_frame)
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = parachute.state
        with self.assertRaises(self.sc.PartDestroyedException):
            _ = parachute.deployed

    def wait_until_destroyed(self, part, timeout=120, confirmations=5):
        # Poll until the part has been destroyed and stays destroyed. During the violent
        # impact a part can briefly become unresolvable and then resolve again, so require
        # several consecutive PartDestroyedException results before concluding it is gone.
        # Fails (rather than hanging) if the vessel never crashes.
        streak = 0
        for _ in range(int(timeout / 0.1)):
            try:
                _ = part.name
                streak = 0
            except self.sc.PartDestroyedException:
                streak += 1
                if streak >= confirmations:
                    return
            self.wait()
        self.fail("vessel did not crash and stay destroyed within the timeout")


if __name__ == "__main__":
    import unittest

    unittest.main()
