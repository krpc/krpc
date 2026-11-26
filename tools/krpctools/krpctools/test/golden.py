"""The golden files that the generators' output is checked against"""

import os
import unittest
from importlib.resources import files
from pathlib import Path

# Set by //:update-goldens, which rewrites the goldens in the source tree instead of checking them
UPDATE = "KRPCTOOLS_UPDATE_GOLDENS"
SOURCE = "tools/krpctools/krpctools/test"


def check_golden(test: unittest.TestCase, name: str, actual: str) -> None:
    """Checks the output against the named golden file, or rewrites the file"""
    if os.environ.get(UPDATE):
        path = Path(os.environ["BUILD_WORKSPACE_DIRECTORY"], SOURCE, name)
        path.write_text(actual, encoding="utf-8", newline="")
        return
    expected = files("krpctools.test").joinpath(name).read_text(encoding="utf-8")
    test.assertEqual(expected, actual)
