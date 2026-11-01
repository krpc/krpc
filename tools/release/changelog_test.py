import tempfile
import unittest
from pathlib import Path

import changelog

CHANGELOG = """\
## [v0.8.0] - unreleased

- Connections
  - Connect over one connection (#1219)

- Fix a typo (#1185)

## [v0.7.0]
- Initial version
"""


def texts(nodes):
    return [(node.text, texts(node.children)) for node in nodes]


class FragmentFiles:
    """Fragment files written to a temporary directory."""

    def __init__(self, test, files):
        directory = tempfile.TemporaryDirectory()
        test.addCleanup(directory.cleanup)
        self.paths = []
        for name, text in files.items():
            path = Path(directory.name) / changelog.FRAGMENT_DIR / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(text)
            self.paths.append(str(path))


class TestParse(unittest.TestCase):
    def test_changelog(self):
        sections = changelog.parse_changelog(CHANGELOG, "CHANGELOG.md")
        self.assertEqual(["0.8.0", "0.7.0"], [s.version for s in sections])
        self.assertEqual([True, False], [s.unreleased for s in sections])
        self.assertEqual(
            [
                ("Connections", [("Connect over one connection (#1219)", [])]),
                ("Fix a typo (#1185)", []),
            ],
            texts(sections[0].nodes),
        )

    def test_changelog_wrapped(self):
        sections = changelog.parse_changelog(
            "## [v1.0.0]\n- A long\n  entry\n  - and a\n    sub-item\n", "c"
        )
        self.assertEqual(
            [("A long entry", [("and a sub-item", [])])], texts(sections[0].nodes)
        )

    def test_changelog_stray_text(self):
        with self.assertRaisesRegex(
            changelog.ChangelogError, r"^c:3: expected a '- ' entry"
        ):
            changelog.parse_changelog("## [v1.0.0]\n- An entry\nstray text\n", "c")

    def test_changelog_headers(self):
        sections = changelog.parse_changelog(
            "# Preamble\n## [v1.0.1] - unreleased\n## [v1.0.0.post1]\n## [v1.0.0]\n",
            "c",
        )
        self.assertEqual(
            [("1.0.1", True), ("1.0.0.post1", False), ("1.0.0", False)],
            [(s.version, s.unreleased) for s in sections],
        )

    def test_changelog_bad_header(self):
        for header in (
            "## [1.0.0]",
            "## [v1.0.0] - Unreleased",
            "## [v1.0.0] - 2026-11-01",
            "## [v1.0]",
        ):
            with self.subTest(header):
                with self.assertRaisesRegex(
                    changelog.ChangelogError, r"^c:2: expected '## \[vX.Y.Z\]'"
                ):
                    changelog.parse_changelog(
                        f"# Preamble\n{header}\n- An entry\n", "c"
                    )

    def test_changelog_nested_too_deep(self):
        for line in ("    - c", "   - c", " - c"):
            with self.subTest(line):
                with self.assertRaisesRegex(
                    changelog.ChangelogError, r"^c:4: an entry is a '- ' line"
                ):
                    changelog.parse_changelog(f"## [v1.0.0]\n- a\n  - b\n{line}\n", "c")

    def test_fragment(self):
        fragment = changelog.parse_fragment(
            "## client/python\n\n- Connections\n  - A change (#1219)\n\n## server\n- Another\n",
            "f.md",
        )
        self.assertEqual(["client/python", "server"], list(fragment.sections))
        self.assertEqual([1219], fragment.numbers())

    def check_fragment_error(self, text, pattern):
        with self.assertRaisesRegex(changelog.ChangelogError, pattern):
            changelog.parse_fragment(text, "f.md")

    def test_numbers(self):
        fragment = changelog.parse_fragment(
            "## client/csharp\n- Support C#10 records (#1240, #1239)\n", "f.md"
        )
        self.assertEqual([1240, 1239], fragment.numbers())

    def test_fragment_text_outside_section(self):
        self.check_fragment_error(
            "- An entry\n", r"^f.md:1: text outside a component section"
        )

    def test_fragment_unknown_component(self):
        self.check_fragment_error(
            "## client/ruby\n- An entry\n", r"^f.md:1: 'client/ruby' is not"
        )

    def test_fragment_buildenv(self):
        self.check_fragment_error(
            "## tools/buildenv\n- An entry\n", r"^f.md:1: 'tools/buildenv'"
        )

    def test_fragment_bad_heading(self):
        self.check_fragment_error(
            "# server\n- An entry\n", r"^f.md:1: expected '## <component>'"
        )

    def test_fragment_duplicate_section(self):
        self.check_fragment_error(
            "## server\n- An entry\n## server\n- Another\n",
            r"^f.md:3: '## server' appears twice",
        )

    def test_fragment_empty_section(self):
        self.check_fragment_error(
            "## server\n## core\n- An entry\n", r"^f.md:1: '## server' has no"
        )

    def test_fragment_empty(self):
        self.check_fragment_error("\n", r"^f.md: no entries")

    def test_fragment_bad_line(self):
        self.check_fragment_error(
            "## server\nAn entry\n", r"^f.md:2: expected a '- ' entry"
        )

    def test_fragment_blank_inside_entry(self):
        self.check_fragment_error(
            "## server\n- An\n\n  entry\n", r"^f.md:4: blank line inside"
        )


class TestLoad(unittest.TestCase):
    def test_none(self):
        self.assertEqual([], changelog.load_fragments([]))

    def test_missing_directory(self):
        with tempfile.TemporaryDirectory() as root:
            self.assertEqual([], changelog.fragment_paths(root))

    def test_order(self):
        files = FragmentFiles(
            self,
            {
                "b.md": "## server\n- Entry (#1230)\n",
                "c.md": "## server\n- Entry (#1100)\n## core\n- Entry (#1228)\n",
                "a.md": "## server\n- Entry\n",
            },
        )
        fragments = changelog.load_fragments(files.paths)
        self.assertEqual(["c.md", "b.md", "a.md"], [f.name for f in fragments])

    def test_bad_names(self):
        for name in ("fix-wait.txt", "fix-wait.MD", "fix wait.md"):
            with self.subTest(name=name):
                files = FragmentFiles(self, {name: "## server\n- Entry\n"})
                with self.assertRaisesRegex(
                    changelog.ChangelogError, "a fragment is named"
                ):
                    changelog.load_fragments(files.paths)

    def test_subdirectory(self):
        files = FragmentFiles(self, {"sub/fix-wait.md": "## server\n- Entry\n"})
        with self.assertRaisesRegex(
            changelog.ChangelogError, "sits directly in changelog.d/"
        ):
            changelog.load_fragments(files.paths)

    def test_every_error_reported(self):
        files = FragmentFiles(self, {"a.txt": "## server\n- Entry\n", "b.md": "text\n"})
        with self.assertRaises(changelog.ChangelogError) as context:
            changelog.load_fragments(files.paths)
        self.assertEqual(2, len(str(context.exception).splitlines()))


class TestMerge(unittest.TestCase):
    def fragments(self, *texts):
        return [
            changelog.parse_fragment(text, f"{i}.md") for i, text in enumerate(texts)
        ]

    def test_topics_merge(self):
        sections = changelog.parse_changelog(CHANGELOG, "CHANGELOG.md")
        fragments = self.fragments(
            "## server\n- Connections\n  - Second (#1)\n- Streams\n  - Third (#2)\n",
            "## server\n- Streams\n  - Fourth (#3)\n- Plain (#4)\n",
        )
        merged = changelog.merge(
            sections[0].nodes, changelog.additions(fragments, "server")
        )
        self.assertEqual(
            [
                (
                    "Connections",
                    [("Connect over one connection (#1219)", []), ("Second (#1)", [])],
                ),
                ("Fix a typo (#1185)", []),
                ("Streams", [("Third (#2)", []), ("Fourth (#3)", [])]),
                ("Plain (#4)", []),
            ],
            texts(merged),
        )

    def test_merge_leaves_input(self):
        sections = changelog.parse_changelog(CHANGELOG, "CHANGELOG.md")
        fragments = self.fragments("## server\n- Connections\n  - Second (#1)\n")
        changelog.merge(sections[0].nodes, changelog.additions(fragments, "server"))
        self.assertEqual(1, len(sections[0].nodes[0].children))


class TestFold(unittest.TestCase):
    def test_fold_matches_merge(self):
        fragments = [
            changelog.parse_fragment(
                "## server\n- Connections\n  - Second\n    wrapped (#1)\n- Plain (#2)\n",
                "a.md",
            )
        ]
        folded = changelog.fold(CHANGELOG, "c", "0.8.0", fragments, "server")
        self.assertEqual(
            """\
## [v0.8.0] - unreleased

- Connections
  - Connect over one connection (#1219)
  - Second
    wrapped (#1)

- Fix a typo (#1185)
- Plain (#2)

## [v0.7.0]
- Initial version
""",
            folded,
        )
        sections = changelog.parse_changelog(CHANGELOG, "c")
        merged = changelog.merge(
            sections[0].nodes, changelog.additions(fragments, "server")
        )
        self.assertEqual(
            texts(merged), texts(changelog.parse_changelog(folded, "c")[0].nodes)
        )

    def test_fold_adds_section(self):
        fragments = [changelog.parse_fragment("## server\n- New (#1)\n", "a.md")]
        folded = changelog.fold(
            "## [v0.7.0]\n- Initial version\n", "c", "0.8.0", fragments, "server"
        )
        self.assertEqual(
            "## [v0.8.0] - unreleased\n- New (#1)\n\n## [v0.7.0]\n- Initial version\n",
            folded,
        )

    def test_fold_other_component(self):
        fragments = [changelog.parse_fragment("## core\n- New (#1)\n", "a.md")]
        self.assertEqual(
            CHANGELOG, changelog.fold(CHANGELOG, "c", "0.8.0", fragments, "server")
        )

    def test_fold_released(self):
        fragments = [changelog.parse_fragment("## server\n- New (#1)\n", "a.md")]
        with self.assertRaisesRegex(
            changelog.ChangelogError, "v0.7.0 is already released"
        ):
            changelog.fold(CHANGELOG, "c", "0.7.0", fragments, "server")

    def test_strip_unreleased(self):
        self.assertTrue(
            changelog.strip_unreleased(CHANGELOG, "0.8.0").startswith("## [v0.8.0]\n")
        )


if __name__ == "__main__":
    unittest.main()
