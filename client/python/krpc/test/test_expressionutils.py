import itertools
import unittest

import krpc.schema.KRPC_pb2 as KRPC
from krpc.expressionutils import build_ptype, common_ptype, promote, with_nullable


def _tuple(code: int) -> KRPC.Type:
    return build_ptype(KRPC.Type.TUPLE, [build_ptype(code)])


class TestExpressionUtils(unittest.TestCase):
    def test_common_ptype_of_numbers(self) -> None:
        cases = [
            ([KRPC.Type.SINT32, KRPC.Type.DOUBLE], KRPC.Type.DOUBLE),
            ([KRPC.Type.SINT32, KRPC.Type.UINT32], KRPC.Type.SINT64),
            ([KRPC.Type.UINT32, KRPC.Type.UINT64], KRPC.Type.UINT64),
            ([KRPC.Type.SINT32, KRPC.Type.UINT64, KRPC.Type.FLOAT], KRPC.Type.FLOAT),
        ]
        for codes, expected in cases:
            for order in itertools.permutations(codes):
                ptype = common_ptype([build_ptype(code) for code in order])
                self.assertEqual(build_ptype(expected), ptype)

    def test_no_common_ptype_in_any_order(self) -> None:
        for codes in (
            [KRPC.Type.SINT32, KRPC.Type.UINT64],
            [KRPC.Type.UINT32, KRPC.Type.SINT64, KRPC.Type.UINT64],
        ):
            for order in itertools.permutations(codes):
                self.assertIsNone(common_ptype([build_ptype(code) for code in order]))
                self.assertIsNone(common_ptype([_tuple(code) for code in order]))

    def test_common_ptype_of_tuples_in_any_order(self) -> None:
        codes = [KRPC.Type.SINT32, KRPC.Type.UINT64, KRPC.Type.FLOAT]
        for order in itertools.permutations(codes):
            self.assertEqual(
                _tuple(KRPC.Type.FLOAT), common_ptype([_tuple(code) for code in order])
            )

    def test_common_ptype_is_nullable_where_any_is(self) -> None:
        nullable = with_nullable(build_ptype(KRPC.Type.SINT32), True)
        for order in itertools.permutations([nullable, build_ptype(KRPC.Type.DOUBLE)]):
            self.assertEqual(
                with_nullable(build_ptype(KRPC.Type.DOUBLE), True), common_ptype(order)
            )

    def test_common_ptype_of_unknown_or_unrelated_types(self) -> None:
        self.assertIsNone(common_ptype([build_ptype(KRPC.Type.SINT32), None]))
        self.assertIsNone(
            common_ptype([build_ptype(KRPC.Type.STRING), build_ptype(KRPC.Type.SINT32)])
        )

    def test_promote_takes_the_value_a_nullable_holds(self) -> None:
        for code in (KRPC.Type.SINT32, KRPC.Type.BOOL):
            plain = build_ptype(code)
            nullable = with_nullable(plain, True)
            for left, right in (
                (nullable, plain),
                (plain, nullable),
                (nullable, nullable),
            ):
                self.assertEqual(plain, promote(left, right))


if __name__ == "__main__":
    unittest.main()
