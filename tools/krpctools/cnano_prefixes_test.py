"""Checks the words that start the C-nano library's macros against the list clientgen escapes a
service name against.

  cnano_prefixes_test.py SOURCE...
"""

import os
import re
import sys

import pytest

from krpctools.lang.cnano import LIBRARY_PREFIXES

SOURCES = [x for x in sys.argv[1:] if x.endswith((".c", ".h"))]


def test_library_prefixes():
    words = set()
    for path in SOURCES:
        with open(path, encoding="utf-8") as source:
            words.update(re.findall(r"\bKRPC_([A-Z0-9]+)_", source.read()))
    assert words == set(LIBRARY_PREFIXES)


if __name__ == "__main__":
    # An empty config file. Without a sandbox, as on Windows, pytest otherwise finds
    # //pytest.ini and roots collection at the execroot, where other actions add and
    # remove files.
    here = os.path.dirname(os.path.abspath(__file__))
    config = os.path.join(os.environ.get("TEST_TMPDIR", "."), "pytest.ini")
    with open(config, "w", encoding="utf-8"):
        pass
    args = [__file__, "-c", config, "--rootdir=" + here, "-p", "no:cacheprovider"]
    sys.exit(pytest.main(args))
