"""Numeric assertion helpers for the integration tests.

``AssertionsMixin`` extends the stock ``unittest`` assertions with tolerance-based and
domain-specific comparisons (angles in degrees/radians, quaternions, NaN, and list/dict
element-wise almost-equal). ``TestCase`` mixes it in, so tests reach these via ``self``.

The mixin is designed to be combined with ``unittest.TestCase`` (it calls ``self.fail`` /
``self.assertEqual``); it is not usable on its own.
"""

import math
import unittest


class AssertionsMixin(unittest.TestCase):  # pylint: disable=too-many-public-methods
    """Tolerance-based assertion helpers, mixed into ``TestCase``.

    Subclasses ``unittest.TestCase`` so ``assertAlmostEqual`` / ``assertNotAlmostEqual``
    override their stock counterparts (and so ``self.fail`` / ``self.assertEqual`` are
    available); it defines no tests of its own."""

    def assertAlmostEqual(self, first, second, places=7, msg=None, delta=None):
        """Check that first is equal to second, within the given error"""
        if not self._is_almost_equal(first, second, places, delta):
            if msg is None:
                msg = self._almost_equal_summary(first, second, "not almost equal")
                msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    def assertNotAlmostEqual(self, first, second, places=7, msg=None, delta=None):
        """Check that first is not equal to second,
        within the given error"""
        if self._is_almost_equal(first, second, places, delta):
            if msg is None:
                msg = self._almost_equal_summary(first, second, "almost equal")
                msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    def assertDegreesAlmostEqual(self, first, second, places=7, msg=None, delta=None):
        """Check that angle first is equal to angle second,
        within the given error.
        Uses clock arithmetic to compare angles, in range (0,360]"""

        def clamp_degrees(angle):
            angle = angle % 360
            if angle < 0:
                angle += 360
            return angle

        first_clamped = clamp_degrees(first)
        second_clamped = clamp_degrees(second)

        if msg is None:
            msg = self._almost_equal_error_msg(
                "Angle %f is not close to %f" % (first, second), places, delta
            )

        if delta is not None:
            min_degrees = clamp_degrees(first - delta)
            max_degrees = clamp_degrees(first + delta)
            if max_degrees >= second_clamped >= min_degrees:
                return
            if min_degrees > max_degrees >= second_clamped >= 0:
                return
            if max_degrees < min_degrees <= second_clamped <= 360:
                return
            self.fail(msg)

        else:
            self.assertAlmostEqual(
                first_clamped, second_clamped, msg=msg, places=places
            )

    def assertRadiansAlmostEqual(self, first, second, places=7, msg=None, delta=None):
        """Check that angle first (radians) is equal to angle second,
        within the given error. Compares the shortest angular distance,
        so it is robust to 2*pi wrapping and to values straddling 0/2*pi."""
        diff = (first - second) % (2 * math.pi)
        diff = min(diff, 2 * math.pi - diff)
        if msg is None:
            msg = self._almost_equal_error_msg(
                "Angle %f is not close to %f" % (first, second), places, delta
            )
        self.assertAlmostEqual(0, diff, places=places, msg=msg, delta=delta)

    def assertQuaternionsAlmostEqual(
        self, second, first, places=7, msg=None, delta=None
    ):
        """Check that a pair of quaternions represent the same orientation,
        within the given error."""
        for mult in [1, -1]:
            if self._is_almost_equal([x * mult for x in second], first, places, delta):
                return
        if msg is None:
            msg = self._almost_equal_summary(
                first, second, "not almost equivalent orientations"
            )
            msg = self._almost_equal_error_msg(msg, places, delta)
            self.fail(msg)

    @staticmethod
    def _is_value_almost_equal(first, second, places, delta=None):
        diff = abs(first - second)
        if delta is not None:
            return diff <= delta
        return round(diff, places) == 0

    def _is_almost_equal(self, first, second, places, delta=None):
        if isinstance(first, (list, tuple)):
            return self._list_is_almost_equal(first, second, places, delta)
        if isinstance(first, dict):
            return self._dict_is_almost_equal(first, second, places, delta)
        return self._is_value_almost_equal(first, second, places, delta)

    def _list_is_almost_equal(self, first, second, places, delta=None):
        if len(first) != len(second):
            return False
        for x, y in zip(first, second):
            if not self._is_almost_equal(x, y, places, delta):
                return False
        return True

    def _dict_is_almost_equal(self, first, second, places, delta=None):
        if set(first.keys()) != set(second.keys()):
            return False
        for k in first.keys():
            if not self._is_almost_equal(first[k], second[k], places, delta):
                return False
        return True

    @staticmethod
    def _almost_equal_summary(first, second, comparison):
        if isinstance(second, (list, tuple)):
            return "%s is %s to %s" % (str(first), comparison, str(second))
        if isinstance(second, dict):
            return "%s is %s to %s" % (str(first), comparison, str(second))
        return "%f is %s to %f" % (first, comparison, second)

    @staticmethod
    def _almost_equal_error_msg(msg, places, delta):
        if delta is not None:
            return "%s, within a delta of %f" % (msg, delta)
        return "%s, to %d places" % (msg, places)

    def assertIsNaN(self, value):
        """Check that the value is nan"""
        msg = "%s is not nan" % str(value)
        try:
            if not math.isnan(value):
                self.fail(msg)
        except TypeError:
            self.fail(msg)

    def assertIsNotNaN(self, value):
        """Check that the value is nan"""
        msg = "%s is nan" % str(value)
        try:
            if math.isnan(value):
                self.fail(msg)
        except TypeError:
            self.fail(msg)
