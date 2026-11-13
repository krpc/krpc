"""Checks each conversion krpctools makes against the test vectors of the name case conversion
specification.

  naming_test.py NAMES STARLARK

NAMES is client/conformance/names.json. STARLARK holds the snake case clientgen.bzl gives each
name, as a json object.
"""

import json
import sys

import pytest

from krpc.utils import lower_camel_case, snake_case, upper_snake_case

NAMES_PATH, STARLARK_PATH = sys.argv[-2:]
with open(NAMES_PATH, encoding="utf-8") as names_file:
    NAMES = json.load(names_file)


@pytest.fixture(params=NAMES, ids=[x["name"] for x in NAMES])
def vector(request):
    return request.param


def test_snake_case(vector):
    assert snake_case(vector["name"]) == vector["snake"]


def test_upper_snake_case(vector):
    assert upper_snake_case(vector["name"]) == vector["upper_snake"]


def test_lower_camel_case(vector):
    assert lower_camel_case(vector["name"]) == vector["lower_camel"]


def test_starlark_snake_case():
    with open(STARLARK_PATH, encoding="utf-8") as starlark_file:
        starlark = json.load(starlark_file)
    assert starlark == {x["name"]: x["snake"] for x in NAMES}


if __name__ == "__main__":
    sys.exit(pytest.main([__file__, "-p", "no:cacheprovider"]))
