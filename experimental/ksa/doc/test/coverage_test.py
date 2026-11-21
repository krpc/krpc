"""Checks that every kRPC for KSA documentation example is declared in the manifest or listed as
unsupported."""

from __future__ import annotations

import pathlib
import unittest

from experimental.ksa.doc.test.examples import EXAMPLES, SETUPS
from tools.doc_examples.coverage import Coverage

HERE = pathlib.Path(__file__).parent


class TestCoverage(Coverage, unittest.TestCase):
    scripts = HERE.parent / "src" / "scripts"
    examples = EXAMPLES
    setups = SETUPS
    listing = HERE / "unsupported.txt"
    minimum = 20


if __name__ == "__main__":
    unittest.main()
