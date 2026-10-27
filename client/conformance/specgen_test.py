"""Tests for the generator of the specification's requirement lists."""

import os
import tempfile
import textwrap
import unittest

import specgen

TESTS = '''
from driver import profile, section, should, untested


@profile("streams")
def test_first(connected):
    """The first requirement, before any section."""


section("Waiting", "Some prose.")


@should
@profile("streams", "threads")
def test_two_words(connected):
    """A requirement that spans
    two lines."""
    assert True


@profile("streams", "threads")
@untested("No command for it")
def test_third():
    """The third requirement."""
'''

EXPECTED = """\
.. _spec-area-first:

``area-first [streams] [MUST]``
   The first requirement, before any section.

Waiting
-------

Some prose.

.. _spec-area-two-words:

``area-two-words [streams, threads] [SHOULD]``
   A requirement that spans
   two lines.

.. _spec-area-third:

``area-third [streams, threads] [MUST]``
   The third requirement.
"""


class TestSpecgen(unittest.TestCase):
    def setUp(self) -> None:
        self.directory = tempfile.TemporaryDirectory()
        self.path = os.path.join(self.directory.name, "test_area.py")
        with open(self.path, "w", encoding="utf-8") as tests:
            tests.write(TESTS)

    def tearDown(self) -> None:
        self.directory.cleanup()

    def test_read_area(self) -> None:
        sections = specgen.read_area(self.path)
        self.assertEqual([None, "Waiting"], [x.title for x in sections])
        third = sections[1].requirements[1]
        self.assertEqual("area-third", third.id)
        self.assertEqual("No command for it", third.untested)

    def test_render(self) -> None:
        self.assertEqual(EXPECTED, specgen.render(specgen.read_area(self.path)))

    def test_requirement_profiles(self) -> None:
        with open(self.path, "a", encoding="utf-8") as tests:
            tests.write(
                textwrap.dedent('''
                @profile("defaults")
                def test_fourth(connected):
                    """The fourth requirement."""
                ''')
            )
            tests.write(
                textwrap.dedent('''
                def test_fifth(connected):
                    """The fifth requirement."""
                ''')
            )
            tests.write(
                textwrap.dedent('''
                @profile("!error-types")
                def test_sixth(connected):
                    """The sixth requirement."""
                ''')
            )
        out = specgen.render(specgen.read_area(self.path))
        self.assertIn("``area-fourth [defaults] [MUST]``\n", out)
        self.assertIn("``area-fifth [core] [MUST]``\n", out)
        self.assertIn("``area-sixth [core, without error-types] [MUST]``\n", out)

    def test_coverage_status(self) -> None:
        sections = specgen.read_area(self.path)
        first, two_words, third = [r for x in sections for r in x.requirements]
        client = specgen.Client(
            "Python",
            {"core", "streams", "threads"},
            {"test_area.py::test_first": {""}, "test_area.py::test_two_words[a]": {""}},
        )
        self.assertEqual("fail", client.status(self.path, "test_first", first))
        self.assertEqual(
            "partial", client.status(self.path, "test_two_words", two_words)
        )
        self.assertEqual("untested", client.status(self.path, "test_third", third))
        client.expected_failures = {}
        self.assertEqual("pass", client.status(self.path, "test_first", first))
        client.unsupported = {"test_area.py::test_first": {""}}
        self.assertEqual("unsupported", client.status(self.path, "test_first", first))
        client.profiles = {"core"}
        self.assertEqual("unsupported", client.status(self.path, "test_first", first))

    def test_coverage_excluded_profile(self) -> None:
        excluded = specgen.Requirement("area-first", "Text.", profiles=["!error-types"])
        client = specgen.Client("Python", {"core"}, {})
        self.assertEqual("pass", client.status(self.path, "test_first", excluded))
        client.profiles.add("error-types")
        self.assertEqual(
            "unsupported", client.status(self.path, "test_first", excluded)
        )

    def test_coverage_protocol(self) -> None:
        first = specgen.read_area(self.path)[0].requirements[0]
        client = specgen.Client(
            "Python", {"core", "streams"}, {"test_area.py::test_first": {"serialio"}}
        )
        self.assertEqual("partial", client.status(self.path, "test_first", first))
        client.expected_failures["test_area.py::test_first"].add("")
        self.assertEqual("fail", client.status(self.path, "test_first", first))

    def test_read_listing(self) -> None:
        path = os.path.join(self.directory.name, "listing.txt")
        with open(path, "w", encoding="utf-8") as listing:
            listing.write(
                "# A comment\n"
                "test_area.py::test_first  # Fails\n"
                "test_area.py::test_third@serialio @flaky  # Flaky over serialio\n"
                "test_area.py::test_fourth @skip  # Hangs\n"
            )
        self.assertEqual(
            {
                "test_area.py::test_first": {""},
                "test_area.py::test_third": {"serialio"},
                "test_area.py::test_fourth": {""},
            },
            specgen._read_listing(path),
        )

    def test_section_keyword(self) -> None:
        with open(self.path, "a", encoding="utf-8") as tests:
            tests.write(
                textwrap.dedent('''
                section("Later", text="Keyword prose.")


                def test_last(connected):
                    """The last requirement."""
                ''')
            )
        sections = specgen.read_area(self.path)
        self.assertEqual("Keyword prose.", sections[-1].text)

    def test_render_coverage(self) -> None:
        client = specgen.Client(
            "Python", {"core", "streams"}, {"test_area.py::test_first": {""}}
        )
        out = specgen.render_coverage([self.path], [client])
        self.assertIn("   * - Python\n     - ``core``, ``streams``\n", out)
        self.assertIn(".. list-table:: Area\n", out)
        self.assertIn(
            "   * - :ref:`area-first <spec-area-first>`\n"
            "     - :coverage-fail:`\u2717`\n",
            out,
        )
        # A requirement outside the client's profiles is a blank cell
        self.assertIn(
            "   * - :ref:`area-two-words <spec-area-two-words>`\n     -\n", out
        )

    def test_missing_docstring(self) -> None:
        with open(self.path, "a", encoding="utf-8") as tests:
            tests.write("\n\ndef test_bare(connected):\n    pass\n")
        with self.assertRaises(SystemExit):
            specgen.read_area(self.path)


if __name__ == "__main__":
    unittest.main()
