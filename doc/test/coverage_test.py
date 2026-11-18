"""Checks that every documentation example is declared in the manifest or listed as
unsupported, and that neither names an example that does not exist."""

from __future__ import annotations

import collections
import pathlib
import unittest

from doc.test import listing
from doc.test.examples import EXAMPLES, SETUPS
from doc.test.spec import example_key

SCRIPTS = pathlib.Path(__file__).parent.parent / "src" / "scripts"


def example_files() -> dict[str, set[str]]:
    """The languages of each example key."""
    files: dict[str, set[str]] = collections.defaultdict(set)
    for path in SCRIPTS.rglob("*"):
        if path.is_file():
            found = example_key(path.relative_to(SCRIPTS))
            if found:
                language, key = found
                files[key].add(language)
    return files


class TestCoverage(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.files = example_files()
        cls.unsupported = listing.read()
        cls.declared = {e.key for e in EXAMPLES}

    def test_found_examples(self):
        self.assertGreater(len(self.files), 100, "no examples found in " + str(SCRIPTS))

    def test_every_example_covered(self):
        missing = [
            f"{language} {key}"
            for key, languages in sorted(self.files.items())
            for language in sorted(languages)
            if key not in self.declared
            and not listing.find(self.unsupported, language, key)
        ]
        self.assertEqual(
            [],
            missing,
            "Declare these examples in examples.py, or list them in unsupported.txt",
        )

    def test_declared_once(self):
        counts = collections.Counter(e.key for e in EXAMPLES)
        self.assertEqual([], [key for key, n in counts.items() if n > 1])

    def test_declared_examples_exist(self):
        self.assertEqual([], sorted(self.declared - self.files.keys()))

    def test_declared_setups_exist(self):
        self.assertEqual([], [e.key for e in EXAMPLES if e.setup not in SETUPS])

    def test_per_language_overrides(self):
        bad = [
            f"{language} {e.key}"
            for e in EXAMPLES
            for language in e.per_language
            if language not in self.files.get(e.key, ())
        ]
        self.assertEqual([], bad)

    def test_listed_examples_exist(self):
        stale = [
            f"unsupported.txt:{e.line}: {e.language} {e.key}"
            for e in self.unsupported
            if e.key not in self.files
            or (e.language != "*" and e.language not in self.files[e.key])
        ]
        self.assertEqual([], stale)

    def test_unsupported_not_declared(self):
        """An example unsupported in every language has no manifest entry."""
        both = [
            e.key
            for e in self.unsupported
            if e.key in self.declared
            and all(
                listing.find(self.unsupported, language, e.key)
                for language in self.files[e.key]
            )
        ]
        self.assertEqual([], both)


if __name__ == "__main__":
    unittest.main()
