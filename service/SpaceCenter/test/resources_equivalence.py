"""Shared helper for asserting that two SpaceCenter Resources objects are equivalent.

This file is not named test_* so pytest does not collect it; test modules import it
by its fully-qualified name (the test directories are namespace packages, made
importable by the pythonpath setting in pytest.ini). Its self-test lives in
test_resources.py, which pytest does collect.
"""


def assert_resources_equivalent(testcase, expected, actual, delta=1):
    """Compare two SpaceCenter Resources objects by resource name, amount, and max."""
    expected_names = set(expected.names)
    actual_names = set(actual.names)
    testcase.assertEqual(expected_names, actual_names)
    for name in sorted(expected_names):
        testcase.assertAlmostEqual(
            expected.amount(name), actual.amount(name), delta=delta
        )
        testcase.assertAlmostEqual(expected.max(name), actual.max(name), delta=delta)
