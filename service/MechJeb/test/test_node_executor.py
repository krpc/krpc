import unittest

from service.MechJeb.test.mechjeb_test_case import (
    MechJebInOrbitTestCase,
    MechJebTestCase,
)


class TestNodeExecutor(MechJebTestCase):
    def test_settings(self):
        executor = self.mj.node_executor
        self.assert_settings(
            [
                (executor, "autowarp", False),
                (executor, "lead_time", 12),
            ]
        )

    def test_read_only(self):
        self.assertFalse(self.mj.node_executor.enabled)


class TestNodeExecutorInOrbit(MechJebInOrbitTestCase):
    def make_node(self, lead_time):
        operation = self.planner.operation_apoapsis
        operation.new_apoapsis = 120000
        operation.time_selector.time_reference = self.mj.TimeReference.x_from_now
        operation.time_selector.lead_time = lead_time
        operation.make_nodes()

    def test_execute_node(self):
        self.make_node(30)
        executor = self.mj.node_executor
        executor.execute_one_node()
        self.assertTrue(executor.enabled)
        self.wait_while(lambda: executor.enabled, timeout=300)
        self.assertEqual(0, len(self.vessel.control.nodes))
        self.assertAlmostEqual(120000, self.vessel.orbit.apoapsis_altitude, delta=2000)
        self.set_circular_orbit("Kerbin", 100000)

    def test_abort(self):
        self.make_node(600)
        executor = self.mj.node_executor
        executor.autowarp = False
        try:
            executor.execute_one_node()
            self.assertTrue(executor.enabled)
            executor.abort()
            self.assertFalse(executor.enabled)
        finally:
            executor.autowarp = True

    def test_without_node(self):
        executor = self.mj.node_executor
        for execute in (executor.execute_one_node, executor.execute_all_nodes):
            with self.subTest(method=execute.__name__):
                with self.assertRaisesRegex(RuntimeError, "no maneuver node"):
                    execute()
                self.assertFalse(executor.enabled)


if __name__ == "__main__":
    unittest.main()
