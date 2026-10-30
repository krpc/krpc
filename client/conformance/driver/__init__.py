"""The conformance driver: runs the conformance tests against a client's testee.

Each test checks one requirement of the client specification. The requirement's id is the area
and the test's name, so test_tcpip in test_connection.py checks connection-tcpip.
The test's docstring is the text of the requirement, and the specification's requirement lists
are generated from the tests.
"""

import pytest

profile = pytest.mark.profile
timing = pytest.mark.timing
protocol = pytest.mark.protocol
should = pytest.mark.should
untested = pytest.mark.untested


def section(title: str, text: str = "") -> None:
    """Start a section of the area's specification page. The tests that follow, up to the next
    section, are its requirements. The text is any prose the section opens with."""
