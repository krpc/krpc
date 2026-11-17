"""Builds the binaries of the selected examples once, before the game starts, so a compile error
fails the run early."""

from __future__ import annotations

import pytest

from doc.test import languages


def pytest_collection_finish(session: pytest.Session) -> None:
    if session.config.option.collectonly:
        return
    examples = []
    for item in session.items:
        function = getattr(item, "obj", None)
        example = getattr(function, "example", None)
        if example is not None:
            examples.append(example)
    languages.build(examples)
