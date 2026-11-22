import datetime
import json
import subprocess
import unittest

from daily_due import MAX_AGE, Run, decide, due, last_full_run

NOW = datetime.datetime(2026, 11, 23, 4, 37, tzinfo=datetime.timezone.utc)


def run(id: int, sha: str, days_ago: float) -> dict:
    created = NOW - datetime.timedelta(days=days_ago)
    return {
        "databaseId": id,
        "headSha": sha,
        "createdAt": created.strftime("%Y-%m-%dT%H:%M:%SZ"),
    }


class FakeApi:
    """The GitHub CLI, answering from a list of runs and the jobs of each."""

    def __init__(self, runs: list[dict], jobs: dict[int, list[tuple[str, str]]]):
        self.runs = runs
        self.jobs = jobs

    def __call__(self, *args, retry=True) -> str:
        if args[:2] == ("run", "list"):
            assert "--status" in args and args[args.index("--status") + 1] == "success"
            return json.dumps(self.runs)
        path = next(a for a in args if a.startswith("repos/"))
        run_id = int(path.split("/")[-2])
        return "\n".join(
            json.dumps({"name": name, "conclusion": conclusion})
            for name, conclusion in self.jobs[run_id]
        )


SKIPPED = [("due", "success"), ("ci", "skipped")]
FULL = [
    ("due", "success"),
    ("ci / linux-test-core", "success"),
    ("ci / all-checks-passed", "success"),
]


class LastFullRunTest(unittest.TestCase):
    def test_skips_runs_that_skipped_ci(self):
        api = FakeApi(
            [run(3, "c", 1), run(2, "b", 2), run(1, "a", 3)],
            {3: SKIPPED, 2: FULL, 1: FULL},
        )
        last = last_full_run("krpc/krpc", api)
        assert last is not None
        self.assertEqual((last.id, last.sha), (2, "b"))

    def test_none_without_a_full_run(self):
        api = FakeApi([run(2, "b", 1), run(1, "a", 2)], {2: SKIPPED, 1: SKIPPED})
        self.assertIsNone(last_full_run("krpc/krpc", api))

    def test_skips_runs_whose_gate_did_not_pass(self):
        gate_skipped = [
            ("due", "success"),
            ("ci / linux-test-core", "failure"),
            ("ci / all-checks-passed", "skipped"),
        ]
        gate_failed = [("due", "success"), ("ci / all-checks-passed", "failure")]
        api = FakeApi(
            [run(3, "c", 1), run(2, "b", 2), run(1, "a", 3)],
            {3: gate_skipped, 2: gate_failed, 1: FULL},
        )
        last = last_full_run("krpc/krpc", api)
        assert last is not None
        self.assertEqual(last.id, 1)

    def test_none_without_runs(self):
        self.assertIsNone(last_full_run("krpc/krpc", FakeApi([], {})))


class DecideTest(unittest.TestCase):
    def test_failed_lookup_runs(self):
        def failing(*args, **kwargs) -> str:
            raise subprocess.CalledProcessError(1, "gh", stderr="HTTP 502")

        self.assertTrue(decide("krpc/krpc", "a", NOW, failing))

    def test_recent_full_run_at_main_skips(self):
        api = FakeApi([run(1, "a", 1)], {1: FULL})
        self.assertFalse(decide("krpc/krpc", "a", NOW, api))


class DueTest(unittest.TestCase):
    def last(self, sha: str, age: datetime.timedelta) -> Run:
        return Run(1, sha, NOW - age)

    def test_unchanged_main_skips(self):
        self.assertFalse(due("a", NOW, self.last("a", datetime.timedelta(days=1))))

    def test_moved_main_runs(self):
        self.assertTrue(due("b", NOW, self.last("a", datetime.timedelta(hours=1))))

    def test_old_run_runs(self):
        self.assertTrue(due("a", NOW, self.last("a", MAX_AGE)))

    def test_no_full_run_runs(self):
        self.assertTrue(due("a", NOW, None))


if __name__ == "__main__":
    unittest.main()
