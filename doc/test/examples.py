"""The manifest: how to run each documentation example, and what it must do.

Each Example covers the files of every language that share its key."""

from __future__ import annotations

from doc.test.spec import Example, Setup

# In the order the test classes run: the shared setups first, then those that relaunch for
# every example
SETUPS: dict[str, Setup] = {}

EXAMPLES: list[Example] = []
