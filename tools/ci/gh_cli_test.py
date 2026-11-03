import json
import subprocess
import unittest
from unittest import mock

import gh_cli
from gh_cli import gh, gh_create, gh_lines


def _ok(stdout):
    return subprocess.CompletedProcess(["gh"], 0, stdout=stdout, stderr="")


def _failed():
    return subprocess.CalledProcessError(1, ["gh"], stderr="HTTP 502")


def _timed_out():
    return subprocess.TimeoutExpired(["gh"], gh_cli.CALL_TIMEOUT)


class TestGh(unittest.TestCase):
    def setUp(self):
        self.call = self._patch("gh_cli.subprocess.run")
        self.sleep = self._patch("gh_cli.time.sleep")
        self._patch("sys.stderr")

    def _patch(self, target):
        patcher = mock.patch(target)
        self.addCleanup(patcher.stop)
        return patcher.start()

    def test_success(self):
        self.call.return_value = _ok("out")
        self.assertEqual("out", gh("api", "user"))
        self.assertEqual(1, self.call.call_count)
        self.assertEqual(["gh", "api", "user"], self.call.call_args.args[0])
        self.assertEqual(gh_cli.CALL_TIMEOUT, self.call.call_args.kwargs["timeout"])
        self.sleep.assert_not_called()

    def test_retries_failures_and_timeouts(self):
        self.call.side_effect = [_failed(), _timed_out(), _ok("out")]
        self.assertEqual("out", gh("api", "user"))
        self.assertEqual(3, self.call.call_count)
        self.assertEqual(
            [mock.call(delay) for delay in gh_cli.RETRY_DELAYS],
            self.sleep.call_args_list,
        )

    def test_gives_up_after_the_last_retry(self):
        self.call.side_effect = _failed()
        with self.assertRaises(subprocess.CalledProcessError):
            gh("api", "user")
        self.assertEqual(len(gh_cli.RETRY_DELAYS) + 1, self.call.call_count)

    def test_gives_up_on_repeated_timeouts(self):
        self.call.side_effect = _timed_out()
        with self.assertRaises(subprocess.TimeoutExpired):
            gh("api", "user")
        self.assertEqual(len(gh_cli.RETRY_DELAYS) + 1, self.call.call_count)

    def test_no_retry(self):
        self.call.side_effect = _failed()
        with self.assertRaises(subprocess.CalledProcessError):
            gh("api", "user", retry=False)
        self.assertEqual(1, self.call.call_count)
        self.sleep.assert_not_called()


class TestHelpers(unittest.TestCase):
    def test_create_posts_once(self):
        api = mock.Mock(return_value="{}")
        gh_create("repos/o/r/issues/1/comments", {"body": "text"}, api=api)
        api.assert_called_once_with(
            "api",
            "repos/o/r/issues/1/comments",
            "--input",
            "-",
            stdin=json.dumps({"body": "text"}),
            retry=False,
        )

    def test_lines(self):
        api = mock.Mock(return_value='{"id": 1}\n\n{"id": 2}\n')
        self.assertEqual([{"id": 1}, {"id": 2}], gh_lines("api", "x", api=api))


if __name__ == "__main__":
    unittest.main()
