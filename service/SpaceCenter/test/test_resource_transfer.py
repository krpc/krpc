import unittest
import krpctest


class TestResourceTransfer(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("ResourceTransfer")
        cls.remove_other_vessels()
        cls.sc = cls.connect().space_center
        vessel = cls.sc.active_vessel
        cls.parts = vessel.parts
        other_vessel = vessel.parts.decouplers[0].decouple()
        cls.other_parts = other_vessel.parts

    def test_transfer(self):
        from_part = self.parts.with_name("rcsTankRadialLong")[0]
        to_part = self.parts.with_name("radialRCSTank")[0]
        from_part_amount = from_part.resources.amount("MonoPropellant")
        to_part_amount = to_part.resources.amount("MonoPropellant")
        transfer = self.sc.ResourceTransfer.start(
            from_part, to_part, "MonoPropellant", 10
        )
        while not transfer.complete:
            self.wait()
        self.assertAlmostEqual(10, transfer.amount)
        self.assertAlmostEqual(
            from_part_amount - 10,
            from_part.resources.amount("MonoPropellant"),
            places=3,
        )
        self.assertAlmostEqual(
            to_part_amount + 10, to_part.resources.amount("MonoPropellant"), places=3
        )

    def test_transfer_all(self):
        from_part = self.parts.with_name("xenonTankRadial")[0]
        to_part = self.parts.with_name("xenonTankLarge")[0]
        transfer = self.sc.ResourceTransfer.start(
            from_part, to_part, "XenonGas", float("inf")
        )
        while not transfer.complete:
            self.wait()
        self.assertAlmostEqual(200, transfer.amount)
        self.assertAlmostEqual(200, from_part.resources.amount("XenonGas"))
        self.assertAlmostEqual(5250, to_part.resources.amount("XenonGas"))

    def test_transfer_with_limited_source(self):
        from_part = self.parts.with_name("fuelTank")[0]
        to_part = self.parts.with_name("fuelTankSmallFlat")[0]
        to_part_amount = to_part.resources.amount("LiquidFuel")
        transfer = self.sc.ResourceTransfer.start(from_part, to_part, "LiquidFuel", 10)
        while not transfer.complete:
            self.wait()
        self.assertAlmostEqual(5, transfer.amount)
        self.assertAlmostEqual(0, from_part.resources.amount("LiquidFuel"))
        self.assertAlmostEqual(
            to_part_amount + 5, to_part.resources.amount("LiquidFuel")
        )

    def test_transfer_with_limited_destination(self):
        from_part = self.parts.with_name("fuelTank")[0]
        to_part = self.parts.with_name("fuelTankSmallFlat")[0]
        from_part_amount = from_part.resources.amount("Oxidizer")
        transfer = self.sc.ResourceTransfer.start(from_part, to_part, "Oxidizer", 40)
        while not transfer.complete:
            self.wait()
        self.assertAlmostEqual(25, transfer.amount, places=3)
        self.assertAlmostEqual(
            from_part_amount - 25, from_part.resources.amount("Oxidizer")
        )
        self.assertAlmostEqual(55, to_part.resources.amount("Oxidizer"))

    def test_transfer_between_different_vessels(self):
        from_part = self.parts.all[0]
        to_part = self.other_parts.all[0]
        with self.assertRaises(ValueError) as cm:
            self.sc.ResourceTransfer.start(from_part, to_part, "Oxidizer", 100)
        self.assertTrue("Parts are not on the same vessel" in str(cm.exception))

    def test_transfer_between_same_parts(self):
        part = self.parts.with_name("fuelTank")[0]
        with self.assertRaises(ValueError) as cm:
            self.sc.ResourceTransfer.start(part, part, "Oxidizer", 100)
        self.assertTrue(
            "Source and destination parts are the same" in str(cm.exception)
        )

    def test_transfer_unknown_resource(self):
        from_part = self.parts.with_name("fuelTank")[0]
        to_part = self.parts.with_name("fuelTankSmallFlat")[0]
        with self.assertRaises(ValueError) as cm:
            self.sc.ResourceTransfer.start(from_part, to_part, "DoesntExist", 100)
        self.assertTrue("Resource 'DoesntExist' does not exist" in str(cm.exception))

    def test_transfer_from_part_without_resource(self):
        from_part = self.parts.with_name("radialRCSTank")[0]
        to_part = self.parts.with_name("fuelTankSmallFlat")[0]
        with self.assertRaises(ValueError) as cm:
            self.sc.ResourceTransfer.start(from_part, to_part, "Oxidizer", 100)
        self.assertTrue("Source part does not contain" in str(cm.exception))

    def test_transfer_to_part_without_resource(self):
        from_part = self.parts.with_name("fuelTankSmallFlat")[0]
        to_part = self.parts.with_name("radialRCSTank")[0]
        with self.assertRaises(ValueError) as cm:
            self.sc.ResourceTransfer.start(from_part, to_part, "Oxidizer", 100)
        self.assertTrue("Destination part cannot store" in str(cm.exception))


class TestResourceTransferDisconnect(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("ResourceTransfer")
        cls.remove_other_vessels()
        cls.parts = cls.connect().space_center.active_vessel.parts

    def test_transfer_cancelled_on_disconnect(self):
        from_part = self.parts.with_name("fuelTank")[0]
        to_part = self.parts.with_name("fuelTankSmallFlat")[0]
        from_amount = from_part.resources.amount("Oxidizer")

        # A second client starts a transfer that would take several seconds to
        # complete, then disconnects mid-transfer
        conn = self.connect(use_cached=False)
        sc = conn.space_center
        other_parts = sc.active_vessel.parts
        sc.ResourceTransfer.start(
            other_parts.with_name("fuelTank")[0],
            other_parts.with_name("fuelTankSmallFlat")[0],
            "Oxidizer",
            float("inf"),
        )
        self.wait(0.5)
        conn.close()
        self.wait(0.5)

        # Some, but not all, of the resource was moved before the disconnect
        # cancelled the transfer (the destination has free space, so the
        # transfer would still be running had it not been cancelled)...
        moved = from_amount - from_part.resources.amount("Oxidizer")
        self.assertGreater(moved, 0.1)
        self.assertLess(
            to_part.resources.amount("Oxidizer"), to_part.resources.max("Oxidizer")
        )
        # ...and no more is moved afterwards
        remaining = from_part.resources.amount("Oxidizer")
        self.wait(1)
        self.assertAlmostEqual(
            remaining, from_part.resources.amount("Oxidizer"), places=2
        )


if __name__ == "__main__":
    unittest.main()
