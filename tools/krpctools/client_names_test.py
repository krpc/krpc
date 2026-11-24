"""Checks the copies of each client's escaped names against the lists clientgen escapes with.
A client's run-time code, its native test and its conformance conventions each keep a copy.

  client_names_test.py KEY=PATH...
"""

import json
import keyword
import os
import re
import sys

import pytest

from krpc.declarations import CONNECTION_MEMBERS as PYTHON_CONNECTION_MEMBERS
from krpctools.lang import cpp, csharp, java, js
from krpctools.lang.lua import LuaLanguage

PATHS = dict(x.split("=", 1) for x in sys.argv[1:] if "=" in x)


def read(key):
    with open(PATHS[key], encoding="utf-8") as source:
        return source.read()


def strings(text):
    return set(re.findall(r"""["']([^"']*)["']""", text))


def array(source, pattern):
    """The strings of the list a pattern opens, up to its closing bracket"""
    match = re.search(pattern + r"\s*[\[{(]([^\]})]*)", source)
    assert match is not None, pattern
    return match.group(1)


def ts_set(source, name):
    """The strings of a Set a TypeScript module declares, with the members of the sets it
    spreads"""
    body = array(source, r"const %s: ReadonlySet<string> = new Set\(" % name)
    names = strings(body)
    for spread in re.findall(r"\.\.\.(\w+)", body):
        names |= ts_set(source, spread)
    return names


def conventions(key):
    return json.loads(read(key))["naming"]


def test_csharp_connection_members():
    source = read("csharp-test")
    names = strings(array(source, r"connectionMembers ="))
    assert names == set(csharp.CONNECTION_MEMBERS)


def test_csharp_generated_type_names():
    source = read("csharp-compiler")
    names = strings(array(source, r"generatedTypeNames ="))
    assert names == set(csharp.GENERATED_TYPE_NAMES)


def test_bound_type_names():
    # C++, C# and Java nest a class Bound in each class with static methods
    assert set(cpp.GENERATED_TYPE_NAMES) == set(java.GENERATED_TYPE_NAMES)
    assert set(java.GENERATED_TYPE_NAMES) <= set(csharp.GENERATED_TYPE_NAMES)
    names = strings(array(read("java-decoder"), r"GENERATED_TYPE_NAMES = Set\.of"))
    assert names == set(java.GENERATED_TYPE_NAMES)


def test_js_names():
    source = read("js-naming")
    assert ts_set(source, "OBJECT_MEMBERS") == set(js.OBJECT_MEMBERS)
    assert ts_set(source, "CLASS_MEMBERS") == set(js.CLASS_MEMBERS)
    assert ts_set(source, "MODULE_NAMES") == set(js.MODULE_NAMES)


def test_js_conventions():
    naming = conventions("js-conventions")
    members = ts_set(read("js-naming"), "CONNECTION_MEMBERS")
    assert set(naming["service_escapes"]) == members
    assert set(naming["type_escapes"]) == set(js.MODULE_NAMES)


def test_lua_member_names():
    names = strings(array(read("lua-service"), r"RESERVED_MEMBER_NAMES = Set"))
    assert names == set(LuaLanguage.reserved_member_names)


def test_lua_conventions():
    members = strings(array(read("lua-connection"), r"Connection\.MEMBERS ="))
    assert set(conventions("lua-conventions")["service_escapes"]) == members


def test_python_conventions():
    naming = conventions("python-conventions")
    # A snake case service name is lower case, so of the keywords it meets only those
    keywords = {x for x in keyword.kwlist if x.islower()}
    assert set(naming["service_escapes"]) == PYTHON_CONNECTION_MEMBERS | keywords
    # A type name starts with an upper case letter, so of the keywords it meets only those
    keywords = {x for x in keyword.kwlist if x[0].isupper()}
    assert set(naming["type_escapes"]) == {"{service}Connection"} | keywords


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
