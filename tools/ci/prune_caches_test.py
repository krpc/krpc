import json
import subprocess
import unittest

from prune_caches import prune


def http_error(status: str) -> subprocess.CalledProcessError:
    return subprocess.CalledProcessError(1, "gh", stderr=f"gh: error (HTTP {status})")


class FakeApi:
    """The GitHub CLI, answering from a list of caches, pull requests and branches."""

    def __init__(
        self,
        caches: list[tuple[int, str]],
        open_pulls: set[int],
        branches: set[str],
        failing: set[str] | None = None,
        gone: set[int] | None = None,
    ):
        self.caches = caches
        self.open_pulls = open_pulls
        self.branches = branches
        # Branches whose lookup fails with a server error, and caches already deleted
        self.failing = failing or set()
        self.gone = gone or set()
        self.deleted: list[int] = []
        self.lookups: list[str] = []

    def __call__(self, *args, retry=True) -> str:
        path = next(a for a in args if a.startswith("repos/"))
        if "DELETE" in args:
            id = int(path.rpartition("/")[2])
            if id in self.gone:
                raise http_error("404")
            self.deleted.append(id)
            return ""
        if "/actions/caches" in path:
            return "\n".join(
                json.dumps(
                    {"id": id, "ref": ref, "key": f"key-{id}", "size_in_bytes": 1}
                )
                for id, ref in self.caches
            )
        self.lookups.append(path)
        if "/pulls/" in path:
            number = int(path.rpartition("/")[2])
            return "open\n" if number in self.open_pulls else "closed\n"
        branch = path.partition("/branches/")[2]
        if branch in self.failing:
            raise http_error("502")
        if branch not in self.branches:
            raise http_error("404")
        return branch + "\n"


QUEUE = "gh-readonly-queue/main/pr-1410-e91e26659"


class PruneTest(unittest.TestCase):
    def test_deletes_closed_pull_requests(self):
        api = FakeApi(
            [(1, "refs/pull/1409/merge"), (2, "refs/pull/1411/merge")], {1411}, set()
        )
        prune("krpc/krpc", api)
        self.assertEqual(api.deleted, [1])

    def test_deletes_deleted_branches(self):
        api = FakeApi(
            [
                (1, f"refs/heads/{QUEUE}"),
                (2, "refs/heads/gh-readonly-queue/main/pr-1411-abc"),
                (3, "refs/heads/release-v0.8.1"),
                (4, "refs/heads/release-v0.9.0"),
            ],
            set(),
            {QUEUE, "release-v0.9.0"},
        )
        prune("krpc/krpc", api)
        self.assertEqual(api.deleted, [2, 3])

    def test_keeps_main_and_other_refs(self):
        api = FakeApi([(1, "refs/heads/main"), (2, "refs/tags/v0.8.0")], set(), set())
        prune("krpc/krpc", api)
        self.assertEqual(api.deleted, [])
        self.assertEqual(api.lookups, [])

    def test_looks_up_each_ref_once(self):
        api = FakeApi(
            [(1, "refs/pull/1409/merge"), (2, "refs/pull/1409/merge")], set(), set()
        )
        prune("krpc/krpc", api)
        self.assertEqual(api.deleted, [1, 2])
        self.assertEqual(len(api.lookups), 1)

    def test_failed_lookup_deletes_nothing_more(self):
        api = FakeApi(
            [
                (1, "refs/pull/1409/merge"),
                (2, "refs/heads/feature"),
                (3, "refs/pull/1408/merge"),
            ],
            set(),
            set(),
            failing={"feature"},
        )
        with self.assertRaises(subprocess.CalledProcessError):
            prune("krpc/krpc", api)
        self.assertEqual(api.deleted, [1])

    def test_failed_lookup_is_retried(self):
        api = FakeApi([(1, "refs/heads/feature")], set(), set(), failing={"feature"})
        with self.assertRaises(subprocess.CalledProcessError):
            prune("krpc/krpc", api)
        self.assertEqual(len(api.lookups), 2)

    def test_cache_already_deleted(self):
        api = FakeApi(
            [(1, "refs/pull/1409/merge"), (2, "refs/pull/1409/merge")],
            set(),
            set(),
            gone={1},
        )
        deleted = prune("krpc/krpc", api)
        self.assertEqual([c.id for c in deleted], [1, 2])
        self.assertEqual(api.deleted, [2])


if __name__ == "__main__":
    unittest.main()
