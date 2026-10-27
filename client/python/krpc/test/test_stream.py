import unittest

from krpc.test.servertestcase import ServerTestCase


class TestStream(ServerTestCase, unittest.TestCase):
    def test_raising_the_ending_again_does_not_grow_its_traceback(self) -> None:
        stream = self.conn.add_stream(
            self.conn.test_service.throw_invalid_operation_exception
        )
        depths = []
        for _ in range(3):
            try:
                stream()
            except RuntimeError as exn:
                depth = 0
                tb = exn.__traceback__
                while tb is not None:
                    depth += 1
                    tb = tb.tb_next
                depths.append(depth)
        self.assertEqual(depths[1], depths[2])


if __name__ == "__main__":
    unittest.main()
