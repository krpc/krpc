"""Object requirements: object handles and their identity."""

import pytest
from driver import profile, section, values
from driver.errors import assert_argument_error, assert_native_error
from driver.helpers import create
from driver.testee import TesteeError

# Ids for objects the testee makes with the client's constructor
OBJECT_IDS = {
    "zero": 0,
    "small": 1,
    "past-32-bits": 2**32,
    "past-63-bits": 2**63,
    "max": 2**64 - 1,
}


section("Equality")


def test_equality(connected):
    """Two objects with the same id are equal. Objects with different ids are not, and
    an object is not equal to null."""
    alpha = create(connected)
    same = connected.invoke("TestService", "EchoTestObject", values.obj(alpha))
    beta = create(connected, "beta")
    assert alpha.handle != same.handle
    assert connected.compare(alpha, same)
    assert not connected.compare(alpha, beta)
    # The server keeps one object per value of a TestClass
    assert connected.compare(alpha, create(connected))
    assert not connected.compare(alpha, values.null())


def test_hash(connected):
    """Equal objects hash equal, in a language that hashes values."""
    alpha = create(connected)
    same = connected.invoke("TestService", "EchoTestObject", values.obj(alpha))
    assert connected.hash(alpha) == connected.hash(same)


@profile("ordering")
def test_sorting(connected):
    """Objects order by their ids, compared as unsigned integers. An object orders equal
    to itself and to another object with its id. Each of the language's comparison
    operators agrees with this order."""
    alpha = create(connected)
    same = connected.invoke("TestService", "EchoTestObject", values.obj(alpha))
    beta = create(connected, "beta")
    expected = (alpha.id > beta.id) - (alpha.id < beta.id)
    assert expected != 0
    assert connected.order(alpha, beta) == expected
    assert connected.order(beta, alpha) == -expected
    assert connected.order(alpha, alpha) == 0
    assert connected.order(alpha, same) == 0
    small = values.make_object("TestService", "TestClass", OBJECT_IDS["small"])
    large = values.make_object("TestService", "TestClass", OBJECT_IDS["past-63-bits"])
    assert connected.order(small, large) == -1
    assert connected.order(large, small) == 1


section("Object Ids")


def test_get_object_id(connected):
    """The client's object id helper returns the id of an object, and raises the
    argument error for null."""
    alpha = create(connected)
    assert connected.object_id(values.obj(alpha)) == alpha.id
    with pytest.raises(TesteeError) as info:
        connected.object_id(values.null())
    assert_argument_error(connected, info.value.error)


@pytest.mark.parametrize("case", list(OBJECT_IDS))
def test_object_id_range(connected, case):
    """An object holds any ``uint64`` id, including 0, which the server never issues."""
    obj = values.make_object("TestService", "TestClass", OBJECT_IDS[case])
    assert connected.object_id(obj) == OBJECT_IDS[case]


@pytest.mark.parametrize("position", ["target", "argument"])
@pytest.mark.parametrize("object_id", [0, 10**12], ids=["zero", "never-issued"])
def test_unknown_object_id(connected, control, position, object_id):
    """The client sends the id of an object as it is, without checking it. An id the
    server never issued, including 0, raises the error for ``KRPC.ArgumentException``, as the
    target of a call or as an argument."""
    obj = values.make_object("TestService", "TestClass", object_id)
    with pytest.raises(TesteeError) as info:
        if position == "target":
            connected.invoke("TestService", "TestClass_GetValue", obj)
        else:
            connected.invoke("TestService", "NotNullableObject", obj)
    assert_native_error(connected, control, info.value.error, "ArgumentException")


section("Connections")


def two_connections(connected, server, request):
    """Open a second connection, and return the client name of each connection."""
    connected.connect(server, name="second connection", connection="second")
    return {"": request.node.name, "second": "second connection"}


@profile("connections")
def test_method_connection(connected, server, request):
    """A method calls over the connection that returned its object, whichever connection
    runs the call."""
    names = two_connections(connected, server, request)
    for made in names:
        probe = connected.invoke("ConformanceService", "CreateProbe", connection=made)
        for x in names:
            assert (
                connected.invoke(
                    "ConformanceService",
                    "Probe_ClientName",
                    values.obj(probe),
                    connection=x,
                )
                == names[made]
            )


@profile("connections")
def test_static_method_connection(connected, server, request):
    """A static method calls over the connection it is called on."""
    names = two_connections(connected, server, request)
    for x, client_name in names.items():
        assert (
            connected.invoke(
                "ConformanceService", "Probe_static_StaticClientName", connection=x
            )
            == client_name
        )


@profile("connections")
def test_static_method_through_an_object_connection(connected, server, request):
    """In a language that calls a static method on an object, a static method called
    through an object calls over the connection that returned the object."""
    names = two_connections(connected, server, request)
    for made in names:
        probe = connected.invoke("ConformanceService", "CreateProbe", connection=made)
        for x in names:
            assert (
                connected.invoke(
                    "ConformanceService",
                    "Probe_static_StaticClientName",
                    connection=x,
                    through=probe,
                )
                == names[made]
            )


# Each place an object takes in a call
OBJECT_POSITIONS = [
    "argument",
    "method-argument",
    "static-method-argument",
    "property",
    "class-property",
    "list",
    "set",
    "dictionary",
    "tuple",
    "nested",
    "struct",
]


def object_ids(value) -> list[int]:
    """The server ids of the objects in a value, at any depth."""
    if isinstance(value, values.Object):
        return [value.id]
    if isinstance(value, values.Struct):
        value = value.fields
    if isinstance(value, dict):
        value = list(value.values())
    if isinstance(value, (list, tuple, set, frozenset)):
        return [x for item in value for x in object_ids(item)]
    return []


def pass_object(connected, obj, position, connection):
    """Pass an object to a call on a connection, at a position. Returns what the call
    returns, or the value of the property the call sets."""

    def call(procedure, *arguments):
        return connected.invoke(
            "TestService", procedure, *arguments, connection=connection
        )

    def create(value):
        return values.obj(call("CreateTestObject", values.string(value)))

    arg = values.obj(obj)
    beta = values.string("beta")
    if position == "argument":
        return call("EchoTestObject", arg)
    if position == "method-argument":
        return call("TestClass_EchoNullableObject", create("beta"), arg)
    if position == "static-method-argument":
        return call("TestClass_static_StaticNullableObject", arg)
    if position == "property":
        call("set_ObjectProperty", arg)
        return call("get_ObjectProperty")
    if position == "class-property":
        target = create("beta")
        call("TestClass_set_ObjectProperty", target, arg)
        return call("TestClass_get_ObjectProperty", target)
    if position == "list":
        return call("AddToObjectList", values.list_of([arg]), beta)
    if position == "set":
        return call("AddToObjectSet", values.set_of([arg]), beta)
    if position == "dictionary":
        argument = values.dictionary([(values.string("alpha"), arg)])
        return call("AddToObjectDictionary", argument, beta)
    if position == "tuple":
        return call("SwapObjectTuple", values.tuple_of(create("beta"), arg))
    if position == "nested":
        argument = values.list_of([values.list_of([arg])])
        return call("EchoNestedListOfNullableObjects", argument)
    null = values.null()
    argument = values.struct(
        "TestService", "TestNullableStruct", values.sint32(42), null, null, null, arg
    )
    return call("NullableStructEcho", argument)


@profile("connections")
@pytest.mark.parametrize("made", ["default", "second"])
@pytest.mark.parametrize("position", OBJECT_POSITIONS)
def test_object_from_another_connection(connected, server, made, position):
    """An object passes to a call on another connection to the same server: as an
    argument of a procedure, a method or a static method, as the value of a property, and
    inside a collection or a structure. The call gets the object with its id."""
    connected.connect(server, name="second connection", connection="second")
    made, used = ("", "second") if made == "default" else ("second", "")
    alpha = connected.invoke(
        "TestService", "CreateTestObject", values.string("alpha"), connection=made
    )
    assert alpha.id in object_ids(pass_object(connected, alpha, position, used))


def from_both_connections(connected, server):
    """An object from the default connection, and the same object from a second one."""
    connected.connect(server, name="second connection", connection="second")
    alpha = create(connected)
    same = connected.invoke(
        "TestService", "EchoTestObject", values.obj(alpha), connection="second"
    )
    assert alpha.handle != same.handle
    return alpha, same


@profile("connections")
def test_equality_across_connections(connected, server):
    """Objects with the same id are equal, whichever connections returned them."""
    alpha, same = from_both_connections(connected, server)
    assert connected.compare(alpha, same)
    assert connected.compare(same, alpha)


@profile("connections")
def test_hash_across_connections(connected, server):
    """Objects with the same id hash equal, whichever connections returned them, in a
    language that hashes values."""
    alpha, same = from_both_connections(connected, server)
    assert connected.hash(alpha) == connected.hash(same)
