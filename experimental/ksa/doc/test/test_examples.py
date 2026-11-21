"""Runs each kRPC for KSA documentation example in game, in each language whose examples run.
The test classes are built by tools.doc_examples.classes."""

from __future__ import annotations

import pathlib
import sys

import pytest

# The runner reads the output and stops the process group with POSIX calls
if sys.platform == "win32":
    pytest.skip("The examples run on Linux only", allow_module_level=True)

from experimental.ksa.doc.test.examples import EXAMPLES, SETUPS  # noqa: E402
from experimental.ksa.doc.test.harness import ExampleTestCase  # noqa: E402
from tools.doc_examples import listing  # noqa: E402
from tools.doc_examples.classes import example_classes  # noqa: E402

_UNSUPPORTED = listing.read(pathlib.Path(__file__).parent / "unsupported.txt")

globals().update(example_classes(ExampleTestCase, SETUPS, EXAMPLES, _UNSUPPORTED))
