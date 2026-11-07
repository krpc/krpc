import unittest
from unittest import mock

import krpc.error
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

    def failing_remove(self) -> mock._patch:
        """Removes the stream on the server, then fails."""
        remove = self.conn._internal.remove_stream

        def fail(stream_id: int) -> None:
            remove(stream_id)
            raise krpc.error.ConnectionError("remove failed")

        return mock.patch.object(self.conn._internal, "remove_stream", fail)

    def test_with_keeps_the_exception_of_the_block(self) -> None:
        with self.failing_remove():
            with self.assertRaisesRegex(ValueError, "block"):
                with self.conn.add_stream(self.conn.test_service.float_to_string, 1):
                    raise ValueError("block")

    def test_with_raises_a_failed_remove(self) -> None:
        with self.failing_remove():
            with self.assertRaisesRegex(krpc.error.ConnectionError, "remove failed"):
                with self.conn.add_stream(self.conn.test_service.float_to_string, 1):
                    pass


if __name__ == "__main__":
    unittest.main()
