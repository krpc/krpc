import shutil
import tempfile
import unittest
from unittest import mock

from krpctools.game import install, releases
from krpctools.game.ksp import KSP
from krpctools.game.ksp.krpc_version import KRPC_VERSION


def _release(tag, *assets, prerelease=False):
    return {
        "tag_name": tag,
        "prerelease": prerelease,
        "assets": [
            {
                "name": name,
                "browser_download_url": "https://example.com/%s/%s" % (tag, name),
                "digest": "sha256:" + tag,
            }
            for name in assets
        ],
    }


_RELEASES = [
    _release("ksa-v0.3.0", "krpc-ksa-0.3.0.zip"),
    _release("ksp-v1.1.0", "krpc-ksp-1.1.0.zip", prerelease=True),
    _release("ksp-v1.0.10", "krpc-ksp-1.0.10.zip"),
    _release("ksp-v1.0.9", "krpc-ksp-1.0.9.zip"),
    _release("python-v1.2.0", "krpc-python-1.2.0.zip"),
    _release("v0.7.0", "krpc-0.7.0.zip", "krpc-cpp-0.7.0.zip"),
]


class TestReleases(unittest.TestCase):
    def find(self, version, items=_RELEASES):
        ksp = KSP()
        return releases.find(items, ksp.release_prefixes, ksp.release_assets, version)

    def test_version_on_the_train(self):
        self.assertEqual(
            ("https://example.com/ksp-v1.0.9/krpc-ksp-1.0.9.zip", "ksp-v1.0.9"),
            self.find("1.0.9"),
        )

    def test_release_before_the_trains_split(self):
        self.assertEqual(
            ("https://example.com/v0.7.0/krpc-0.7.0.zip", "v0.7.0"), self.find("0.7.0")
        )

    def test_version_is_exact(self):
        with self.assertRaises(RuntimeError):
            self.find("0.7")

    def test_prerelease_is_skipped(self):
        with self.assertRaises(RuntimeError):
            self.find("1.1.0")

    def test_other_train_is_skipped(self):
        with self.assertRaises(RuntimeError):
            self.find("0.3.0")

    def test_release_without_archive_raises(self):
        with self.assertRaises(RuntimeError):
            self.find("1.0.0", [_release("ksp-v1.0.0", "krpc-cpp-1.0.0.zip")])

    def test_asset_name_ignores_case(self):
        url, _ = self.find("0.6.0", [_release("v0.6.0", "kRPC-0.6.0.zip")])
        self.assertEqual("https://example.com/v0.6.0/kRPC-0.6.0.zip", url)

    def test_missing_digest(self):
        release = _release("v0.6.0", "krpc-0.6.0.zip")
        del release["assets"][0]["digest"]
        self.assertIsNone(self.find("0.6.0", [release])[1])


class TestRelease(unittest.TestCase):
    def setUp(self):
        directory = tempfile.mkdtemp()
        self.addCleanup(shutil.rmtree, directory, ignore_errors=True)
        patcher = mock.patch.object(install, "cache_dir", return_value=directory)
        patcher.start()
        self.addCleanup(patcher.stop)

    def release(self):
        ksp = KSP()
        return releases.release(
            ksp.name, ksp.release_prefixes, ksp.release_assets, "1.0.9"
        )

    def test_lookup_is_cached(self):
        with mock.patch.object(
            releases, "github_releases", return_value=_RELEASES
        ) as listing:
            first = self.release()
            second = self.release()
        self.assertEqual(first, second)
        self.assertEqual(1, listing.call_count)

    def test_failed_lookup_is_not_cached(self):
        with mock.patch.object(releases, "github_releases", return_value=[]):
            with self.assertRaises(RuntimeError):
                self.release()
        with mock.patch.object(releases, "github_releases", return_value=_RELEASES):
            self.assertEqual("ksp-v1.0.9", self.release()[1])


class TestKSPRelease(unittest.TestCase):
    def test_default_is_the_built_release(self):
        self.assertRegex(KRPC_VERSION, r"^\d+\.\d+\.\d+$")
        self.assertEqual(KRPC_VERSION, KSP().krpc_release)

    def test_explicit_archive_skips_the_release(self):
        self.assertEqual(("krpc.zip", None), KSP(krpc="krpc.zip").krpc_source())
