import unittest
import krpctest


class EngineTestBase:

    # Keyed by language-independent internal part name (part.name); the inline
    # comment is the English title (part.title) for readability.
    engine_data = {
        "liquidEngine": {  # LV-T30 "Reliant" Liquid Fuel Engine
            "propellants": {"LiquidFuel": 9.0 / 11.0, "Oxidizer": 1.0},
            "gimballed": False,
            "throttle_locked": False,
            "can_restart": True,
            "can_shutdown": True,
            "max_vac_thrust": 240000,
            "msl_isp": 265,
            "vac_isp": 310,
            "modes": None,
        },
        "liquidEngine2": {  # LV-T45 "Swivel" Liquid Fuel Engine
            "propellants": {"LiquidFuel": 9.0 / 11.0, "Oxidizer": 1.0},
            "gimballed": True,
            "gimbal_range": 3,
            "throttle_locked": False,
            "can_restart": True,
            "can_shutdown": True,
            "max_vac_thrust": 215000,
            "msl_isp": 250,
            "vac_isp": 320,
            "modes": None,
        },
        "nuclearEngine": {  # LV-N "Nerv" Atomic Rocket Motor
            "propellants": {"LiquidFuel": 1.0},
            "gimballed": False,
            "throttle_locked": False,
            "can_restart": True,
            "can_shutdown": True,
            "max_vac_thrust": 60000,
            "msl_isp": 185,
            "vac_isp": 800,
            "modes": None,
        },
        "ionEngine": {  # IX-6315 "Dawn" Electric Propulsion System
            "propellants": {"XenonGas": 0.1 / 1.8, "ElectricCharge": 1.0},
            "gimballed": False,
            "throttle_locked": False,
            "can_restart": True,
            "can_shutdown": True,
            "max_vac_thrust": 2000,
            "msl_isp": 100,
            "vac_isp": 4200,
            "modes": None,
        },
        "omsEngine": {  # O-10 "Puff" MonoPropellant Fuel Engine
            "propellants": {"MonoPropellant": 1.0},
            "gimballed": True,
            "gimbal_range": 6,
            "throttle_locked": False,
            "can_restart": True,
            "can_shutdown": True,
            "max_vac_thrust": 20000,
            "msl_isp": 120,
            "vac_isp": 250,
            "modes": None,
        },
        "solidBooster.v2": {  # RT-10 "Hammer" Solid Fuel Booster
            "propellants": {"SolidFuel": 1.0},
            "gimballed": False,
            "throttle_locked": True,
            "can_restart": False,
            "can_shutdown": False,
            "max_vac_thrust": 227000,
            "msl_isp": 170,
            "vac_isp": 195,
            "modes": None,
        },
        "JetEngine": {  # J-33 "Wheesley" Turbofan Engine
            "propellants": {"IntakeAir": 1.0, "LiquidFuel": 1.0 / 127},
            "gimballed": False,
            "throttle_locked": False,
            "can_restart": True,
            "can_shutdown": True,
            "max_vac_thrust": 120000,
            "msl_isp": 10500,
            "vac_isp": 10500,
            "modes": None,
        },
        "RAPIER": {  # CR-7 R.A.P.I.E.R. Engine
            "propellants": {"IntakeAir": 1.0, "LiquidFuel": 0.166666},
            "gimballed": True,
            "gimbal_range": 3,
            "throttle_locked": False,
            "can_restart": True,
            "can_shutdown": True,
            "max_vac_thrust": 105000,
            "msl_isp": 3200,
            "vac_isp": 3200,
            "modes": ["AirBreathing", "ClosedCycle"],
        },
    }

    @classmethod
    def add_engine_data(cls, name, data):
        for k, v in data.items():
            cls.engine_data[name][k] = v

    def get_engine(self, name):
        return next(iter(self.parts.with_name(name))).engine

    def set_idle(self, engine):
        data = self.engine_data[engine.part.name]
        self.vessel.control.throttle = 0
        engine.active = False
        self.wait(1)
        if not data["throttle_locked"]:
            self.assertAlmostEqual(0, engine.throttle)

    def set_throttle(self, engine, value):
        data = self.engine_data[engine.part.name]
        self.vessel.control.throttle = value
        engine.active = True
        self.wait(1)
        if not data["throttle_locked"]:
            self.assertAlmostEqual(value, engine.throttle)


class EngineTest(EngineTestBase):

    def thrust_delta(self, data):
        """Absolute tolerance, in Newtons, for thrust comparisons.

        Air-breathing engines (those that consume IntakeAir) compute their
        thrust from the local atmospheric density and the vehicle's Mach
        number, so their sea-level thrust drifts as KSP tweaks its atmosphere
        model. Allow a wider tolerance for them than for rocket engines, whose
        thrust is deterministic.
        """
        if "IntakeAir" in data["propellants"]:
            return 0.03 * data["max_thrust"]
        return 500

    def check_engine_properties(self, engine):
        """Check engine properties independent of activity/throttle"""
        data = self.engine_data[engine.part.name]
        self.assertAlmostEqual(data["max_vac_thrust"], engine.max_vacuum_thrust)
        self.assertCountEqual(data["propellants"].keys(), engine.propellant_names)
        self.assertAlmostEqual(data["propellants"], engine.propellant_ratios, places=3)
        self.assertTrue(engine.has_fuel)
        self.assertEqual(data["throttle_locked"], engine.throttle_locked)
        self.assertAlmostEqual(
            data["msl_isp"], engine.kerbin_sea_level_specific_impulse
        )
        self.assertAlmostEqual(data["vac_isp"], engine.vacuum_specific_impulse)
        self.assertEqual(data["can_restart"], engine.can_restart)
        self.assertEqual(data["can_shutdown"], engine.can_shutdown)
        self.assertEqual(data["modes"] is not None, engine.has_modes)
        if data["modes"] is not None:
            self.assertCountEqual(data["modes"], engine.modes.keys())
            self.assertTrue(engine.mode in data["modes"])
        self.assertEqual(data["gimballed"], engine.gimballed)
        if not data["gimballed"]:
            self.assertAlmostEqual(0, engine.gimbal_range)
        else:
            self.assertFalse(engine.gimbal_locked)
            self.assertAlmostEqual(data["gimbal_range"], engine.gimbal_range)

    def check_engine_idle(self, engine):
        """Check engine properties when engine is deactivated"""
        data = self.engine_data[engine.part.name]
        self.assertFalse(engine.active)
        self.assertAlmostEqual(1, engine.thrust_limit)
        self.assertEqual(0, engine.thrust)
        delta = self.thrust_delta(data)
        self.assertAlmostEqual(data["max_thrust"], engine.available_thrust, delta=delta)
        self.assertAlmostEqual(data["max_thrust"], engine.max_thrust, delta=delta)
        self.assertAlmostEqual(data["isp"], engine.specific_impulse, delta=5)
        self.assertTrue(engine.has_fuel)
        self.assertAlmostEqual((0, 0, 0), engine.available_torque[0])
        self.assertAlmostEqual((0, 0, 0), engine.available_torque[1])

    def check_engine_active(self, engine, throttle):
        """Check engine properties when engine is activated"""
        data = self.engine_data[engine.part.name]
        self.assertTrue(engine.active)
        self.assertAlmostEqual(throttle, engine.throttle, places=2)
        self.assertAlmostEqual(1, engine.thrust_limit, delta=1)
        delta = self.thrust_delta(data)
        self.assertAlmostEqual(
            data["max_thrust"] * throttle, engine.thrust, delta=delta
        )
        self.assertAlmostEqual(data["max_thrust"], engine.available_thrust, delta=delta)
        self.assertAlmostEqual(data["max_thrust"], engine.max_thrust, delta=delta)
        self.assertAlmostEqual(data["isp"], engine.specific_impulse, delta=5)
        self.assertTrue(engine.has_fuel)
        if data["gimballed"] and throttle > 0:
            self.assertGreater(engine.available_torque[0], (100, 100, 100))
            self.assertLess(engine.available_torque[1], (-100, -100, -100))
        else:
            self.assertAlmostEqual((0, 0, 0), engine.available_torque[0])
            self.assertAlmostEqual((0, 0, 0), engine.available_torque[1])

    def check_engine(self, engine):
        self.set_idle(engine)
        self.check_engine_properties(engine)
        self.check_engine_idle(engine)
        for throttle in (1, 0.6, 0.2):
            self.set_throttle(engine, throttle)
            self.check_engine_properties(engine)
            self.check_engine_active(engine, throttle)
        self.set_idle(engine)
        self.check_engine_properties(engine)
        engine.active = False

    def test_lfo_engine(self):
        engine = self.get_engine("liquidEngine")  # LV-T30 "Reliant"
        self.check_engine(engine)

    def test_gimballed_lfo_engine(self):
        engine = self.get_engine("liquidEngine2")  # LV-T45 "Swivel"
        self.check_engine(engine)

    def test_nuclear_engine(self):
        engine = self.get_engine("nuclearEngine")  # LV-N "Nerv"
        self.check_engine(engine)

    def test_ion_engine(self):
        engine = self.get_engine("ionEngine")  # IX-6315 "Dawn"
        self.check_engine(engine)

    def test_rcs_engine(self):
        engine = self.get_engine("omsEngine")  # O-10 "Puff"
        self.check_engine(engine)

    def test_srb_engine(self):
        engine = self.get_engine("solidBooster.v2")  # RT-10 "Hammer"
        self.set_idle(engine)
        self.check_engine_properties(engine)
        self.check_engine_idle(engine)
        self.set_throttle(engine, 1)
        self.check_engine_properties(engine)
        self.check_engine_active(engine, 1)
        engine.active = False
        self.assertTrue(engine.active)
        self.check_engine_properties(engine)
        self.check_engine_active(engine, 1)
        self.set_idle(engine)


class TestPartsEngine(krpctest.TestCase, EngineTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsEngine")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def test_has_fuel(self):
        engine = self.get_engine("liquidEngine")  # LV-T30 "Reliant"
        self.assertTrue(engine.has_fuel)

    def test_has_no_fuel(self):
        engine = self.get_engine("liquidEngine3.v2")  # LV-909 "Terrier"
        # FIXME: have to activate engine for this to work
        engine.active = True
        self.wait()
        self.assertFalse(engine.has_fuel)
        engine.active = False

    def test_thrust_limit(self):
        engine = self.get_engine("liquidEngine")  # LV-T30 "Reliant"
        thrust = 205600

        engine.active = False
        engine.thrust_limit = 1
        self.vessel.control.throttle = 1
        self.assertTrue(engine.has_fuel)
        self.assertEqual(1, engine.thrust_limit)
        self.assertAlmostEqual(0, engine.thrust, 500)
        self.assertAlmostEqual(thrust, engine.available_thrust, delta=500)
        self.assertAlmostEqual(thrust, engine.max_thrust, delta=500)
        engine.active = True

        for throttle in (1, 0.666, 0.2, 0):
            self.vessel.control.throttle = throttle
            for thrust_limit in (1, 0.8, 0.333, 0.1, 0):
                engine.thrust_limit = thrust_limit
                self.assertAlmostEqual(thrust_limit, engine.thrust_limit)
                self.wait(0.5)
                self.assertAlmostEqual(
                    throttle * thrust_limit * thrust, engine.thrust, delta=500
                )
                self.assertAlmostEqual(
                    thrust_limit * thrust, engine.available_thrust, delta=500
                )
                self.assertAlmostEqual(thrust, engine.max_thrust, delta=500)

        engine.active = False
        engine.thrust_limit = 1
        self.assertAlmostEqual(1, engine.thrust_limit)

    def test_set_mode(self):
        engine = self.get_engine("RAPIER")  # CR-7 R.A.P.I.E.R. Engine
        engine.mode = "ClosedCycle"
        self.wait()
        self.assertEqual("ClosedCycle", engine.mode)
        engine.mode = "AirBreathing"

    def test_auto_mode_switch(self):
        engine = self.get_engine("RAPIER")  # CR-7 R.A.P.I.E.R. Engine
        engine.auto_mode_switch = True
        self.wait()
        self.assertTrue(engine.auto_mode_switch)
        engine.auto_mode_switch = False
        self.wait()
        self.assertFalse(engine.auto_mode_switch)

    def test_gimbal_lock(self):
        engine = self.get_engine("liquidEngine2")  # LV-T45 "Swivel"
        self.assertTrue(engine.gimballed)
        self.assertEqual(3, engine.gimbal_range)
        self.assertFalse(engine.gimbal_locked)
        engine.gimbal_locked = True
        self.wait()
        self.assertTrue(engine.gimbal_locked)
        engine.gimbal_locked = False
        self.wait()
        self.assertFalse(engine.gimbal_locked)

    def test_gimbal_limit(self):
        engine = self.get_engine("liquidEngine2")  # LV-T45 "Swivel"
        self.assertTrue(engine.gimballed)
        self.assertFalse(engine.gimbal_locked)
        self.assertEqual(1, engine.gimbal_limit)
        for limit in (1, 0.6, 0.234, 0):
            engine.gimbal_limit = limit
            self.assertAlmostEqual(limit, engine.gimbal_limit)
            self.wait(1)
        engine.gimbal_limit = 1
        engine.gimbal_locked = True
        self.assertEqual(0, engine.gimbal_limit)
        engine.gimbal_locked = False
        self.assertEqual(1, engine.gimbal_limit)

    def test_no_thrust_reverser(self):
        engine = self.get_engine("liquidEngine")  # LV-T30 "Reliant"
        self.assertFalse(engine.can_reverse_thrust)
        self.assertRaises(RuntimeError, getattr, engine, "thrust_reversed")
        self.assertRaises(RuntimeError, setattr, engine, "thrust_reversed", True)
        self.assertRaises(RuntimeError, engine.toggle_thrust_reversal)


class TestPartsEngineMSL(krpctest.TestCase, EngineTest):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsEngine")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.add_engine_data("liquidEngine", {"max_thrust": 205161, "isp": 265})
        cls.add_engine_data("liquidEngine2", {"max_thrust": 168430, "isp": 250})
        cls.add_engine_data("nuclearEngine", {"max_thrust": 14300, "isp": 190.6})
        cls.add_engine_data("ionEngine", {"max_thrust": 63, "isp": 128.0})
        cls.add_engine_data("omsEngine", {"max_thrust": 9700, "isp": 121.2})
        cls.add_engine_data("solidBooster.v2", {"max_thrust": 197897, "isp": 170.4})
        cls.add_engine_data("JetEngine", {"max_thrust": 110000, "isp": 10500})
        cls.add_engine_data("RAPIER", {"max_thrust": 96895, "isp": 3200})

    def test_jet_engine(self):
        engine = self.get_engine("JetEngine")  # J-33 "Wheesley"
        self.set_idle(engine)
        self.check_engine_properties(engine)
        self.check_engine_idle(engine)
        engine.active = True
        for throttle in (1, 0.6, 0.2):
            self.vessel.control.throttle = throttle
            while abs(engine.throttle - throttle) > 0.02:
                self.wait(1)
            self.check_engine_properties(engine)
            self.check_engine_active(engine, engine.throttle)
        engine.active = False

    def test_multi_mode_engine(self):
        engine = self.get_engine("RAPIER")  # CR-7 R.A.P.I.E.R. Engine
        engine.mode = "AirBreathing"
        self.set_idle(engine)
        self.check_engine_properties(engine)
        self.check_engine_idle(engine)
        engine.active = True
        for throttle in (1, 0.6, 0.2):
            self.vessel.control.throttle = throttle
            while abs(engine.throttle - throttle) > 0.1:
                self.wait(1)
            self.check_engine_properties(engine)
            self.check_engine_active(engine, engine.throttle)
        engine.active = False


class TestPartsEngineVacuum(krpctest.TestCase, EngineTest):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsEngine")
        cls.remove_other_vessels()
        cls.set_circular_orbit("Kerbin", 250000)
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts
        cls.add_engine_data("liquidEngine", {"max_thrust": 240000, "isp": 310})
        cls.add_engine_data("liquidEngine2", {"max_thrust": 215000, "isp": 320})
        cls.add_engine_data("nuclearEngine", {"max_thrust": 60000, "isp": 800})
        cls.add_engine_data("ionEngine", {"max_thrust": 2000, "isp": 4200})
        cls.add_engine_data("omsEngine", {"max_thrust": 20000, "isp": 250})
        cls.add_engine_data("solidBooster.v2", {"max_thrust": 227000, "isp": 195})
        cls.add_engine_data(
            "RAPIER",  # CR-7 R.A.P.I.E.R. Engine
            {
                "propellants": {"Oxidizer": 1.0, "LiquidFuel": 0.818181},
                "gimballed": True,
                "gimbal_range": 3,
                "throttle_locked": False,
                "can_restart": True,
                "can_shutdown": True,
                "max_vac_thrust": 180000,
                "msl_isp": 275,
                "vac_isp": 305,
                "modes": ["AirBreathing", "ClosedCycle"],
                "max_thrust": 180000,
                "isp": 305,
            },
        )

    def test_multi_mode_engine(self):
        engine = self.get_engine("RAPIER")  # CR-7 R.A.P.I.E.R. Engine
        engine.mode = "ClosedCycle"
        self.check_engine(engine)


class TestPartsEngineReverser(krpctest.TestCase, EngineTestBase):

    @classmethod
    def setUpClass(cls):
        cls.new_save()
        cls.launch_vessel_from_vab("PartsEngineReverser")
        cls.remove_other_vessels()
        cls.vessel = cls.connect().space_center.active_vessel
        cls.parts = cls.vessel.parts

    def check_thrust_reverser(self, name):
        engine = self.get_engine(name)
        self.assertTrue(engine.can_reverse_thrust)
        self.assertFalse(engine.thrust_reversed)

        # Engage the reverser
        engine.thrust_reversed = True
        self.wait(2)  # the reverser animation takes time
        self.assertTrue(engine.thrust_reversed)

        # Setting to the same state is a no-op
        engine.thrust_reversed = True
        self.assertTrue(engine.thrust_reversed)

        # Toggle back off
        engine.toggle_thrust_reversal()
        self.wait(2)
        self.assertFalse(engine.thrust_reversed)

    def test_goliath_thrust_reverser(self):
        self.check_thrust_reverser("turboFanSize2")  # J-90 "Goliath"

    def test_wheesley_thrust_reverser(self):
        self.check_thrust_reverser("JetEngine")  # J-33 "Wheesley"

    def test_thrust_reversers_aggregate(self):
        control = self.vessel.control
        self.assertFalse(control.thrust_reversers)
        control.thrust_reversers = True
        self.wait(2)
        self.assertTrue(control.thrust_reversers)
        control.thrust_reversers = False
        self.wait(2)
        self.assertFalse(control.thrust_reversers)


if __name__ == "__main__":
    unittest.main()
