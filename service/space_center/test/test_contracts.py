import unittest
import krpctest


class TestContracts(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        cls.new_save('krpctest_career', always_load=True)
        cls.space_center = cls.connect().space_center
        cls.cm = cls.space_center.contract_manager
        cls.ContractState = cls.space_center.ContractState

    def test_contract(self):
        contract = self.cm.active_contracts[0]
        self.assertEqual(
            'FinePrint.Contracts.ExplorationContract', contract.type)
        self.assertEqual('Orbit Kerbin!', contract.title)
        self.assertEqual(
            'One of the founding principles of Ionic Symphonic Protonic '
            'Electronics is to continue to advance the frontiers of science. '
            'The problem is that we don\'t actually have any idea how '
            'difficult it is to get a Kerbal to orbit, and we need to find '
            'out quickly. It\'s time for you to take another step in '
            'advancing the frontier.',
            contract.description)
        self.assertEqual('', contract.notes)
        self.assertEqual('We need to achieve a stable orbit around Kerbin. '
                         'All you should need to do is throw yourself at '
                         'the ground and miss.', contract.synopsis)
        self.assertCountEqual([], contract.keywords)
        self.assertEqual(self.ContractState.active, contract.state)
        self.assertTrue(contract.seen)
        self.assertTrue(contract.read)
        self.assertTrue(contract.active)
        self.assertFalse(contract.failed)
        self.assertFalse(contract.can_be_canceled)
        self.assertFalse(contract.can_be_declined)
        self.assertFalse(contract.can_be_failed)
        self.assertEqual(16800, contract.funds_advance)
        self.assertEqual(31200, contract.funds_completion)
        self.assertEqual(16800, contract.funds_failure)
        self.assertEqual(10, contract.reputation_completion)
        self.assertEqual(0, contract.reputation_failure)
        self.assertEqual(6, contract.science_completion)
        self.assertEqual(1, len(contract.parameters))
        parameter = contract.parameters[0]
        self.assertEqual('Achieve goal', parameter.title)
        self.assertEqual(
            'Fly a vessel up and out of the atmosphere and accelerate '
            'parallel with the surface until you are in a stable orbit to '
            'achieve this goal.',
            parameter.notes)
        self.assertCountEqual([], parameter.children)
        self.assertFalse(parameter.completed)
        self.assertFalse(parameter.failed)
        self.assertFalse(parameter.optional)
        self.assertEqual(48000, parameter.funds_completion)
        self.assertEqual(0, parameter.funds_failure)
        self.assertEqual(10, parameter.reputation_completion)
        self.assertEqual(0, parameter.reputation_failure)
        self.assertEqual(6, parameter.science_completion)

    def test_child_parameters(self):
        contract = self.cm.all_contracts[13]
        self.assertEqual('Test RT-10 "Hammer" Solid Fuel Booster '
                         'at the Launch Site.', contract.title)
        self.assertEqual(1, len(contract.parameters))
        parameter = contract.parameters[0]
        self.assertEqual(
            'Test RT-10 "Hammer" Solid Fuel Booster', parameter.title)
        self.assertEqual(2, len(parameter.children))
        self.assertEqual(['Kerbin', 'Launch Site'],
                         [x.title for x in parameter.children])

    def test_types(self):
        self.assertCountEqual(
            ['FinePrint.Contracts.ISRUContract',
             'FinePrint.Contracts.ExplorationContract',
             'Contracts.Templates.PlantFlag',
             'FinePrint.Contracts.BaseContract',
             'Contracts.Templates.RecoverAsset',
             'Contracts.Templates.GrandTour',
             'FinePrint.Contracts.ARMContract',
             'FinePrint.Contracts.SatelliteContract',
             'FinePrint.Contracts.StationContract',
             'Contracts.Templates.CollectScience',
             'FinePrint.Contracts.SurveyContract',
             'FinePrint.Contracts.TourismContract',
             'Contracts.Templates.PartTest',
             'SentinelMission.SentinelContract'],
            self.cm.types)

    def test_all_contracts(self):
        self.assertCountEqual(
            ['Conduct a focused observational survey of Kerbin.']*5 +
            ['Gather scientific data from Kerbin.',
             'Escape the atmosphere!',
             'Haul Mk16 Parachute into flight above Kerbin.',
             'Orbit Kerbin!',
             'Launch our first vessel!',
             'Test Mk16 Parachute in flight over Kerbin.',
             'Test RT-10 "Hammer" Solid Fuel Booster at the Launch Site.',
             'Test RT-10 "Hammer" Solid Fuel Booster at the Launch Site.',
             'Test Heat Shield (0.625m) in flight over Kerbin.'],
            [x.title for x in self.cm.all_contracts])

    def test_active_contracts(self):
        contracts = self.cm.active_contracts
        self.assertCountEqual(
            ['Orbit Kerbin!'],
            [x.title for x in contracts])
        contract = contracts[0]
        self.assertEqual(
            'FinePrint.Contracts.ExplorationContract', contract.type)
        self.assertEqual('Orbit Kerbin!', contract.title)
        self.assertEqual(self.ContractState.active, contract.state)
        self.assertTrue(contract.seen)
        self.assertTrue(contract.read)
        self.assertTrue(contract.active)
        self.assertFalse(contract.failed)
        self.assertFalse(contract.can_be_canceled)
        self.assertFalse(contract.can_be_declined)
        self.assertFalse(contract.can_be_failed)

    def test_offered_contracts(self):
        contracts = self.cm.offered_contracts
        self.assertCountEqual(
            ['Conduct a focused observational survey of Kerbin.']*5 +
            ['Gather scientific data from Kerbin.',
             'Escape the atmosphere!',
             'Haul Mk16 Parachute into flight above Kerbin.',
             'Test RT-10 "Hammer" Solid Fuel Booster at the Launch Site.',
             'Test Heat Shield (0.625m) in flight over Kerbin.'],
            [x.title for x in contracts])
        contract = contracts[0]
        self.assertEqual('FinePrint.Contracts.SurveyContract', contract.type)
        self.assertEqual('Conduct a focused observational survey of Kerbin.',
                         contract.title)
        self.assertEqual(self.ContractState.offered, contract.state)
        self.assertTrue(contract.seen)
        self.assertFalse(contract.read)
        self.assertFalse(contract.active)
        self.assertFalse(contract.failed)
        self.assertTrue(contract.can_be_canceled)
        self.assertTrue(contract.can_be_declined)
        self.assertTrue(contract.can_be_failed)

    def test_completed_contracts(self):
        contracts = self.cm.completed_contracts
        self.assertCountEqual(
            ['Launch our first vessel!'],
            [x.title for x in contracts])
        contract = contracts[0]
        self.assertEqual(
            'FinePrint.Contracts.ExplorationContract', contract.type)
        self.assertEqual('Launch our first vessel!', contract.title)
        self.assertEqual(self.ContractState.completed, contract.state)
        self.assertTrue(contract.seen)
        self.assertTrue(contract.read)
        self.assertFalse(contract.active)
        self.assertFalse(contract.failed)
        self.assertFalse(contract.can_be_canceled)
        self.assertFalse(contract.can_be_declined)
        self.assertFalse(contract.can_be_failed)

    def test_failed_contracts(self):
        # TODO: fail a contract to test this
        self.assertCountEqual(
            [],
            [x.title for x in self.cm.failed_contracts])


if __name__ == '__main__':
    unittest.main()
