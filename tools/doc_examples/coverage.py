"""The checks that every documentation example of a game is declared in its manifest or listed
as unsupported, and that neither names an example that does not exist."""

from __future__ import annotations

import collections
import pathlib
import unittest
from collections.abc import Mapping, Sequence
from typing import TYPE_CHECKING, Any, ClassVar

from tools.doc_examples import listing
from tools.doc_examples.spec import Example, example_key

_Base: Any = unittest.TestCase if TYPE_CHECKING else object


def example_files(scripts: pathlib.Path) -> dict[str, set[str]]:
    """The languages of each example key."""
    files: dict[str, set[str]] = collections.defaultdict(set)
    for path in scripts.rglob("*"):
        if path.is_file():
            found = example_key(path.relative_to(scripts))
            if found:
                language, key = found
                files[key].add(language)
    return files


class Coverage(_Base):
    """The checks, for a unittest.TestCase that sets the class attributes."""

    # The scripts folder, the manifest and the listing of unsupported examples
    scripts: ClassVar[pathlib.Path]
    examples: ClassVar[Sequence[Example]]
    setups: ClassVar[Mapping[str, Any]]
    listing: ClassVar[pathlib.Path]
    # The fewest example files the scripts folder holds, to catch a wrong path
    minimum: ClassVar[int]

    @classmethod
    def setUpClass(cls):
        cls.files = example_files(cls.scripts)
        cls.unsupported = listing.read(cls.listing)
        cls.declared = {e.key for e in cls.examples}

    def test_found_examples(self):
        self.assertGreaterEqual(
            len(self.files), self.minimum, "no examples found in " + str(self.scripts)
        )

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
        counts = collections.Counter(e.key for e in self.examples)
        self.assertEqual([], [key for key, n in counts.items() if n > 1])

    def test_declared_examples_exist(self):
        self.assertEqual([], sorted(self.declared - self.files.keys()))

    def test_declared_setups_exist(self):
        self.assertEqual(
            [], [e.key for e in self.examples if e.setup not in self.setups]
        )

    def test_per_language_overrides(self):
        bad = [
            f"{language} {e.key}"
            for e in self.examples
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
