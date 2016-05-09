import unittest
import krpctest
import krpc
import time

class TestResourceTransfer(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('ResourceTransfer')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(name='TestResourceTransfer')
        cls.sc = cls.conn.space_center
        cls.vessel = cls.sc.active_vessel
        cls.vessel.parts.decouplers[0].decouple()
        time.sleep(0.1)
        cls.other_vessel = next(iter(filter(lambda v: v != cls.vessel, cls.sc.vessels)))

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_transfer(self):
        fromPart = next(iter(self.vessel.parts.with_title('Stratus-V Cylindrified Monopropellant Tank')))
        toPart = next(iter(self.vessel.parts.with_title('Stratus-V Roundified Monopropellant Tank')))
        fromPartAmount = fromPart.resources.amount('MonoPropellant')
        toPartAmount = toPart.resources.amount('MonoPropellant')
        transfer = self.sc.ResourceTransfer.start(fromPart, toPart, 'MonoPropellant', 10)
        while not transfer.complete:
            time.sleep(0.1)
        self.assertClose (transfer.amount, 10)
        self.assertClose (fromPartAmount - 10, fromPart.resources.amount('MonoPropellant'))
        self.assertClose (toPartAmount + 10, toPart.resources.amount('MonoPropellant'))

    def test_transfer_all(self):
        fromPart = next(iter(self.vessel.parts.with_title('PB-X50R Xenon Container')))
        toPart = next(iter(self.vessel.parts.with_title('PB-X750 Xenon Container')))
        transfer = self.sc.ResourceTransfer.start(fromPart, toPart, 'XenonGas', float('inf'))
        while not transfer.complete:
            time.sleep(0.1)
        self.assertClose (transfer.amount, 200)
        self.assertClose (200, fromPart.resources.amount('XenonGas'))
        self.assertClose (5250, toPart.resources.amount('XenonGas'))

    def test_transfer_with_limited_source (self):
        fromPart = next(iter(self.vessel.parts.with_title('FL-T400 Fuel Tank')))
        toPart = next(iter(self.vessel.parts.with_title('FL-T100 Fuel Tank')))
        toPartAmount = toPart.resources.amount('LiquidFuel')
        transfer = self.sc.ResourceTransfer.start(fromPart, toPart, 'LiquidFuel', 10)
        while not transfer.complete:
            time.sleep(0.1)
        self.assertClose (transfer.amount, 5)
        self.assertClose (0, fromPart.resources.amount('LiquidFuel'))
        self.assertClose (toPartAmount + 5, toPart.resources.amount('LiquidFuel'))

    def test_transfer_with_limited_destination (self):
        fromPart = next(iter(self.vessel.parts.with_title('FL-T400 Fuel Tank')))
        toPart = next(iter(self.vessel.parts.with_title('FL-T100 Fuel Tank')))
        fromPartAmount = fromPart.resources.amount('Oxidizer')
        transfer = self.sc.ResourceTransfer.start(fromPart, toPart, 'Oxidizer', 40)
        while not transfer.complete:
            time.sleep(0.1)
        self.assertClose (transfer.amount, 25)
        self.assertClose (fromPartAmount - 25, fromPart.resources.amount('Oxidizer'))
        self.assertClose (55, toPart.resources.amount('Oxidizer'))

    def test_transfer_between_different_vessels(self):
        fromPart = next(iter(self.vessel.parts.all))
        toPart = next(iter(self.other_vessel.parts.all))
        with self.assertRaises(krpc.error.RPCError) as cm:
            self.sc.ResourceTransfer.start(fromPart, toPart, 'Oxidizer', 100)
        self.assertTrue('Parts are not on the same vessel' in str(cm.exception))

    def test_transfer_between_same_parts(self):
        part = next(iter(self.vessel.parts.with_title('FL-T400 Fuel Tank')))
        with self.assertRaises(krpc.error.RPCError) as cm:
            self.sc.ResourceTransfer.start(part, part, 'Oxidizer', 100)
        self.assertTrue('Source and destination parts are the same' in str(cm.exception))

    def test_transfer_unknown_resource(self):
        fromPart = next(iter(self.vessel.parts.with_title('FL-T400 Fuel Tank')))
        toPart = next(iter(self.vessel.parts.with_title('FL-T100 Fuel Tank')))
        with self.assertRaises(krpc.error.RPCError) as cm:
            self.sc.ResourceTransfer.start(fromPart, toPart, 'DoesntExist', 100)
        self.assertTrue('Resource \'DoesntExist\' does not exist' in str(cm.exception))

    def test_transfer_from_part_without_resource(self):
        fromPart = next(iter(self.vessel.parts.with_title('Stratus-V Roundified Monopropellant Tank')))
        toPart = next(iter(self.vessel.parts.with_title('FL-T100 Fuel Tank')))
        with self.assertRaises(krpc.error.RPCError) as cm:
            self.sc.ResourceTransfer.start(fromPart, toPart, 'Oxidizer', 100)
        self.assertTrue('Source part does not contain' in str(cm.exception))

    def test_transfer_to_part_without_resource(self):
        fromPart = next(iter(self.vessel.parts.with_title('FL-T100 Fuel Tank')))
        toPart = next(iter(self.vessel.parts.with_title('Stratus-V Roundified Monopropellant Tank')))
        with self.assertRaises(krpc.error.RPCError) as cm:
            self.sc.ResourceTransfer.start(fromPart, toPart, 'Oxidizer', 100)
        self.assertTrue('Destination part cannot store' in str(cm.exception))

if __name__ == '__main__':
    unittest.main()
