import contextlib
import gzip
import hashlib
import io
import json
import shutil
import subprocess
import tempfile
import textwrap
import unittest
from pathlib import Path
from unittest import mock

import deps
from deps import Dep, Location, Sources, Verify

SHA_A = "a" * 64


class TestRender(unittest.TestCase):
    def test_version(self):
        self.assertEqual("lua-5.4.9", deps.render("lua-{version}", "5.4.9"))

    def test_separator(self):
        self.assertEqual("v1_9_0", deps.render("v{version:_}", "1.9.0"))
        self.assertEqual("asio-1-38-2", deps.render("asio-{version:-}", "1.38.2"))
        self.assertEqual("py310", deps.render("py{version:}", "3.10"))

    def test_parts(self):
        self.assertEqual(
            "23 1 3 23.1",
            deps.render("{major} {minor} {patch} {major_minor}", "23.1.3"),
        )

    def test_missing_part(self):
        with self.assertRaises(ValueError):
            deps.render("{patch}", "3.12")

    def test_other_braces(self):
        self.assertEqual(
            '{"version": "1.0"}', deps.render('{"version": "{version}"}', "1.0")
        )

    def test_anchor(self):
        self.assertEqual("protobuf==7.", deps.anchor("protobuf==7.{version}"))
        self.assertEqual("", deps.anchor("{version}"))


class TestPinSpans(unittest.TestCase):
    def test_whole_version_at_the_end(self):
        content = "protobuf==7.36.21\nprotobuf==7.36.2\n"
        self.assertEqual(
            [(18, 34)], deps.pin_spans(content, "protobuf==7.{version}", "36.2")
        )

    def test_whole_version_at_the_start(self):
        self.assertEqual([], deps.pin_spans("19.3.2\n", "{version}", "9.3.2"))
        self.assertEqual([], deps.pin_spans("1.9.3.2\n", "{version}", "9.3.2"))
        self.assertEqual([(0, 5)], deps.pin_spans("9.3.2\n", "{version}", "9.3.2"))

    def test_suffix(self):
        self.assertEqual([], deps.pin_spans("x==1.2rc1", "x=={version}", "1.2"))
        self.assertEqual([], deps.pin_spans("x==1.2.post1", "x=={version}", "1.2"))
        self.assertEqual([(0, 6)], deps.pin_spans("x==1.2,", "x=={version}", "1.2"))

    def test_inside_the_template(self):
        content = "lua-5.4.9.tar.gz"
        self.assertEqual(
            [(0, 16)], deps.pin_spans(content, "lua-{version}.tar.gz", "5.4.9")
        )


class TestCalls(unittest.TestCase):
    def test_skips_strings_and_comments(self):
        content = textwrap.dedent("""\
            # a comment (
            http_archive(
                build_file_content = \"\"\"
            cc_library(
                name = ')',
            \"\"\",
                url = "u(",
            )
            x = [1, f(2)]
            """)
        (call,) = deps.calls(content)
        self.assertTrue(content[call[0] :].startswith("http_archive("))
        self.assertTrue(content[: call[1]].endswith('"u(",\n)'))

    def test_cmake(self):
        content = "if(X)\n  FetchContent_Declare(asio\n    URL u\n)\nendif()\n"
        self.assertEqual(3, len(deps.calls(content)))


class TestTagVersion(unittest.TestCase):
    def test_default(self):
        self.assertEqual("1.18.0", deps.tag_version("v1.18.0", None))
        self.assertEqual("0.6.0", deps.tag_version("0.6.0", None))
        self.assertIsNone(deps.tag_version("v2.0.0-rc1", None))

    def test_pattern(self):
        self.assertEqual(
            "3.5", deps.tag_version("LUAUNIT_V3_5", r"LUAUNIT_V(\d+_\d+(?:_\d+)?)")
        )
        self.assertEqual(
            "1.38.2", deps.tag_version("asio-1-38-2", r"asio-(\d+-\d+-\d+)")
        )
        self.assertIsNone(deps.tag_version("other", r"asio-(\d+-\d+-\d+)"))


class FakeSources(Sources):
    def __init__(self, versions: dict[str, list[str]]):
        super().__init__(fetch=self.fail, gh=self.fail)
        self.listed = versions

    @staticmethod
    def fail(_):
        raise AssertionError("no network in tests")

    def lookup(self, source, tag):
        return self.listed[source]


class TestStatus(unittest.TestCase):
    def test_newest_release(self):
        dep = Dep("x", "1.2.0", source="s")
        st = deps.status(
            FakeSources({"s": ["1.1.0", "1.10.0", "1.2.0", "2.0.0-beta"]}), dep
        )
        self.assertEqual("1.10.0", st.target)
        self.assertTrue(st.outdated)

    def test_up_to_date(self):
        st = deps.status(FakeSources({"s": ["1.2.0"]}), Dep("x", "1.2.0", source="s"))
        self.assertFalse(st.outdated)
        self.assertEqual("ok", deps.describe(st))

    def test_below(self):
        dep = Dep("x", "22.1.0", source="s", below="23")
        st = deps.status(FakeSources({"s": ["22.2.0", "24.0.0"]}), dep)
        self.assertEqual("22.2.0", st.target)
        self.assertEqual("24.0.0", st.newest)
        self.assertIn("newest 24.0.0", deps.describe(st))

    def test_verify(self):
        dep = Dep("x", "1.0", source="s", verify=[Verify("t", "4.{version}")])
        sources = FakeSources({"s": ["1.0", "1.1", "1.2"], "t": ["4.1.0", "4.1.1"]})
        self.assertEqual("1.1", deps.status(sources, dep).target)

    def test_capped(self):
        st = deps.status(
            FakeSources({"s": ["1.0", "2.0"]}), Dep("x", "1.0", source="s", below="2")
        )
        self.assertFalse(st.outdated)
        self.assertTrue(st.capped)

    def test_no_source(self):
        st = deps.status(FakeSources({}), Dep("x", "1.0"))
        self.assertFalse(st.outdated)
        self.assertEqual("no source", deps.describe(st))

    def test_lookup_error(self):
        st = deps.status(FakeSources({}), Dep("x", "1.0", source="missing"))
        self.assertIsNotNone(st.error)


class TreeTest(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.root = Path(directory.name)

    def write(self, path: str, content: str) -> None:
        file = self.root / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(textwrap.dedent(content))

    def read(self, path: str) -> str:
        return (self.root / path).read_text()


class TestCheck(TreeTest):
    def test_agrees(self):
        self.write("a.txt", "foo==1.2\n")
        dep = Dep("foo", "1.2", locations=[Location("a.txt", "foo=={version}")])
        self.assertEqual([], deps.check(self.root, [dep]))

    def test_missing_text(self):
        self.write("a.txt", "bar\n")
        dep = Dep("foo", "1.2", locations=[Location("a.txt", "foo=={version}")])
        self.assertIn("does not contain", deps.check(self.root, [dep])[0])

    def test_missing_file(self):
        dep = Dep("foo", "1.2", locations=[Location("a.txt", "foo=={version}")])
        self.assertIn("not found", deps.check(self.root, [dep])[0])

    def test_drift(self):
        self.write("a.txt", "foo==1.2\n")
        self.write("b.txt", "foo==1.1\n")
        dep = Dep("foo", "1.2", locations=[Location("*.txt", "foo=={version}")])
        errors = deps.check(self.root, [dep])
        self.assertEqual(1, len(errors))
        self.assertIn("b.txt:1", errors[0])

    def test_longer_version(self):
        self.write("a.txt", "foo==1.21\n")
        dep = Dep("foo", "1.2", locations=[Location("a.txt", "foo=={version}")])
        errors = deps.check(self.root, [dep])
        self.assertEqual(1, len(errors))
        self.assertIn("a.txt:1", errors[0])

    def test_sha256_location_needs_a_hash(self):
        self.write("MODULE.bazel", 'http_file(\n    url = "https://x/1.0",\n)\n')
        dep = Dep(
            "foo",
            "1.0",
            locations=[Location("MODULE.bazel", "https://x/{version}", True)],
        )
        self.assertIn("one sha256", deps.check(self.root, [dep])[0])

    def test_not_strict(self):
        self.write("a.txt", "lua-5.4.9\nlua-protobuf-0.6.0\n")
        strict = Dep("lua", "5.4.9", locations=[Location("a.txt", "lua-{version}")])
        loose = Dep(
            "lua", "5.4.9", locations=[Location("a.txt", "lua-{version}", strict=False)]
        )
        self.assertEqual(1, len(deps.check(self.root, [strict])))
        self.assertEqual([], deps.check(self.root, [loose]))


MODULE = """\
http_archive(
    name = "one",
    sha256 = "{sha}",
    url = "https://example.com/one-1.0.tar.gz",
)

http_archive(
    name = "two",
    sha256 = "{sha}",
    url = "https://example.com/one-1.0.tar.gz",
)
"""


class TestUpdate(TreeTest):
    MANIFEST = """\
    [[dep]]
    name = "one"
    version = "1.0"
    at = ["MODULE.bazel: one-{version}"]

    [[dep]]
    name = "two"
    version = "1.0"
    """

    def setUp(self):
        super().setUp()
        self.write("tools/deps/deps.toml", self.MANIFEST)

    def test_rewrites_and_hashes(self):
        self.write("MODULE.bazel", MODULE.format(sha=SHA_A))
        dep = Dep(
            "one",
            "1.0",
            locations=[
                Location(
                    "MODULE.bazel", "https://example.com/one-{version}.tar.gz", True
                )
            ],
        )
        fetched = []

        def fetch(url):
            fetched.append(url)
            return b"archive"

        contents = deps.plan_update(self.root, [(dep, "1.1")], fetch)
        module = contents[self.root / "MODULE.bazel"]
        digest = hashlib.sha256(b"archive").hexdigest()
        self.assertEqual(MODULE.replace("1.0", "1.1").format(sha=digest), module)
        self.assertEqual(["https://example.com/one-1.1.tar.gz"], fetched)
        manifest = contents[self.root / "tools/deps/deps.toml"]
        self.assertIn('name = "one"\nversion = "1.1"', manifest)
        self.assertIn('name = "two"\nversion = "1.0"', manifest)
        # Nothing is written until the caller writes the plan
        self.assertIn("one-1.0", self.read("MODULE.bazel"))

    def test_skips_generated(self):
        self.write("MODULE.bazel", "one-1.0\n")
        self.write("lock.txt", "one-1.0\n")
        dep = Dep(
            "one",
            "1.0",
            locations=[
                Location("MODULE.bazel", "one-{version}"),
                Location("lock.txt", "one-{version}", generated=True),
            ],
        )
        contents = deps.plan_update(self.root, [(dep, "1.1")])
        self.assertEqual("one-1.1\n", contents[self.root / "MODULE.bazel"])
        self.assertNotIn(self.root / "lock.txt", contents)

    def test_missing_location(self):
        self.write("MODULE.bazel", "nothing\n")
        dep = Dep("one", "1.0", locations=[Location("MODULE.bazel", "one-{version}")])
        with self.assertRaises(ValueError):
            deps.plan_update(self.root, [(dep, "1.1")])

    def test_sha256_needs_one_hash(self):
        content = 'http_file(\n    url = "https://example.com/x",\n)\n'
        with self.assertRaises(ValueError):
            deps.replace_sha256(content, [content.index("https")], SHA_A)

    def test_sha256_before_build_file_content(self):
        content = textwrap.dedent(f"""\
            http_archive(
                name = "asio",
                sha256 = "{SHA_A}",
                build_file_content = \"\"\"
            cc_library(
                name = 'asio',
            )
            \"\"\",
                url = "https://example.com/asio.tar.gz",
            )
            """)
        new = deps.replace_sha256(content, [content.index("https")], "b" * 64)
        self.assertEqual(content.replace(SHA_A, "b" * 64), new)

    def test_cmake_sha256(self):
        content = f"FetchContent_Declare(asio\n  URL u\n  URL_HASH SHA256={SHA_A}\n)\n"
        new = deps.replace_sha256(content, [content.index("u\n")], "b" * 64)
        self.assertEqual(content.replace(SHA_A, "b" * 64), new)

    def test_downloads_once(self):
        self.write("MODULE.bazel", MODULE.format(sha=SHA_A))
        self.write(
            "CMakeLists.txt",
            f"F(\n  URL https://example.com/one-1.0.tar.gz\n  URL_HASH SHA256={SHA_A}\n)\n",
        )
        url = "https://example.com/one-{version}.tar.gz"
        dep = Dep(
            "one",
            "1.0",
            locations=[
                Location("MODULE.bazel", url, True),
                Location("CMakeLists.txt", url, True),
            ],
        )
        fetched = []

        def fetch(url):
            fetched.append(url)
            return b"archive"

        deps.plan_update(self.root, [(dep, "1.1")], fetch)
        self.assertEqual(["https://example.com/one-1.1.tar.gz"], fetched)

    def test_names_or_all(self):
        root = str(self.root)
        with contextlib.redirect_stderr(io.StringIO()):
            with self.assertRaises(SystemExit):
                deps.main(["--root", root, "update", "--all", "one"])
            with self.assertRaises(SystemExit):
                deps.main(["--root", root, "update"])

    def test_whole_versions_only(self):
        self.write("a.txt", "one==1.0\none==1.01\n")
        dep = Dep("one", "1.0", locations=[Location("a.txt", "one=={version}")])
        contents = deps.plan_update(self.root, [(dep, "1.1")])
        self.assertEqual("one==1.1\none==1.01\n", contents[self.root / "a.txt"])

    def test_needs_a_versioned_location(self):
        self.write("MODULE.bazel", "https://x/abc123.tar.gz\n")
        dep = Dep(
            "one",
            "1.0",
            locations=[Location("MODULE.bazel", "https://x/abc123.tar.gz")],
        )
        with self.assertRaises(ValueError):
            deps.plan_update(self.root, [(dep, "1.1")])


class TestUntracked(TreeTest):
    MODULE = f"""\
bazel_dep(name = "rules_foo", version = "1.0")

http_archive(
    name = "bar",
    sha256 = "{SHA_A}",
    url = "https://example.com/bar-2.0.tar.gz",
)
"""

    def test_untracked(self):
        self.write("MODULE.bazel", self.MODULE)
        self.write("a/BUILD.bazel", 'x = "@nuget.system.buffers.v4.4.0//:lib"\n')
        errors = deps.untracked(self.root, [])
        self.assertEqual(3, len(errors))
        self.assertIn("MODULE.bazel:1", errors[0])
        self.assertIn("no sha256 location", errors[1])
        self.assertIn("a/BUILD.bazel:1", errors[2])

    def test_tracked(self):
        self.write("MODULE.bazel", self.MODULE)
        tracked = [
            Dep(
                "rules_foo",
                "1.0",
                locations=[
                    Location(
                        "MODULE.bazel",
                        'bazel_dep(name = "rules_foo", version = "{version}")',
                    )
                ],
            ),
            Dep(
                "bar",
                "2.0",
                locations=[
                    Location(
                        "MODULE.bazel", "https://example.com/bar-{version}.tar.gz", True
                    )
                ],
            ),
        ]
        self.assertEqual([], deps.untracked(self.root, tracked))

    def test_skips_bazel_output(self):
        self.write("bazel-out/x/MODULE.bazel", self.MODULE)
        self.assertEqual([], deps.untracked(self.root, []))

    def test_requirement_floors(self):
        self.write("a/pyproject.toml", 'dependencies = ["jinja2", "pytest >= 9"]\n')
        self.write("b/rockspec.tmpl", 'dependencies = {\n  "luasocket >= 3.0",\n}\n')
        self.write("c/vcpkg.json", '{ "name": "asio", "version>=": "1.20.0" }\n')
        self.write("d/CMakeLists.txt", "find_package(Asio 1.20.0)\n")
        errors = deps.untracked(self.root, [])
        self.assertEqual(4, len(errors))

    @unittest.skipUnless(shutil.which("git"), "needs git")
    def test_skips_ignored_files(self):
        self.write(".gitignore", "venv/\n")
        self.write("venv/MODULE.bazel", self.MODULE)
        self.write("new/BUILD.bazel", 'x = "@nuget.system.buffers.v4.4.0//:lib"\n')
        subprocess.run(["git", "init", "-q"], cwd=self.root, check=True)
        (error,) = deps.untracked(self.root, [])
        self.assertIn("new/BUILD.bazel:1", error)

    def test_line_of_a_form_feed_file(self):
        self.write("MODULE.bazel", 'x = 1\f\nbazel_dep(name = "a", version = "1")\n')
        (error,) = deps.untracked(self.root, [])
        self.assertIn("MODULE.bazel:2 'bazel_dep(", error)

    def test_glob_match(self):
        self.assertTrue(deps.glob_match("a/b/BUILD.bazel", "**/BUILD.bazel"))
        self.assertTrue(deps.glob_match("BUILD.bazel", "**/BUILD.bazel"))
        self.assertTrue(deps.glob_match("a/x.csproj", "**/*.csproj"))
        self.assertTrue(deps.glob_match("a/CMakeLists.txt.tmpl", "**/CMakeLists.txt*"))
        self.assertTrue(deps.glob_match(".github/workflows/ci.yml", ".github/**/*.yml"))
        self.assertFalse(deps.glob_match("a/MODULE.bazel.lock", "MODULE.bazel"))


class TestManifest(TreeTest):
    def test_load(self):
        self.write(
            "tools/deps/deps.toml",
            """\
            [[dep]]
            name = "foo"
            version = "1.0"
            source = "pypi:foo"
            regen = ["pip"]
            at = ["a.txt: foo=={version}", { file = "b.txt", text = "x{version}", sha256 = true }]
            """,
        )
        (dep,) = deps.load_manifest(self.root)
        self.assertEqual("pypi:foo", dep.source)
        self.assertEqual(Location("a.txt", "foo=={version}"), dep.locations[0])
        self.assertTrue(dep.locations[1].sha256)

    def test_unknown_key(self):
        self.write(
            "tools/deps/deps.toml", '[[dep]]\nname = "foo"\nversion = "1"\nsrc = "x"\n'
        )
        with self.assertRaises(ValueError):
            deps.load_manifest(self.root)

    def test_unknown_regenerator(self):
        self.write(
            "tools/deps/deps.toml",
            '[[dep]]\nname = "foo"\nversion = "1"\nregen = ["x"]\n',
        )
        with self.assertRaises(ValueError):
            deps.load_manifest(self.root)


class TestLookupOrder(unittest.TestCase):
    def test_uses_the_updated_pin(self):
        sdk = Dep(
            "sdk",
            "1.0",
            source="s",
            verify=[Verify("regex:https://x/{dep:rules}", "{version}", "(.*)")],
        )
        rules = Dep("rules", "0.1", source="r")

        class Fake(FakeSources):
            def lookup(self, source, tag):
                if source.startswith("regex:"):
                    return {"regex:https://x/0.2": ["1.0", "1.1"]}.get(source, ["1.0"])
                return super().lookup(source, tag)

        sources = Fake({"s": ["1.0", "1.1"], "r": ["0.1", "0.2"]})
        sources.pins = {"sdk": "1.0", "rules": "0.1"}
        found = deps.statuses(sources, [sdk, rules], lambda dep: True)
        self.assertEqual("0.2", found["rules"].target)
        self.assertEqual("1.1", found["sdk"].target)

    def test_held_pin_stays(self):
        sdk = Dep(
            "sdk", "1.0", source="s", verify=[Verify("v{dep:rules}", "{version}")]
        )
        rules = Dep("rules", "0.1", source="r", hold="held")
        sources = FakeSources(
            {"s": ["1.0", "1.1"], "r": ["0.1", "0.2"], "v0.1": ["1.0"], "v0.2": ["1.1"]}
        )
        sources.pins = {"sdk": "1.0", "rules": "0.1"}
        found = deps.statuses(sources, [sdk, rules], lambda dep: dep.hold is None)
        self.assertEqual("1.0", found["sdk"].target)


class TestSources(unittest.TestCase):
    def test_pypi_skips_yanked(self):
        data = {
            "releases": {
                "1.0": [{"yanked": False}],
                "1.1": [{"yanked": True}],
                "1.2": [],
            }
        }
        sources = Sources(
            fetch=lambda url: json.dumps(data).encode(), gh=FakeSources.fail
        )
        self.assertEqual(["1.0"], sources.versions("pypi:foo"))

    def test_pypi_skips_newer_python(self):
        data = {
            "releases": {
                "1.0": [{"yanked": False, "requires_python": ">=3.10"}],
                "1.1": [{"yanked": False, "requires_python": ">=3.13"}],
                "1.2": [{"yanked": False, "requires_python": None}],
            }
        }
        sources = Sources(
            fetch=lambda url: json.dumps(data).encode(),
            gh=FakeSources.fail,
            pins={"python": "3.12"},
        )
        self.assertEqual(["1.0", "1.2"], sources.versions("pypi:foo"))

    def test_supports_python(self):
        def supports(spec):
            return deps.supports_python([{"requires_python": spec}], "3.12")

        self.assertTrue(supports(">=3.12.0"))
        self.assertTrue(supports(">=3.8, <4"))
        self.assertTrue(supports(">=3.8, !=3.11.*"))
        self.assertFalse(supports(">=3.13"))
        self.assertFalse(supports("<3.12"))

    def test_github_release_skips_prereleases(self):
        releases = [
            {"tag_name": "v1.0.0", "draft": False, "prerelease": False},
            {"tag_name": "v1.1.0", "draft": False, "prerelease": True},
            {"tag_name": "v1.2.0", "draft": True, "prerelease": False},
        ]
        sources = Sources(fetch=FakeSources.fail, gh=lambda path: releases)
        self.assertEqual(["1.0.0"], sources.versions("github-release:owner/repo"))

    def test_nuget_skips_unlisted(self):
        def leaf(version, listed=None):
            entry = {"version": version}
            if listed is not None:
                entry["listed"] = listed
            return {"catalogEntry": entry}

        pages = {
            "https://api.nuget.org/v3/registration5-gz-semver2/foo/index.json": {
                "items": [
                    {"items": [leaf("1.0"), leaf("1.1", False)]},
                    {"@id": "https://page2"},
                ]
            },
            "https://page2": {"items": [leaf("1.2", True)]},
        }

        def fetch(url):
            return gzip.compress(json.dumps(pages[url]).encode())

        sources = Sources(fetch=fetch, gh=FakeSources.fail)
        self.assertEqual(["1.0", "1.2"], sources.versions("nuget:Foo"))

    def test_docker_reads_every_page(self):
        pages = {
            "https://hub.docker.com/v2/repositories/library/x/tags?page_size=100": {
                "results": [{"name": "24.04"}, {"name": "latest"}],
                "next": "https://page2",
            },
            "https://page2": {"results": [{"name": "26.04"}], "next": None},
        }
        sources = Sources(
            fetch=lambda url: json.dumps(pages[url]).encode(), gh=FakeSources.fail
        )
        self.assertEqual(
            ["24.04", "26.04"], sources.versions("docker:library/x", r"(\d+\.04)")
        )

    def test_regex_with_pinned_dependency(self):
        fetched = []

        def fetch(url):
            fetched.append(url)
            return b'"10.0.400": {\n"10.0.303": {\n'

        sources = Sources(fetch=fetch, gh=FakeSources.fail, pins={"rules": "0.22.2"})
        versions = sources.versions(
            r"regex:https://x/v{dep:rules}/v.bzl", r'"([\d.]+)": \{'
        )
        self.assertEqual(["10.0.400", "10.0.303"], versions)
        self.assertEqual(["https://x/v0.22.2/v.bzl"], fetched)


class TestMods(unittest.TestCase):
    REGISTRY = """\
MODS = [
    mod(
        name = "Harmony",
        repo = "KSPModdingLibs/HarmonyKSP",
        versions = [version(
            "2.2.1.0",
            source = "releases/download/{version}/Harmony.zip",
        )],
    ),
    mod(
        name = "Unpinned",
        versions = [],
    ),
]
"""

    def test_parse(self):
        pins = deps.parse_mods(self.REGISTRY)
        self.assertEqual(
            [deps.ModPin("Harmony", "KSPModdingLibs/HarmonyKSP", "2.2.1.0")], pins
        )

    def test_needs_gh(self):
        root = Path(tempfile.mkdtemp())
        self.addCleanup(shutil.rmtree, root)
        (root / "service").mkdir()
        (root / "service/registry.bzl").write_text(self.REGISTRY)
        with mock.patch.object(deps, "gh_api", side_effect=FileNotFoundError("gh")):
            with self.assertRaises(SystemExit):
                deps.report_mods(root)

    def test_newer(self):
        self.assertTrue(deps.mod_release_is_newer("v1.0.44", "1.0.41"))
        self.assertTrue(deps.mod_release_is_newer("v0.17.0.0_Maxwell", "0.16.1.2"))
        self.assertFalse(deps.mod_release_is_newer("21.1", "22.1"))
        self.assertFalse(deps.mod_release_is_newer("2.2.1.0", "2.2.1.0"))


if __name__ == "__main__":
    unittest.main()
