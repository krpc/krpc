"""Builds the binaries of the selected examples once, before the game starts, so a compile error
fails the run early."""

from __future__ import annotations

import pytest

from tools.doc_examples.classes import build_examples


def pytest_collection_finish(session: pytest.Session) -> None:
    build_examples(session)
