"""Helpers the tests share: the names of a test's server state, the streams and objects of
TestService and ConformanceService they use, and checks on what a testee raises."""

from __future__ import annotations

from typing import Any

import pytest

from driver import values
from driver.testee import Callback, Client, Ending, Stream, TesteeError


def name(request: pytest.FixtureRequest, suffix: str = "") -> str:
    """A name for the test's ConformanceService state, unique to the test."""
    return request.node.nodeid + suffix


def raised(func: Any) -> Any:
    """The error a call raises."""
    with pytest.raises(TesteeError) as info:
        func()
    return info.value.error


def ended(stream: Stream) -> bool:
    """Whether reading the stream raises, as it does once the stream has ended."""
    try:
        stream.get()
    except TesteeError:
        return True
    return False


def watched(client: Client, value_name: str) -> Stream:
    """A stream of the named ConformanceService value."""
    return client.add_stream("ConformanceService", "Value", values.string(value_name))


def counter(client: Client, counter_name: str) -> Stream:
    """A stream whose value goes up by one on every update."""
    return client.add_stream(
        "TestService", "Counter", values.string(counter_name), values.sint32(1)
    )


def round_trip(client: Client) -> None:
    """A call, whose response follows every update the server sent before it."""
    client.invoke("TestService", "Int32ToString", values.sint32(1))


def create(client: Client, value: str = "alpha") -> Any:
    """A new TestService object holding the value."""
    return client.invoke("TestService", "CreateTestObject", values.string(value))


def single_ending(callback: Callback) -> Ending:
    """The one call of an end callback."""
    (ending,) = callback.wait_for_calls(1)
    assert isinstance(ending, Ending)
    return ending


def enum_value(value: int) -> Any:
    """A TestService.TestEnum value."""
    return values.enum("TestService", "TestEnum", value)


def struct_value(
    int_field: int, string_field: str, enum_field: int, list_field: list[int]
) -> Any:
    """A TestService.TestStruct value."""
    return values.struct(
        "TestService",
        "TestStruct",
        values.sint32(int_field),
        values.string(string_field),
        enum_value(enum_field),
        values.list_of([values.sint32(x) for x in list_field]),
    )


def ids(value: Any) -> Any:
    """The server ids of the objects in a value, with the testee's handles dropped."""
    if isinstance(value, values.Object):
        return value.id
    if isinstance(value, list):
        return [ids(x) for x in value]
    if isinstance(value, tuple):
        return tuple(ids(x) for x in value)
    if isinstance(value, dict):
        return {k: ids(v) for k, v in value.items()}
    if isinstance(value, values.Struct):
        return values.Struct(tuple(ids(x) for x in value.fields))
    return value
