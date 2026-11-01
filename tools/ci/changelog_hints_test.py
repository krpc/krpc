import unittest

from changelog_hints import File, Hint, PullRequest, added_lines, comment_body, hints

SOURCE = File("service/SpaceCenter/src/Services/Vessel.cs", "modified")
FRAGMENT = "changelog.d/fix-vessel.md"


def pull_request(files, fragments=None, body="", linked=(), labels=()):
    return PullRequest(
        number=1240,
        body=body,
        files=files,
        fragments=fragments or {},
        linked=set(linked),
        labels=set(labels),
    )


def with_fragment(text, files=(SOURCE,), **kwargs):
    return pull_request([*files, File(FRAGMENT, "added")], {FRAGMENT: text}, **kwargs)


def texts(pr):
    return [hint.text for hint in hints(pr)]


class TestHints(unittest.TestCase):
    def test_no_fragment(self):
        found = texts(pull_request([SOURCE]))
        self.assertEqual(1, len(found))
        self.assertIn(
            "changes files in `service/SpaceCenter/...`, but has no changelog entry",
            found[0],
        )

    def test_no_component_changed(self):
        self.assertEqual([], texts(pull_request([File("README.md", "modified")])))

    def test_tests_do_not_count(self):
        files = [File("service/SpaceCenter/test/test_vessel.py", "modified")]
        self.assertEqual([], texts(pull_request(files)))

    def test_fragment_matches(self):
        pr = with_fragment("## service/SpaceCenter\n- Fix a vessel (#1240)\n")
        self.assertEqual([], texts(pr))

    def test_fragment_for_another_component(self):
        found = texts(with_fragment("## server\n- Fix a vessel (#1240)\n"))
        self.assertEqual(2, len(found))
        self.assertIn(
            "adds no entry under a `## service/SpaceCenter` heading", found[0]
        )
        self.assertIn(
            f"`{FRAGMENT}` has an entry under `## server`, but this pull request "
            "doesn't change files in `server/...`",
            found[1],
        )

    def test_missing_number(self):
        found = texts(with_fragment("## service/SpaceCenter\n- Fix a vessel\n"))
        self.assertEqual(1, len(found))
        self.assertIn(f"entry on line 2 of `{FRAGMENT}`", found[0])
        self.assertIn("`(#1240)`", found[0])

    def test_related_numbers(self):
        text = "## service/SpaceCenter\n- One (#1240)\n- Two (#1001)\n- Three (#1002)\n"
        pr = with_fragment(text, body="Fixes #1001", linked=[1002])
        self.assertEqual([], texts(pr))

    def test_unrelated_number(self):
        found = texts(with_fragment("## service/SpaceCenter\n- Fix a vessel (#999)\n"))
        self.assertEqual(1, len(found))
        self.assertIn(f"entry on line 2 of `{FRAGMENT}` refers to `#999`", found[0])
        pr = with_fragment(
            "## service/SpaceCenter\n- Fix a vessel (#999)\n", body="Related to #999"
        )
        self.assertEqual([], texts(pr))

    def test_only_changed_entries_checked(self):
        text = "## service/SpaceCenter\n- Old entry (#999)\n- New entry (#1240)\n"
        patch = "@@ -1,2 +1,3 @@\n ## service/SpaceCenter\n - Old entry (#999)\n+- New entry (#1240)\n"
        pr = pull_request([SOURCE, File(FRAGMENT, "modified", patch)], {FRAGMENT: text})
        self.assertEqual([], texts(pr))

    def test_topic_heading_needs_no_number(self):
        pr = with_fragment(
            "## service/SpaceCenter\n- Vessels\n  - Fix a vessel (#1240)\n"
        )
        self.assertEqual([], texts(pr))

    def test_changelog_edit(self):
        files = [SOURCE, File("service/SpaceCenter/CHANGELOG.md", "modified")]
        found = texts(pull_request(files))
        self.assertEqual(1, len(found))
        self.assertIn(
            "don't edit `service/SpaceCenter/CHANGELOG.md` directly", found[0]
        )

    def test_release_preparation(self):
        files = [
            File("service/SpaceCenter/CHANGELOG.md", "modified"),
            File(FRAGMENT, "removed"),
            File("config.bzl", "modified"),
        ]
        self.assertEqual([], texts(pull_request(files)))

    def test_no_changelog_label(self):
        self.assertEqual([], texts(pull_request([SOURCE], labels=["no-changelog"])))

    def test_unparsable_fragment(self):
        found = texts(with_fragment("## service/SpaceCenter\n- An entry\nstray\n"))
        self.assertEqual(1, len(found))
        self.assertIn(f"`{FRAGMENT}` has a mistake", found[0])
        self.assertIn("`line 3: expected a '- ' entry", found[0])

    def test_bad_fragment_name(self):
        name = "changelog.d/fix-vessel.txt"
        pr = pull_request(
            [SOURCE, File(name, "added")],
            {name: "## service/SpaceCenter\n- A (#1240)\n"},
        )
        found = texts(pr)
        self.assertEqual(1, len(found))
        self.assertIn("a fragment is named '<branch>.md'", found[0])

    def test_other_sections_of_an_edited_fragment(self):
        text = (
            "## server\n- Old entry (#999)\n\n## service/SpaceCenter\n- New (#1240)\n"
        )
        patch = (
            "@@ -1,2 +1,5 @@\n ## server\n - Old entry (#999)\n"
            "+\n+## service/SpaceCenter\n+- New (#1240)\n"
        )
        pr = pull_request([SOURCE, File(FRAGMENT, "modified", patch)], {FRAGMENT: text})
        self.assertEqual([], texts(pr))

    def test_no_newline_marker(self):
        patch = (
            "@@ -1,2 +1,3 @@\n ## server\n-- Old\n\\ No newline at end of file\n"
            "+- New\n+- More\n"
        )
        self.assertEqual({2, 3}, added_lines(File(FRAGMENT, "modified", patch)))

    def test_language_version_is_not_a_number(self):
        pr = with_fragment("## service/SpaceCenter\n- Support C#10 (#1240)\n")
        self.assertEqual([], texts(pr))

    def test_comment(self):
        body = comment_body([Hint("A hint.")], "krpc/krpc")
        self.assertTrue(body.startswith("<!-- changelog-hints -->\n"))
        self.assertIn("- A hint.", body)
        self.assertIn("krpc/krpc/blob/main/CONTRIBUTING.md#changelog-entries", body)
        self.assertNotIn("no-changelog", body)
        body = comment_body([Hint("A hint.", needs_entry=True)], "krpc/krpc")
        self.assertIn("`no-changelog` label", body)


if __name__ == "__main__":
    unittest.main()
