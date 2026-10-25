import math

import ksatest

G0 = 9.80665


class TestSequence(ksatest.TestCase):
    @classmethod
    def setUpClass(cls):
        super().setUpClass()
        cls.load_save()
        cls.vehicle = cls.ksa.active_vehicle
        cls.sequences = cls.vehicle.sequences

    def active_sequence(self):
        number = self.vehicle.control.active_sequence
        return next(s for s in self.vehicle.sequences if s.number == number)

    def test_sequences(self):
        numbers = [sequence.number for sequence in self.sequences]
        self.assertGreater(len(numbers), 1)
        self.assertEqual(sorted(numbers), numbers)
        self.assertIn(self.vehicle.control.active_sequence, numbers)
        self.assertEqual(self.sequences[0], self.vehicle.sequences[0])

    def test_activated(self):
        active = self.vehicle.control.active_sequence
        for sequence in self.sequences:
            self.assertEqual(sequence.number <= active, sequence.activated)

    def test_active_sequence(self):
        sequence = self.active_sequence()
        self.assertAlmostEqual(self.vehicle.mass, sequence.start_mass, delta=1)
        self.assertAlmostEqual(
            self.vehicle.available_thrust, sequence.thrust, delta=sequence.thrust * 0.01
        )

    def test_performance(self):
        burning = [s for s in self.sequences if s.delta_v > 0]
        self.assertTrue(burning)
        for sequence in burning:
            self.assertGreater(sequence.start_mass, sequence.end_mass)
            self.assertAlmostEqual(
                sequence.propellant_mass,
                sequence.start_mass - sequence.end_mass,
                delta=1,
            )
            # The rocket equation, and the mass flow rate at full thrust
            exhaust_velocity = sequence.specific_impulse * G0
            self.assertAlmostEqual(
                exhaust_velocity * math.log(sequence.start_mass / sequence.end_mass),
                sequence.delta_v,
                delta=sequence.delta_v * 0.02,
            )
            self.assertAlmostEqual(
                sequence.propellant_mass * exhaust_velocity / sequence.thrust,
                sequence.burn_time,
                delta=sequence.burn_time * 0.02,
            )

    def test_total_delta_v(self):
        self.assertAlmostEqual(
            sum(s.delta_v for s in self.sequences), self.vehicle.delta_v, delta=1
        )

    def test_burn_updates_performance(self):
        sequence = self.active_sequence()
        delta_v = sequence.delta_v
        control = self.vehicle.control
        throttle = control.throttle
        try:
            control.throttle = 1
            control.engine_on = True
            ksatest.wait_until(lambda: self.vehicle.thrust > 0, timeout=5)
            ksatest.wait_until(lambda: sequence.delta_v < delta_v - 10, timeout=10)
        finally:
            control.engine_on = False
            control.throttle = throttle
        self.assertAlmostEqual(self.vehicle.mass, sequence.start_mass, delta=10)
