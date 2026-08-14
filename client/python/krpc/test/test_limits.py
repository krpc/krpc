import math
import struct
import unittest
import krpc.limits


class TestLimits(unittest.TestCase):
    """The constants are checked against values derived a different way, so a typo in one
    of them is caught rather than being asserted against itself."""

    @staticmethod
    def _narrow(value: float) -> float:
        """value as the 32-bit float it becomes on the wire, widened back to a double"""
        return struct.unpack("<f", struct.pack("<f", value))[0]

    def test_double_extremes(self) -> None:
        self.assertEqual(krpc.limits.DOUBLE_MAX, -krpc.limits.DOUBLE_LOWEST)
        # At the top of the range: finite itself, but nothing above it is
        self.assertTrue(math.isfinite(krpc.limits.DOUBLE_MAX))
        self.assertEqual(float("inf"), krpc.limits.DOUBLE_MAX * 2)

    def test_float_extremes(self) -> None:
        self.assertEqual(krpc.limits.FLOAT_MAX, -krpc.limits.FLOAT_LOWEST)
        # Exactly representable as a 32-bit float, unlike anything larger
        self.assertEqual(krpc.limits.FLOAT_MAX, self._narrow(krpc.limits.FLOAT_MAX))
        self.assertRaises(
            OverflowError, struct.pack, "<f", krpc.limits.FLOAT_MAX * 1.01
        )
        self.assertTrue(krpc.limits.FLOAT_MAX < krpc.limits.DOUBLE_MAX)

    def test_signed_integer_extremes(self) -> None:
        for maximum, minimum, bits in (
            (krpc.limits.SINT32_MAX, krpc.limits.SINT32_MIN, 32),
            (krpc.limits.SINT64_MAX, krpc.limits.SINT64_MIN, 64),
        ):
            # Two's complement: the range spans 2**bits values, one more below zero
            self.assertEqual(2**bits, maximum - minimum + 1)
            self.assertEqual(minimum, -maximum - 1)

    def test_unsigned_integer_extremes(self) -> None:
        self.assertEqual(2**32, krpc.limits.UINT32_MAX + 1)
        self.assertEqual(2**64, krpc.limits.UINT64_MAX + 1)
        # An unsigned type reaches one bit further up than the signed one of its width
        self.assertEqual(krpc.limits.UINT32_MAX, krpc.limits.SINT32_MAX * 2 + 1)
        self.assertEqual(krpc.limits.UINT64_MAX, krpc.limits.SINT64_MAX * 2 + 1)


if __name__ == "__main__":
    unittest.main()
