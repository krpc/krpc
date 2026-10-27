import unittest

from krpc.schema.KRPC_pb2 import Type
from krpc.types import (
    ClassBase,
    StaticMethod,
    StructType,
    Types,
    WrappedClass,
)


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
        value = typ.python_type(count=42, name="alpha")
        self.assertEqual(42, value.count)
        self.assertEqual("alpha", value.name)
        self.assertEqual((42, "alpha"), tuple(value))
        typ2 = types.as_type(typ.protobuf_type)
        self.assertEqual(typ, typ2)

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
        assert isinstance(nullable_struct, StructType)
        struct.set_fields([("count", types.uint32_type)])
        self.assertIs(struct.python_type, nullable_struct.python_type)
        self.assertEqual(struct.field_types, nullable_struct.field_types)

    def test_struct_comparison(self) -> None:
        types = Types()
        typ = types.struct_type("ServiceName", "ComparableStruct")
        typ.set_fields([("count", types.uint32_type), ("name", types.string_type)])
        one = typ.python_type(1, "alpha")
        same = typ.python_type(1, "alpha")
        two = typ.python_type(2, "alpha")
        self.assertEqual(one, same)
        self.assertNotEqual(one, two)
        # A structure is ordered and hashed as the tuple of its field values is, and equals
        # that tuple
        self.assertEqual((1, "alpha"), one)
        self.assertLess(one, two)
        self.assertGreater(two, one)
        self.assertEqual([one, two], sorted([two, one]))
        self.assertEqual(hash(one), hash(same))
        self.assertEqual({one, two}, {one, same, two})

    def test_coerce_to_struct(self) -> None:
        types = Types()
        typ = types.struct_type("ServiceName", "StructName")
        typ.set_fields([("count", types.uint32_type), ("name", types.string_type)])
        expected = typ.python_type(count=42, name="alpha")
        for value in ((42, "alpha"), [42, "alpha"]):
            coerced_value = types.coerce_to(value, typ)
            self.assertEqual(expected, coerced_value)
            self.assertEqual(typ.python_type, type(coerced_value))
        # A value with the wrong number of fields is not a value of the structure
        self.assertRaises(ValueError, types.coerce_to, (42,), typ)
        self.assertRaises(ValueError, types.coerce_to, (42, "alpha", 1), typ)

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
        self.assertRaises(ValueError, types.coerce_to, (None, "alpha"), typ)

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
    def my_static(cls, x: int) -> int:
        return cls._client(x)  # pyright: ignore[reportCallIssue]


class TestStaticMethod(unittest.TestCase):
    def setUp(self) -> None:
        if hasattr(_MyClass, "_client"):
            del _MyClass._client  # pyright: ignore[reportAttributeAccessIssue]

    def _make_instance(self, client_fn: object) -> _MyClass:
        instance = object.__new__(_MyClass)
        instance._client = client_fn  # pyright: ignore[reportAttributeAccessIssue]
        instance._object_id = 1
        return instance

    def test_bound_has_dunder_self_and_name(self) -> None:
        _MyClass._client = lambda x: x  # pyright: ignore[reportAttributeAccessIssue]
        bound = _MyClass.my_static
        self.assertIs(_MyClass, bound.__self__)  # pyright: ignore[reportFunctionMemberAccess]
        self.assertEqual("my_static", bound.__name__)

    def test_bound_via_instance_has_dunder_self(self) -> None:
        instance = self._make_instance(lambda x: x)
        bound = instance.my_static
        self.assertIs(_MyClass, bound.__self__)  # pyright: ignore[reportFunctionMemberAccess]


class TestWrappedClass(unittest.TestCase):
    def setUp(self) -> None:
        if hasattr(_MyClass, "_client"):
            del _MyClass._client  # pyright: ignore[reportAttributeAccessIssue]

    def test_static_methods_use_the_wrapping_client(self) -> None:
        wrapped1 = WrappedClass(lambda x: x * 2, _MyClass)  # pyright: ignore[reportArgumentType]
        wrapped2 = WrappedClass(lambda x: x + 100, _MyClass)  # pyright: ignore[reportArgumentType]
        # Retrieve both static methods before calling either: a single _client slot shared on the
        # class would let the second client's value leak into the first client's call.
        method1 = wrapped1.my_static
        method2 = wrapped2.my_static
        self.assertEqual(6, method1(3))
        self.assertEqual(103, method2(3))
        # The shared base class is never mutated.
        self.assertFalse(hasattr(_MyClass, "_client"))

    def test_construction_through_wrapper(self) -> None:
        wrapped = WrappedClass(lambda x: x, _MyClass)  # pyright: ignore[reportArgumentType]
        instance = wrapped(None, 1)
        self.assertIsInstance(instance, _MyClass)


if __name__ == "__main__":
    unittest.main()
