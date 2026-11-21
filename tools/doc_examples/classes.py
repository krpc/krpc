"""The test classes of a game's examples, one per setup, with a test per example and language.

A test class holds the examples that share a setup, so the game is set up once for them, and
carries the setup's mods, so the examples that need the same mods run together. Each test is
named for its language and example, as test_<language>__<example>, and marked example, with
its language, and slow for a long flight."""

from __future__ import annotations

import re
from collections.abc import Callable, Iterable, Mapping
from typing import Any

import pytest

from tools.doc_examples.harness import ExampleRunner
from tools.doc_examples.listing import Entry, find
from tools.doc_examples.spec import Example, example_path


def _test(example: Example, language: str) -> Callable[[ExampleRunner], None]:
    def test(self: ExampleRunner) -> None:
        self.run_example(example, language)

    # The language and path, from which build_examples builds the example before the run
    test.example = (language, example_path(example.key, language))  # pyright: ignore[reportFunctionMemberAccess]

    marks = [pytest.mark.example, getattr(pytest.mark, language)]
    if example.slow:
        marks.append(pytest.mark.slow)
    for mark in marks:
        test = mark(test)
    return test


def _name(text: str) -> str:
    return re.sub(r"\W", "_", text)


def example_classes(
    base: type[ExampleRunner],
    setups: Mapping[str, Any],
    examples: Iterable[Example],
    unsupported: list[Entry],
) -> dict[str, type[ExampleRunner]]:
    """The test classes, on base, by name. A setup's mods, if it has any, are the class's."""
    examples = list(examples)
    scripts = base.languages.scripts
    classes: dict[str, type[ExampleRunner]] = {}
    for setup_name, setup in setups.items():
        tests = {}
        for example in examples:
            if example.setup != setup_name:
                continue
            for language in base.languages.languages:
                path = example_path(example.key, language)
                if not (scripts / path).exists():
                    continue
                if find(unsupported, language, example.key):
                    continue
                name = "test_%s__%s" % (language, _name(example.key))
                tests[name] = _test(example, language)
        if tests:
            class_name = "Test" + "".join(
                part.capitalize() for part in setup_name.split("-")
            )
            attributes = {
                "setup": setup,
                "mods": list(getattr(setup, "mods", ())),
                **tests,
            }
            classes[class_name] = type(class_name, (base,), attributes)
    return classes


def build_examples(session: pytest.Session) -> None:
    """Builds the binaries of the selected examples once, before the game starts, so a
    compile error fails the run early. For a pytest_collection_finish hook."""
    if session.config.option.collectonly:
        return
    by_languages: dict[int, tuple[Any, list]] = {}
    for item in session.items:
        function = getattr(item, "obj", None)
        example = getattr(function, "example", None)
        cls = getattr(item, "cls", None)
        if example is None or cls is None:
            continue
        languages = cls.languages
        by_languages.setdefault(id(languages), (languages, []))[1].append(example)
    for languages, selected in by_languages.values():
        languages.build(selected)
