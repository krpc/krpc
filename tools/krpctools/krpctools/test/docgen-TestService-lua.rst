.. default-domain:: lua
.. highlight:: lua

.. currentmodule:: TestService


.. module:: TestService

Service documentation string.


.. staticmethod:: add_multiple_values(x: number, y: number, z: number): string







.. staticmethod:: add_to_object_dictionary(d: Map, key: string): Map







.. staticmethod:: add_to_object_list(l: List, value: string): List







.. staticmethod:: add_to_object_set(h: Set, value: string): Set







.. staticmethod:: blocking_procedure(n: number, sum?: number): number



   :param sum: Defaults to ``0``.




.. staticmethod:: bool_to_string(value: boolean): string







.. staticmethod:: bytes_default(x?: string): string



   :param x: Defaults to ``'\202\254'``.




.. staticmethod:: bytes_to_hex_string(value: string): string







.. staticmethod:: counter(id?: string, divisor?: number): number



   :param id: Defaults to ``''``.
   :param divisor: Defaults to ``1``.




.. staticmethod:: counter_struct(id?: string): TestService.TestStruct

   Returns a struct whose IntField counts the number of times it has been called,
   so that a stream on it changes on every update.

   :param id: Defaults to ``''``.




.. staticmethod:: create_test_object(value: string): TestService.TestClass







.. staticmethod:: deprecated_procedure(value: number): string

   .. warning:: Deprecated. Use :meth:`TestService.float_to_string` instead.

   Deprecated procedure documentation string.





.. staticmethod:: deprecated_procedure_no_message(value: number): string

   .. warning:: Deprecated.

   Deprecated procedure with no reason documentation string.





.. attribute:: deprecated_property: string

   .. warning:: Deprecated. Use :attr:`TestService.string_property` instead.

   Deprecated property documentation string.

   :Attribute: Can be read or written




.. staticmethod:: dictionary_default(x?: Map): Map



   :param x: Defaults to ``{[1] = false, [2] = true}``.




.. staticmethod:: double_special_defaults(nan?: number, infinity?: number, negative_infinity?: number, maximum?: number, lowest?: number): List

   Procedure whose defaults are the special values of double.

   :param nan: Defaults to ``0/0``.
   :param infinity: Defaults to ``math.huge``.
   :param negative_infinity: Defaults to ``-math.huge``.
   :param maximum: Defaults to ``krpc.limits.DOUBLE_MAX``.
   :param lowest: Defaults to ``krpc.limits.DOUBLE_LOWEST``.




.. staticmethod:: double_to_string(value: number): string







.. staticmethod:: dump_expression_tree(expression: KRPC.Expression): string

   The expression tree of a function, as text.





.. staticmethod:: echo_dictionary_of_nullable_objects(d: Map): Map







.. staticmethod:: echo_list_of_nullable_ints(l: List): List







.. staticmethod:: echo_list_of_nullable_objects(l: List): List







.. staticmethod:: echo_nested_list_of_nullable_objects(l: List): List







.. staticmethod:: echo_nested_nullable_struct(value: TestService.TestNestedNullableStruct): TestService.TestNestedNullableStruct







.. staticmethod:: echo_nullable_bool(value: boolean | Types.none): boolean | nil







.. staticmethod:: echo_nullable_bytes(value: string | Types.none): string | nil







.. staticmethod:: echo_nullable_dictionary(d: Map | Types.none): Map | nil







.. staticmethod:: echo_nullable_double(value: number | Types.none): number | nil







.. staticmethod:: echo_nullable_enum(x: TestService.TestEnum | Types.none): TestService.TestEnum | nil







.. staticmethod:: echo_nullable_flags(x: TestService.TestFlags | Types.none): TestService.TestFlags | nil







.. staticmethod:: echo_nullable_float(value: number | Types.none): number | nil







.. staticmethod:: echo_nullable_int(value: number | Types.none): number | nil

   Returns the given value.

   :param value: The value, or ``nil``.
   :returns: The value.




.. staticmethod:: echo_nullable_list(l: List | Types.none): List | nil







.. staticmethod:: echo_nullable_long(value: number | Types.none): number | nil







.. staticmethod:: echo_nullable_set(h: Set | Types.none): Set | nil







.. staticmethod:: echo_nullable_string(value: string | Types.none): string | nil







.. staticmethod:: echo_nullable_tuple(t: Tuple | Types.none): Tuple | nil







.. staticmethod:: echo_nullable_u_int(value: number | Types.none): number | nil







.. staticmethod:: echo_nullable_u_long(value: number | Types.none): number | nil







.. staticmethod:: echo_string_set(h: Set): Set







.. staticmethod:: echo_test_object(value: TestService.TestClass | Types.none): TestService.TestClass | nil







.. staticmethod:: echo_tuple_with_a_nullable_object(t: Tuple): Tuple







.. staticmethod:: empty_list_default(x?: List): List



   :param x: Defaults to ``{}``.




.. staticmethod:: enum_default(x?: TestService.TestEnum): TestService.TestEnum



   :param x: Defaults to ``TestService.TestEnum.value_c``.




.. staticmethod:: enum_echo(x: TestService.TestEnum): TestService.TestEnum







.. staticmethod:: enum_list_default(x?: List): List



   :param x: Defaults to ``{TestService.TestEnum.value_b, TestService.TestEnum.value_c}``.




.. staticmethod:: enum_to_string(x: TestService.TestEnum): string







.. staticmethod:: flags_default(x?: TestService.TestFlags): TestService.TestFlags



   :param x: Defaults to ``TestService.TestFlags.value_a:union(TestService.TestFlags.value_c)``.




.. staticmethod:: flags_echo(x: TestService.TestFlags): TestService.TestFlags







.. staticmethod:: flags_list_echo(x: List): List







.. staticmethod:: flags_struct_echo(x: TestService.TestFlagsStruct): TestService.TestFlagsStruct







.. staticmethod:: flags_to_int(x: TestService.TestFlags): number







.. staticmethod:: float_special_defaults(nan?: number, infinity?: number, negative_infinity?: number, maximum?: number, lowest?: number, fraction?: number): List

   Procedure whose defaults are the special values of float, plus a finite fraction
   that no float can hold exactly.

   :param nan: Defaults to ``0/0``.
   :param infinity: Defaults to ``math.huge``.
   :param negative_infinity: Defaults to ``-math.huge``.
   :param maximum: Defaults to ``krpc.limits.FLOAT_MAX``.
   :param lowest: Defaults to ``krpc.limits.FLOAT_LOWEST``.
   :param fraction: Defaults to ``0.1``.




.. staticmethod:: float_to_string(value: number): string

   Procedure documentation string.





.. staticmethod:: hex_string_to_bytes(value: string): string







.. staticmethod:: increment_dictionary(d: Map): Map







.. staticmethod:: increment_list(l: List): List







.. staticmethod:: increment_list_of_structs(l: List): List







.. staticmethod:: increment_nested_collection(d: Map): Map







.. staticmethod:: increment_set(h: Set): Set







.. staticmethod:: increment_tuple(t: Tuple): Tuple







.. staticmethod:: int32_special_defaults(maximum?: number, minimum?: number): List

   Procedure whose defaults are the extremes of int.

   :param maximum: Defaults to ``krpc.limits.SINT32_MAX``.
   :param minimum: Defaults to ``krpc.limits.SINT32_MIN``.




.. staticmethod:: int32_to_string(value: number): string







.. staticmethod:: int64_special_defaults(maximum?: number, minimum?: number): List

   Procedure whose defaults are the extremes of long.

   :param maximum: Defaults to ``krpc.limits.SINT64_MAX``.
   :param minimum: Defaults to ``krpc.limits.SINT64_MIN``.




.. staticmethod:: int64_to_string(value: number): string







.. staticmethod:: int_to_flags(x: number): TestService.TestFlags







.. staticmethod:: join_with_at_sign(first_value: string, from: string): string

   Returns *first_value* and *from* joined by an at
   sign, e.g. ``name@host``. The parameter names are:

   * a name in camel case, and
   * a name that is a keyword in some languages.

   :param first_value: The string before the at sign.
   :param from: The string after the at sign.
   :returns: The strings joined.




.. staticmethod:: list_default(x?: List): List



   :param x: Defaults to ``{1, 2, 3}``.




.. staticmethod:: nested_struct_echo(x: TestService.TestNestedStruct): TestService.TestNestedStruct







.. staticmethod:: not_nullable_object(value: TestService.TestClass): TestService.TestClass







.. staticmethod:: null_default(obj?: TestService.TestClass | Types.none): string







.. attribute:: nullable_object: TestService.TestClass | nil



   :Attribute: Can be read or written




.. staticmethod:: nullable_struct_echo(x: TestService.TestNullableStruct): TestService.TestNullableStruct







.. attribute:: object_property: TestService.TestClass | nil

   A property holding an object.

   :Attribute: Can be read or written

   :returns: The object, or ``nil``.




.. staticmethod:: on_timer(milliseconds: number, repeats?: number): number



   :param repeats: Defaults to ``1``.




.. staticmethod:: optional_arguments(x: string, y?: string, z?: string, obj?: TestService.TestClass | Types.none): string



   :param y: Defaults to ``'foo'``.
   :param z: Defaults to ``'bar'``.




.. staticmethod:: remove_event_after(milliseconds: number): number







.. staticmethod:: remove_test_object(value: TestService.TestClass)







.. staticmethod:: reset_custom_exception_later()







.. staticmethod:: reset_invalid_operation_exception_later()







.. staticmethod:: return_null_when_not_allowed(): TestService.TestClass







.. staticmethod:: scalar_default(x?: number): number



   :param x: Defaults to ``42``.




.. staticmethod:: set_default(x?: Set): Set



   :param x: Defaults to ``{1, 2, 3}``.




.. staticmethod:: string_default(x?: string): string



   :param x: Defaults to ``'foo'``.




.. attribute:: string_property: string

   Property documentation string.

   :Attribute: Can be read or written




.. attribute:: string_property_private_get: string



   :Attribute: Write-only, cannot be read




.. attribute:: string_property_private_set: string



   :Attribute: Read-only, cannot be set




.. staticmethod:: string_to_bool(value: string): boolean







.. staticmethod:: string_to_double(value: string): number







.. staticmethod:: string_to_enum(value: string): TestService.TestEnum







.. staticmethod:: string_to_float(value: string): number







.. staticmethod:: string_to_int32(value: string): number







.. staticmethod:: string_to_int64(value: string): number







.. staticmethod:: string_to_u_int32(value: string): number







.. staticmethod:: string_to_u_int64(value: string): number







.. staticmethod:: struct_default(x?: TestService.TestStruct): TestService.TestStruct



   :param x: Defaults to ``{42, 'alpha', TestService.TestEnum.value_b, {1, 2, 3}}``.




.. staticmethod:: struct_echo(x: TestService.TestStruct): TestService.TestStruct







.. staticmethod:: struct_echo_nullable(x: TestService.TestStruct | Types.none): TestService.TestStruct | nil







.. staticmethod:: swap_object_tuple(t: Tuple): Tuple







.. staticmethod:: throw_argument_exception(): number







.. staticmethod:: throw_argument_null_exception(foo: string): number







.. staticmethod:: throw_argument_out_of_range_exception(foo: number): number







.. staticmethod:: throw_custom_exception(): number

   Throws a :class:`TestService.CustomException`.


   .. note::

      Links to each kind of member: :mod:`TestService`, :class:`TestService.TestClass`,
      :class:`TestService.TestEnum`, :class:`TestService.TestStruct`, :class:`KRPC.InvalidOperationException`,
      :meth:`TestService.float_to_string`, :attr:`TestService.string_property`,
      :attr:`TestService.string_property_private_get`, :meth:`TestService.TestClass.float_to_string`,
      :meth:`TestService.TestClass.static_method`, :attr:`TestService.TestClass.int_property`,
      :attr:`TestService.TestClass.string_property_private_get`, :data:`TestService.TestEnum.value_a` and
      :attr:`TestService.TestStruct.int_field`.




.. staticmethod:: throw_custom_exception_later(): number







.. staticmethod:: throw_invalid_operation_exception(): number







.. staticmethod:: throw_invalid_operation_exception_later(): number







.. staticmethod:: throw_mapped_exception(name: string): number

   Throws the system exception of the given name, which the server maps to the KRPC
   exception of that name.





.. staticmethod:: tuple_default(x?: Tuple): Tuple



   :param x: Defaults to ``{1, false}``.




.. staticmethod:: u_int32_to_string(value: number): string







.. staticmethod:: u_int64_to_string(value: number): string







.. staticmethod:: uint32_special_defaults(maximum?: number): List

   Procedure whose default is the largest uint.

   :param maximum: Defaults to ``krpc.limits.UINT32_MAX``.




.. staticmethod:: uint64_special_defaults(maximum?: number): List

   Procedure whose default is the largest ulong.

   :param maximum: Defaults to ``krpc.limits.UINT64_MAX``.





.. class:: DeprecatedClass

   .. warning:: Deprecated. Use :class:`TestService.TestClass` instead.

   Deprecated class documentation string.

   .. method:: deprecated_method(): string

      .. warning:: Deprecated. Use :meth:`TestService.TestClass.get_value` instead.

      Deprecated method documentation string.



.. class:: TestClass

   Class documentation string.

   .. method:: echo_nullable_object(value: TestService.TestClass | Types.none): TestService.TestClass | nil

   .. method:: float_to_string(x: number): string

   .. method:: get_value(): string

      Method documentation string.

   .. attribute:: int_property: number

      Property documentation string.

      :Attribute: Can be read or written

   .. attribute:: object_property: TestService.TestClass | nil

      A property of the object, holding another object.

      :Attribute: Can be read or written

      :returns: The other object, or ``nil``.

   .. method:: object_to_string(other: TestService.TestClass | Types.none): string

   .. method:: optional_arguments(x: string, y?: string, z?: string, obj?: TestService.TestClass | Types.none): string



      :param y: Defaults to ``'foo'``.
      :param z: Defaults to ``'bar'``.

   .. staticmethod:: static_method(a?: string, b?: string): string

      Returns static followed by *a* and *b*.

      :param a: The first string. Defaults to ``''``.
      :param b: The second string. Defaults to ``''``.
      :returns: The strings joined.

   .. staticmethod:: static_nullable_object(value: TestService.TestClass | Types.none): TestService.TestClass | nil

   .. staticmethod:: static_optional_arguments(x: string, y?: string, z?: string, obj?: TestService.TestClass | Types.none): string



      :param y: Defaults to ``'foo'``.
      :param z: Defaults to ``'bar'``.

   .. attribute:: string_property_private_get: string



      :Attribute: Write-only, cannot be read

   .. attribute:: string_property_private_set: string



      :Attribute: Read-only, cannot be set



.. class:: DeprecatedEnum

   .. warning:: Deprecated. Use :class:`TestService.TestEnum` instead.

   Deprecated enum documentation string.


   .. data:: value_a

      Deprecated enum ValueA documentation string.


   .. data:: value_b

      .. warning:: Deprecated. Use :data:`TestService.DeprecatedEnum.value_a` instead.

      Deprecated enum ValueB documentation string.



.. class:: TestEnum

   Enum documentation string.


   .. data:: value_a

      Enum ValueA documentation string.


   .. data:: value_b

      Enum ValueB documentation string.


   .. data:: value_c

      Enum ValueC documentation string, in :math:`\mathrm{m/s^{2}}`, equal to
      :math:`\frac{1}{2} \rho v^{2}`.



.. class:: TestFlags

   Flags documentation string.

   A flags enumeration. :std:ref:`lua-client-flags` shows how to combine its values.


   .. data:: none

      Flags None documentation string.


   .. data:: value_a

      Flags ValueA documentation string.


   .. data:: value_ab

      Flags ValueAB documentation string.


   .. data:: value_b

      Flags ValueB documentation string.


   .. data:: value_c

      Flags ValueC documentation string.



.. class:: DeprecatedStruct

   .. warning:: Deprecated. Use :class:`TestService.TestStruct` instead.

   Deprecated struct documentation string.


   .. attribute:: value: number

      Deprecated struct Value documentation string.


   .. attribute:: old_value: number

      .. warning:: Deprecated. Use :attr:`TestService.DeprecatedStruct.value` instead.

      Deprecated struct OldValue documentation string.



.. class:: TestFlagsStruct

   Flags struct documentation string.


   .. attribute:: flags_field: TestService.TestFlags





.. class:: TestNestedNullableStruct

   Nested nullable struct documentation string.


   .. attribute:: list_field: List




   .. attribute:: string_field: string





.. class:: TestNestedStruct

   Nested struct documentation string.


   .. attribute:: struct_field: TestService.TestStruct




   .. attribute:: object_field: TestService.TestClass




   .. attribute:: string_field: string





.. class:: TestNullableStruct

   Nullable struct documentation string.


   .. attribute:: int_field: number




   .. attribute:: nullable_int_field: number | Types.none




   .. attribute:: nullable_enum_field: TestService.TestEnum | Types.none




   .. attribute:: nullable_string_field: string | Types.none




   .. attribute:: nullable_object_field: TestService.TestClass | Types.none





.. class:: TestStruct

   Struct documentation string.


   .. attribute:: int_field: number

      Struct IntField documentation string.


   .. attribute:: string_field: string




   .. attribute:: enum_field: TestService.TestEnum




   .. attribute:: list_field: List





.. class:: CustomException





.. class:: DeprecatedException

   .. warning:: Deprecated. Use :class:`TestService.CustomException` instead.

   Deprecated exception documentation string.
