import unittest
from enum import Enum
from krpc.types import (
    Types,
    ValueType,
    ClassType,
    EnumerationType,
    MessageType,
    ClassBase,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
    StaticMethod,
)
from krpc.schema.KRPC_pb2 import Type, ProcedureCall, Stream, Status, Services


class TestTypes(unittest.TestCase):
    def check_protobuf_type(
        self,
        code: Type.TypeCode,
        service: str,
        name: str,
        numtypes: int,
        protobuf_type: Type,
    ) -> None:
        self.assertEqual(code, protobuf_type.code)
        self.assertEqual(service, protobuf_type.service)
        self.assertEqual(name, protobuf_type.name)
        self.assertEqual(numtypes, len(protobuf_type.types))

    def test_none_type(self) -> None:
        types = Types()
        none_type = Type()
        none_type.code = Type.NONE
        self.assertRaises(ValueError, types.as_type, none_type)

    def test_value_types(self) -> None:
        types = Types()
        cases = [
            (types.double_type, Type.DOUBLE, float),
            (types.float_type, Type.FLOAT, float),
            (types.sint32_type, Type.SINT32, int),
            (types.sint64_type, Type.SINT64, int),
            (types.uint32_type, Type.UINT32, int),
            (types.uint64_type, Type.UINT64, int),
            (types.bool_type, Type.BOOL, bool),
            (types.string_type, Type.STRING, str),
            (types.bytes_type, Type.BYTES, bytes),
        ]
        for typ, protobuf_code, python_type in cases:
            self.assertTrue(isinstance(typ, ValueType))
            self.check_protobuf_type(protobuf_code, "", "", 0, typ.protobuf_type)
            self.assertEqual(python_type, typ.python_type)

    def test_class_types(self) -> None:
        types = Types()
        typ = types.class_type("ServiceName", "ClassName", "class documentation")
        self.assertTrue(isinstance(typ, ClassType))
        self.assertTrue(issubclass(typ.python_type, ClassBase))
        self.assertEqual("class documentation", typ.python_type.__doc__)
        self.check_protobuf_type(
            Type.CLASS, "ServiceName", "ClassName", 0, typ.protobuf_type
        )
        instance = typ.python_type(None, 42)
        self.assertEqual(42, instance._object_id)
        self.assertEqual("ServiceName", instance._service_name)
        self.assertEqual("ClassName", instance._class_name)
        typ2 = types.as_type(typ.protobuf_type)
        self.assertEqual(typ, typ2)

    def test_enumeration_types(self) -> None:
        types = Types()
        typ = types.enumeration_type("ServiceName", "EnumName", "enum documentation")
        self.assertTrue(isinstance(typ, EnumerationType))
        self.assertIsNone(typ.python_type)
        self.check_protobuf_type(
            Type.ENUMERATION, "ServiceName", "EnumName", 0, typ.protobuf_type
        )
        typ.set_values(
            {
                "a": {"value": 0, "doc": "doca"},
                "b": {"value": 42, "doc": "docb"},
                "c": {"value": 100, "doc": "docc"},
            }
        )
        self.assertTrue(issubclass(typ.python_type, Enum))
        self.assertEqual("enum documentation", typ.python_type.__doc__)
        self.assertEqual(0, typ.python_type.a.value)  # type: ignore[attr-defined]
        self.assertEqual(42, typ.python_type.b.value)  # type: ignore[attr-defined]
        self.assertEqual(100, typ.python_type.c.value)  # type: ignore[attr-defined]
        self.assertEqual("doca", typ.python_type.a.__doc__)  # type: ignore[attr-defined]
        self.assertEqual("docb", typ.python_type.b.__doc__)  # type: ignore[attr-defined]
        self.assertEqual("docc", typ.python_type.c.__doc__)  # type: ignore[attr-defined]
        typ2 = types.as_type(typ.protobuf_type)
        self.assertEqual(typ, typ2)

    def test_message_types(self) -> None:
        types = Types()
        cases = [
            (types.procedure_call_type, Type.PROCEDURE_CALL, ProcedureCall),
            (types.stream_type, Type.STREAM, Stream),
            (types.status_type, Type.STATUS, Status),
            (types.services_type, Type.SERVICES, Services),
        ]
        for typ, protobuf_code, python_type in cases:
            self.assertTrue(isinstance(typ, MessageType))
            self.assertEqual(python_type, typ.python_type)
            self.check_protobuf_type(protobuf_code, "", "", 0, typ.protobuf_type)

    def test_tuple_1_types(self) -> None:
        types = Types()
        typ = types.tuple_type(types.bool_type)
        self.assertTrue(isinstance(typ, TupleType))
        self.assertEqual(typ.python_type, tuple)
        self.check_protobuf_type(Type.TUPLE, "", "", 1, typ.protobuf_type)
        self.check_protobuf_type(Type.BOOL, "", "", 0, typ.protobuf_type.types[0])
        self.assertEqual(1, len(typ.value_types))
        self.assertTrue(isinstance(typ.value_types[0], ValueType))
        self.assertEqual(bool, typ.value_types[0].python_type)
        self.check_protobuf_type(Type.BOOL, "", "", 0, typ.value_types[0].protobuf_type)

    def test_tuple_2_types(self) -> None:
        types = Types()
        typ = types.tuple_type(types.uint32_type, types.string_type)
        self.assertTrue(isinstance(typ, TupleType))
        self.assertEqual(typ.python_type, tuple)
        self.check_protobuf_type(Type.TUPLE, "", "", 2, typ.protobuf_type)
        self.check_protobuf_type(Type.UINT32, "", "", 0, typ.protobuf_type.types[0])
        self.check_protobuf_type(Type.STRING, "", "", 0, typ.protobuf_type.types[1])
        self.assertEqual(2, len(typ.value_types))
        self.assertTrue(isinstance(typ.value_types[0], ValueType))
        self.assertTrue(isinstance(typ.value_types[1], ValueType))
        self.assertEqual(int, typ.value_types[0].python_type)
        self.assertEqual(str, typ.value_types[1].python_type)
        self.check_protobuf_type(
            Type.UINT32, "", "", 0, typ.value_types[0].protobuf_type
        )
        self.check_protobuf_type(
            Type.STRING, "", "", 0, typ.value_types[1].protobuf_type
        )

    def test_tuple_3_types(self) -> None:
        types = Types()
        typ = types.tuple_type(types.float_type, types.uint64_type, types.string_type)
        self.assertTrue(isinstance(typ, TupleType))
        self.assertEqual(typ.python_type, tuple)
        self.check_protobuf_type(Type.TUPLE, "", "", 3, typ.protobuf_type)
        self.check_protobuf_type(Type.FLOAT, "", "", 0, typ.protobuf_type.types[0])
        self.check_protobuf_type(Type.UINT64, "", "", 0, typ.protobuf_type.types[1])
        self.check_protobuf_type(Type.STRING, "", "", 0, typ.protobuf_type.types[2])
        self.assertEqual(3, len(typ.value_types))
        self.assertTrue(isinstance(typ.value_types[0], ValueType))
        self.assertTrue(isinstance(typ.value_types[1], ValueType))
        self.assertTrue(isinstance(typ.value_types[2], ValueType))
        self.assertEqual(float, typ.value_types[0].python_type)
        self.assertEqual(int, typ.value_types[1].python_type)
        self.assertEqual(str, typ.value_types[2].python_type)
        self.check_protobuf_type(
            Type.FLOAT, "", "", 0, typ.value_types[0].protobuf_type
        )
        self.check_protobuf_type(
            Type.UINT64, "", "", 0, typ.value_types[1].protobuf_type
        )
        self.check_protobuf_type(
            Type.STRING, "", "", 0, typ.value_types[2].protobuf_type
        )

    def test_list_types(self) -> None:
        types = Types()
        typ = types.list_type(types.uint32_type)
        self.assertTrue(isinstance(typ, ListType))
        self.assertEqual(typ.python_type, list)
        self.check_protobuf_type(Type.LIST, "", "", 1, typ.protobuf_type)
        self.check_protobuf_type(Type.UINT32, "", "", 0, typ.protobuf_type.types[0])
        self.assertTrue(isinstance(typ.value_type, ValueType))
        self.assertEqual(int, typ.value_type.python_type)
        self.check_protobuf_type(Type.UINT32, "", "", 0, typ.value_type.protobuf_type)

    def test_set_types(self) -> None:
        types = Types()
        typ = types.set_type(types.string_type)
        self.assertTrue(isinstance(typ, SetType))
        self.assertEqual(typ.python_type, set)
        self.check_protobuf_type(Type.SET, "", "", 1, typ.protobuf_type)
        self.check_protobuf_type(Type.STRING, "", "", 0, typ.protobuf_type.types[0])
        self.assertTrue(isinstance(typ.value_type, ValueType))
        self.assertEqual(str, typ.value_type.python_type)
        self.check_protobuf_type(Type.STRING, "", "", 0, typ.value_type.protobuf_type)

    def test_dictionary_types(self) -> None:
        types = Types()
        typ = types.dictionary_type(types.string_type, types.uint32_type)
        self.assertTrue(isinstance(typ, DictionaryType))
        self.assertEqual(typ.python_type, dict)
        self.check_protobuf_type(Type.DICTIONARY, "", "", 2, typ.protobuf_type)
        self.check_protobuf_type(Type.STRING, "", "", 0, typ.protobuf_type.types[0])
        self.check_protobuf_type(Type.UINT32, "", "", 0, typ.protobuf_type.types[1])
        self.assertTrue(isinstance(typ.key_type, ValueType))
        self.assertEqual(str, typ.key_type.python_type)
        self.check_protobuf_type(Type.STRING, "", "", 0, typ.key_type.protobuf_type)
        self.assertTrue(isinstance(typ.value_type, ValueType))
        self.assertEqual(int, typ.value_type.python_type)
        self.check_protobuf_type(Type.UINT32, "", "", 0, typ.value_type.protobuf_type)

    def test_coerce_to(self) -> None:
        types = Types()
        cases = [
            (42.0, 42, types.double_type),
            (42.0, 42, types.float_type),
            (42, 42.0, types.sint32_type),
            (42, 42, types.sint32_type),
            (42, 42.0, types.sint64_type),
            (42, 42, types.sint64_type),
            (42, 42.0, types.uint32_type),
            (42, 42, types.uint32_type),
            (42, 42.0, types.uint64_type),
            (42, 42, types.uint64_type),
            ([], tuple(), types.list_type(types.string_type)),
            (
                (0, 1, 2),
                [0, 1, 2],
                types.tuple_type(
                    types.sint32_type, types.sint32_type, types.sint32_type
                ),
            ),
            ([0, 1, 2], (0, 1, 2), types.list_type(types.sint32_type)),
            (["foo", "bar"], ["foo", "bar"], types.list_type(types.string_type)),
        ]
        for expected, value, typ in cases:
            coerced_value = types.coerce_to(value, typ)
            self.assertEqual(expected, coerced_value)
            self.assertEqual(type(expected), type(coerced_value))

        strings = ["foo", "\xe2\x84\xa2", "Mystery Goo\xe2\x84\xa2 Containment Unit"]
        for string in strings:
            self.assertEqual(string, types.coerce_to(string, types.string_type))

        self.assertRaises(ValueError, types.coerce_to, None, types.float_type)
        self.assertRaises(ValueError, types.coerce_to, "", types.float_type)
        self.assertRaises(ValueError, types.coerce_to, True, types.float_type)

        self.assertRaises(
            ValueError, types.coerce_to, [], types.tuple_type(types.uint32_type)
        )
        self.assertRaises(
            ValueError, types.coerce_to, ["foo", 2], types.tuple_type(types.string_type)
        )
        self.assertRaises(
            ValueError, types.coerce_to, [1], types.tuple_type(types.string_type)
        )


class _MyClass(ClassBase):
    @StaticMethod
    def my_static(cls, x: int) -> int:  # type: ignore[misc]  # pylint: disable=no-self-argument
        return cls._client(x)  # type: ignore[attr-defined]


class TestStaticMethod(unittest.TestCase):
    def setUp(self) -> None:
        if hasattr(_MyClass, "_client"):
            del _MyClass._client  # type: ignore[attr-defined]

    def _make_instance(self, client_fn: object) -> ClassBase:
        instance = object.__new__(_MyClass)
        instance._client = client_fn  # type: ignore[attr-defined]
        instance._object_id = 1
        return instance

    def test_call_via_class_with_client_set(self) -> None:
        _MyClass._client = lambda x: x * 2  # type: ignore[attr-defined]
        self.assertEqual(6, _MyClass.my_static(3))  # type: ignore[call-arg]  # pylint: disable=no-value-for-parameter

    def test_call_via_instance_without_class_client(self) -> None:
        instance = self._make_instance(lambda x: x + 10)
        self.assertEqual(15, instance.my_static(5))  # type: ignore[call-arg]

    def test_instance_client_injected_onto_class(self) -> None:
        def identity(x):  # type: ignore[no-untyped-def]
            return x

        instance = self._make_instance(identity)
        instance.my_static(0)  # type: ignore[call-arg]
        self.assertIs(identity, _MyClass._client)  # type: ignore[attr-defined]

    def test_bound_has_dunder_self_and_name(self) -> None:
        _MyClass._client = lambda x: x  # type: ignore[attr-defined]
        bound = _MyClass.my_static
        self.assertIs(_MyClass, bound.__self__)
        self.assertEqual("my_static", bound.__name__)

    def test_bound_via_instance_has_dunder_self(self) -> None:
        instance = self._make_instance(lambda x: x)
        bound = instance.my_static
        self.assertIs(_MyClass, bound.__self__)


if __name__ == "__main__":
    unittest.main()
