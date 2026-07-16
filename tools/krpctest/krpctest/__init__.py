"""Automated integration-testing framework for kRPC services.

The framework is split into focused submodules:

 * :mod:`krpctest.testcase` — the ``TestCase`` base class tests subclass.
 * :mod:`krpctest.assertions` — tolerance-based assertion helpers.
 * :mod:`krpctest.game` — KSP process lifecycle and server-connection management.
 * :mod:`krpctest.env` — filesystem locations shared with the install/run entrypoints.
 * :mod:`krpctest.geometry` — vector/quaternion math used by the tests.

``TestCase`` is re-exported here so existing ``import krpctest; krpctest.TestCase`` usage
keeps working.
"""

from krpctest.testcase import TestCase

__all__ = ["TestCase"]
