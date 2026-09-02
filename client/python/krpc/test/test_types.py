import unittest
from enum import Enum
from krpc.types import (
    Types,
    ValueType,
    ClassType,
    EnumerationType,
    MessageType,
    ClassBase,
    StructType,
    TupleType,
    ListType,
    SetType,
    DictionaryType,
    StaticMethod,
    WrappedClass,
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

    def test_struct_types(self) -> None:
        types = Types()
        typ = types.struct_type("ServiceName", "StructName", "struct documentation")
        self.assertTrue(isinstance(typ, StructType))
        self.assertIsNone(typ.python_type)
        self.assertFalse(typ.has_fields)
        self.check_protobuf_type(
            Type.STRUCT, "ServiceName", "StructName", 0, typ.protobuf_type
        )
        typ.set_fields([("count", types.uint32_type), ("name", types.string_type)])
        self.assertTrue(typ.has_fields)
        self.assertEqual(["count", "name"], typ.field_names)
        self.assertEqual([types.uint32_type, types.string_type], list(typ.field_types))
        self.assertEqual("struct documentation", typ.python_type.__doc__)
        value = typ.python_type(count=42, name="jeb")
        self.assertEqual(42, value.count)
        self.assertEqual("jeb", value.name)
        self.assertEqual((42, "jeb"), tuple(value))
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

    def test_nullable_types(self) -> None:
        types = Types()
        typ = types.nullable(types.uint32_type)
        self.assertTrue(isinstance(typ, ValueType))
        self.assertTrue(typ.nullable)
        self.assertFalse(types.uint32_type.nullable)
        self.assertTrue(typ.protobuf_type.nullable)
        self.assertEqual("<type: uint32?>", str(typ))
        # The nullable form of a type is the same type in every position that names it
        self.assertIs(typ, types.nullable(typ))
        self.assertIs(typ, types.nullable(types.uint32_type))
        self.assertIs(typ, types.as_type(typ.protobuf_type))

    def test_nullable_type_shares_its_python_type(self) -> None:
        types = Types()
        # Nullability belongs to the position a value sits in, so a class has one python
        # type however the position that holds it is declared
        cls = types.class_type("ServiceName", "ClassName")
        self.assertIs(cls.python_type, types.nullable(cls).python_type)
        enumeration = types.enumeration_type("ServiceName", "EnumName")
        nullable_enumeration = types.nullable(enumeration)
        enumeration.set_values({"a": {"value": 0, "doc": ""}})
        self.assertIs(enumeration.python_type, nullable_enumeration.python_type)
        struct = types.struct_type("ServiceName", "StructName")
        nullable_struct = types.nullable(struct)
        struct.set_fields([("count", types.uint32_type)])
        self.assertIs(struct.python_type, nullable_struct.python_type)
        self.assertEqual(struct.field_types, nullable_struct.field_types)

    def test_nullable_list_types(self) -> None:
        types = Types()
        typ = types.list_type(types.nullable(types.uint32_type))
        self.assertTrue(isinstance(typ, ListType))
        self.assertTrue(typ.value_type.nullable)
        self.check_protobuf_type(Type.LIST, "", "", 1, typ.protobuf_type)
        self.assertTrue(typ.protobuf_type.types[0].nullable)
        self.assertEqual("<type: List(uint32?)>", str(typ))
        # A nullable element makes a list type of its own
        self.assertIsNot(types.list_type(types.uint32_type), typ)

    def test_nullable_dictionary_types(self) -> None:
        types = Types()
        typ = types.dictionary_type(
            types.string_type, types.nullable(types.uint32_type)
        )
        self.assertTrue(isinstance(typ, DictionaryType))
        self.assertFalse(typ.key_type.nullable)
        self.assertTrue(typ.value_type.nullable)
        self.check_protobuf_type(Type.DICTIONARY, "", "", 2, typ.protobuf_type)
        self.assertFalse(typ.protobuf_type.types[0].nullable)
        self.assertTrue(typ.protobuf_type.types[1].nullable)
        self.assertEqual("<type: Dict(string,uint32?)>", str(typ))
        self.assertIsNot(
            types.dictionary_type(types.string_type, types.uint32_type), typ
        )

    def test_nullable_tuple_types(self) -> None:
        types = Types()
        typ = types.tuple_type(types.sint32_type, types.nullable(types.string_type))
        self.assertTrue(isinstance(typ, TupleType))
        self.assertEqual([False, True], [t.nullable for t in typ.value_types])
        self.check_protobuf_type(Type.TUPLE, "", "", 2, typ.protobuf_type)
        self.assertFalse(typ.protobuf_type.types[0].nullable)
        self.assertTrue(typ.protobuf_type.types[1].nullable)
        self.assertEqual("<type: Tuple(sint32,string?)>", str(typ))
        self.assertIsNot(types.tuple_type(types.sint32_type, types.string_type), typ)

    def test_nullable_set_types(self) -> None:
        types = Types()
        typ = types.set_type(types.nullable(types.uint32_type))
        self.assertTrue(typ.value_type.nullable)
        self.assertEqual("<type: Set(uint32?)>", str(typ))

    def test_struct_with_nullable_fields(self) -> None:
        types = Types()
        typ = types.struct_type("ServiceName", "NullableStructName")
        typ.set_fields(
            [("count", types.uint32_type), ("name", types.nullable(types.string_type))]
        )
        self.assertEqual([False, True], [t.nullable for t in typ.field_types])
        value = typ.python_type(count=42, name=None)
        self.assertEqual(42, value.count)
        self.assertIsNone(value.name)

    def test_struct_comparison(self) -> None:
        types = Types()
        typ = types.struct_type("ServiceName", "ComparableStruct")
        typ.set_fields([("count", types.uint32_type), ("name", types.string_type)])
        one = typ.python_type(1, "jeb")
        same = typ.python_type(1, "jeb")
        two = typ.python_type(2, "jeb")
        self.assertEqual(one, same)
        self.assertNotEqual(one, two)
        # A structure is ordered and hashed as the tuple of its field values is, and equals
        # that tuple
        self.assertEqual((1, "jeb"), one)
        self.assertLess(one, two)
        self.assertGreater(two, one)
        self.assertEqual([one, two], sorted([two, one]))
        self.assertEqual(hash(one), hash(same))
        self.assertEqual({one, two}, {one, same, two})

    def test_struct_holding_an_unhashable_value(self) -> None:
        types = Types()
        typ = types.struct_type("ServiceName", "UnhashableStruct")
        typ.set_fields([("items", types.list_type(types.sint32_type))])
        value = typ.python_type([1, 2])
        self.assertEqual(typ.python_type([1, 2]), value)
        # A list cannot be hashed, so neither can a structure holding one, exactly as a
        # tuple holding one cannot
        self.assertRaises(TypeError, hash, value)
        self.assertRaises(TypeError, hash, ([1, 2],))

    def test_coerce_to_struct(self) -> None:
        types = Types()
        typ = types.struct_type("ServiceName", "StructName")
        typ.set_fields([("count", types.uint32_type), ("name", types.string_type)])
        expected = typ.python_type(count=42, name="jeb")
        for value in ((42, "jeb"), [42, "jeb"]):
            coerced_value = types.coerce_to(value, typ)
            self.assertEqual(expected, coerced_value)
            self.assertEqual(typ.python_type, type(coerced_value))
        # A value with the wrong number of fields is not a value of the structure
        self.assertRaises(ValueError, types.coerce_to, (42,), typ)
        self.assertRaises(ValueError, types.coerce_to, (42, "jeb", 1), typ)

    def test_coerce_to_struct_with_a_nullable_field(self) -> None:
        types = Types()
        typ = types.struct_type("ServiceName", "CoercedNullableStruct")
        typ.set_fields(
            [("count", types.uint32_type), ("name", types.nullable(types.string_type))]
        )
        expected = typ.python_type(count=42, name=None)
        for value in ((42, None), [42, None]):
            self.assertEqual(expected, types.coerce_to(value, typ))
        # A null in a field that cannot hold one is not a value of the structure
        self.assertRaises(ValueError, types.coerce_to, (None, "jeb"), typ)

    def test_coerce_to_collection_holding_a_null(self) -> None:
        types = Types()
        list_type = types.list_type(types.nullable(types.sint32_type))
        self.assertEqual([1, None, 3], types.coerce_to((1, None, 3), list_type))
        tuple_type = types.tuple_type(
            types.sint32_type, types.nullable(types.string_type)
        )
        self.assertEqual((1, None), types.coerce_to([1, None], tuple_type))
        # A null in a position that cannot hold one is not a value of the collection
        self.assertRaises(
            ValueError, types.coerce_to, (1, None), types.list_type(types.sint32_type)
        )
        # A class type is no different, though a class value carries a null of its own
        self.assertRaises(
            ValueError,
            types.coerce_to,
            (None,),
            types.list_type(types.class_type("ServiceName", "ClassName")),
        )

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


class TestWrappedClass(unittest.TestCase):
    def setUp(self) -> None:
        if hasattr(_MyClass, "_client"):
            del _MyClass._client  # type: ignore[attr-defined]

    def test_static_methods_use_the_wrapping_client(self) -> None:
        wrapped1 = WrappedClass(lambda x: x * 2, _MyClass)  # type: ignore[arg-type]
        wrapped2 = WrappedClass(lambda x: x + 100, _MyClass)  # type: ignore[arg-type]
        # Retrieve both static methods before calling either: a single _client slot shared on the
        # class would let the second client's value leak into the first client's call.
        method1 = wrapped1.my_static
        method2 = wrapped2.my_static
        self.assertEqual(6, method1(3))
        self.assertEqual(103, method2(3))
        # The shared base class is never mutated.
        self.assertFalse(hasattr(_MyClass, "_client"))

    def test_construction_through_wrapper(self) -> None:
        wrapped = WrappedClass(lambda x: x, _MyClass)  # type: ignore[arg-type]
        instance = wrapped(None, 1)
        self.assertIsInstance(instance, _MyClass)


if __name__ == "__main__":
    unittest.main()
