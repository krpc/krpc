.. default-domain:: lua
.. highlight:: lua

.. currentmodule:: TestService


.. module:: TestService

Service documentation string.


.. staticmethod:: add_multiple_values(x, y, z)



   :param number x:
   :param number y:
   :param number z:
   :rtype: string




.. staticmethod:: add_to_object_dictionary(d, key)



   :param Map d:
   :param string key:
   :rtype: Map




.. staticmethod:: add_to_object_list(l, value)



   :param List l:
   :param string value:
   :rtype: List




.. staticmethod:: add_to_object_set(h, value)



   :param Set h:
   :param string value:
   :rtype: Set




.. staticmethod:: blocking_procedure(n, [sum = 0])



   :param number n:
   :param number sum:
   :rtype: number




.. staticmethod:: bool_to_string(value)



   :param boolean value:
   :rtype: string




.. staticmethod:: bytes_default([x = '\202\254'])



   :param string x:
   :rtype: string




.. staticmethod:: bytes_to_hex_string(value)



   :param string value:
   :rtype: string




.. staticmethod:: counter([id = ''], [divisor = 1])



   :param string id:
   :param number divisor:
   :rtype: number




.. staticmethod:: counter_struct([id = ''])

   Returns a struct whose IntField counts the number of times it has been called,
   so that a stream on it changes on every update.

   :param string id:
   :rtype: :class:`TestService.TestStruct`




.. staticmethod:: create_test_object(value)



   :param string value:
   :rtype: :class:`TestService.TestClass`




.. staticmethod:: deprecated_procedure(value)

   .. warning:: Deprecated. Use :meth:`TestService.float_to_string` instead.

   Deprecated procedure documentation string.

   :param number value:
   :rtype: string




.. staticmethod:: deprecated_procedure_no_message(value)

   .. warning:: Deprecated.

   Deprecated procedure with no reason documentation string.

   :param number value:
   :rtype: string




.. attribute:: deprecated_property: string

   .. warning:: Deprecated. Use :attr:`TestService.string_property <TestService.TestService.string_property>` instead.

   Deprecated property documentation string.

   :Attribute: Can be read or written
   :rtype: string




.. staticmethod:: dictionary_default([x = {[1] = false, [2] = true}])



   :param Map x:
   :rtype: Map




.. staticmethod:: double_special_defaults([nan = 0/0], [infinity = math.huge], [negative_infinity = -math.huge], [maximum = krpc.limits.DOUBLE_MAX], [lowest = krpc.limits.DOUBLE_LOWEST])

   Procedure whose defaults are the special values of double.

   :param number nan:
   :param number infinity:
   :param number negative_infinity:
   :param number maximum:
   :param number lowest:
   :rtype: List




.. staticmethod:: double_to_string(value)



   :param number value:
   :rtype: string




.. staticmethod:: dump_expression_tree(expression)

   The expression tree of a function, as text.

   :param KRPC.Expression expression:
   :rtype: string




.. staticmethod:: echo_dictionary_of_nullable_objects(d)



   :param Map d:
   :rtype: Map




.. staticmethod:: echo_list_of_nullable_ints(l)



   :param List l:
   :rtype: List




.. staticmethod:: echo_list_of_nullable_objects(l)



   :param List l:
   :rtype: List




.. staticmethod:: echo_nested_list_of_nullable_objects(l)



   :param List l:
   :rtype: List




.. staticmethod:: echo_nested_nullable_struct(value)



   :param TestService.TestNestedNullableStruct value:
   :rtype: :class:`TestService.TestNestedNullableStruct`




.. staticmethod:: echo_nullable_bool(value)



   :param value:
   :type value: boolean or ``Types.none``
   :rtype: boolean or ``Types.none``




.. staticmethod:: echo_nullable_bytes(value)



   :param value:
   :type value: string or ``Types.none``
   :rtype: string or ``Types.none``




.. staticmethod:: echo_nullable_dictionary(d)



   :param d:
   :type d: Map or ``Types.none``
   :rtype: Map or ``Types.none``




.. staticmethod:: echo_nullable_double(value)



   :param value:
   :type value: number or ``Types.none``
   :rtype: number or ``Types.none``




.. staticmethod:: echo_nullable_enum(x)



   :param x:
   :type x: :class:`TestService.TestEnum` or ``Types.none``
   :rtype: :class:`TestService.TestEnum` or ``Types.none``




.. staticmethod:: echo_nullable_float(value)



   :param value:
   :type value: number or ``Types.none``
   :rtype: number or ``Types.none``




.. staticmethod:: echo_nullable_int(value)

   Returns the given value.

   :param value: The value, or ``nil``.
   :type value: number or ``Types.none``
   :returns: The value.
   :rtype: number or ``Types.none``




.. staticmethod:: echo_nullable_list(l)



   :param l:
   :type l: List or ``Types.none``
   :rtype: List or ``Types.none``




.. staticmethod:: echo_nullable_long(value)



   :param value:
   :type value: number or ``Types.none``
   :rtype: number or ``Types.none``




.. staticmethod:: echo_nullable_set(h)



   :param h:
   :type h: Set or ``Types.none``
   :rtype: Set or ``Types.none``




.. staticmethod:: echo_nullable_string(value)



   :param value:
   :type value: string or ``Types.none``
   :rtype: string or ``Types.none``




.. staticmethod:: echo_nullable_tuple(t)



   :param t:
   :type t: Tuple or ``Types.none``
   :rtype: Tuple or ``Types.none``




.. staticmethod:: echo_nullable_u_int(value)



   :param value:
   :type value: number or ``Types.none``
   :rtype: number or ``Types.none``




.. staticmethod:: echo_nullable_u_long(value)



   :param value:
   :type value: number or ``Types.none``
   :rtype: number or ``Types.none``




.. staticmethod:: echo_string_set(h)



   :param Set h:
   :rtype: Set




.. staticmethod:: echo_test_object(value)



   :param value:
   :type value: :class:`TestService.TestClass` or ``Types.none``
   :rtype: :class:`TestService.TestClass` or ``Types.none``




.. staticmethod:: echo_tuple_with_a_nullable_object(t)



   :param Tuple t:
   :rtype: Tuple




.. staticmethod:: empty_list_default([x = {}])



   :param List x:
   :rtype: List




.. staticmethod:: enum_default_arg([x = TestService.TestEnum.value_c])



   :param TestService.TestEnum x:
   :rtype: :class:`TestService.TestEnum`




.. staticmethod:: enum_echo(x)



   :param TestService.TestEnum x:
   :rtype: :class:`TestService.TestEnum`




.. staticmethod:: enum_list_default([x = {TestService.TestEnum.value_b, TestService.TestEnum.value_c}])



   :param List x:
   :rtype: List




.. staticmethod:: enum_return()



   :rtype: :class:`TestService.TestEnum`




.. staticmethod:: enum_to_string(x)



   :param TestService.TestEnum x:
   :rtype: string




.. staticmethod:: float_special_defaults([nan = 0/0], [infinity = math.huge], [negative_infinity = -math.huge], [maximum = krpc.limits.FLOAT_MAX], [lowest = krpc.limits.FLOAT_LOWEST], [fraction = 0.1])

   Procedure whose defaults are the special values of float, plus a finite fraction
   that no float can hold exactly.

   :param number nan:
   :param number infinity:
   :param number negative_infinity:
   :param number maximum:
   :param number lowest:
   :param number fraction:
   :rtype: List




.. staticmethod:: float_to_string(value)

   Procedure documentation string.

   :param number value:
   :rtype: string




.. staticmethod:: hex_string_to_bytes(value)



   :param string value:
   :rtype: string




.. staticmethod:: increment_dictionary(d)



   :param Map d:
   :rtype: Map




.. staticmethod:: increment_list(l)



   :param List l:
   :rtype: List




.. staticmethod:: increment_list_of_structs(l)



   :param List l:
   :rtype: List




.. staticmethod:: increment_nested_collection(d)



   :param Map d:
   :rtype: Map




.. staticmethod:: increment_set(h)



   :param Set h:
   :rtype: Set




.. staticmethod:: increment_tuple(t)



   :param Tuple t:
   :rtype: Tuple




.. staticmethod:: int32_special_defaults([maximum = krpc.limits.SINT32_MAX], [minimum = krpc.limits.SINT32_MIN])

   Procedure whose defaults are the extremes of int.

   :param number maximum:
   :param number minimum:
   :rtype: List




.. staticmethod:: int32_to_string(value)



   :param number value:
   :rtype: string




.. staticmethod:: int64_special_defaults([maximum = krpc.limits.SINT64_MAX], [minimum = krpc.limits.SINT64_MIN])

   Procedure whose defaults are the extremes of long.

   :param number maximum:
   :param number minimum:
   :rtype: List




.. staticmethod:: int64_to_string(value)



   :param number value:
   :rtype: string




.. staticmethod:: join_with_at_sign(first_value, from)

   Returns *first_value* and *from* joined by an at
   sign, e.g. ``name@host``. The parameter names are:

   * a name in camel case, and
   * a name that is a keyword in some languages.

   :param string first_value: The string before the at sign.
   :param string from: The string after the at sign.
   :returns: The strings joined.
   :rtype: string




.. staticmethod:: list_default([x = {1, 2, 3}])



   :param List x:
   :rtype: List




.. staticmethod:: nested_struct_echo(x)



   :param TestService.TestNestedStruct x:
   :rtype: :class:`TestService.TestNestedStruct`




.. staticmethod:: not_nullable_object(value)



   :param TestService.TestClass value:
   :rtype: :class:`TestService.TestClass`




.. staticmethod:: null_default([obj = Types.none])



   :param obj:
   :type obj: :class:`TestService.TestClass` or ``Types.none``
   :rtype: string




.. attribute:: nullable_object: TestService.TestClass



   :Attribute: Can be read or written
   :rtype: :class:`TestService.TestClass` or ``Types.none``




.. staticmethod:: nullable_struct_echo(x)



   :param TestService.TestNullableStruct x:
   :rtype: :class:`TestService.TestNullableStruct`




.. attribute:: object_property: TestService.TestClass

   A property holding an object.

   :Attribute: Can be read or written
   :returns: The object, or ``nil``.
   :rtype: :class:`TestService.TestClass` or ``Types.none``




.. staticmethod:: on_timer(milliseconds, [repeats = 1])



   :param number milliseconds:
   :param number repeats:
   :rtype: :class:`krpc.schema.Event`




.. staticmethod:: on_timer_using_lambda(milliseconds)



   :param number milliseconds:
   :rtype: :class:`krpc.schema.Event`




.. staticmethod:: optional_arguments(x, [y = 'foo'], [z = 'bar'], [obj = Types.none])



   :param string x:
   :param string y:
   :param string z:
   :param obj:
   :type obj: :class:`TestService.TestClass` or ``Types.none``
   :rtype: string




.. staticmethod:: remove_event_after(milliseconds)



   :param number milliseconds:
   :rtype: :class:`krpc.schema.Event`




.. staticmethod:: remove_test_object(value)



   :param TestService.TestClass value:




.. staticmethod:: reset_custom_exception_later()







.. staticmethod:: reset_invalid_operation_exception_later()







.. staticmethod:: return_null_when_not_allowed()



   :rtype: :class:`TestService.TestClass`




.. staticmethod:: scalar_default([x = 42])



   :param number x:
   :rtype: number




.. staticmethod:: set_default([x = {1, 2, 3}])



   :param Set x:
   :rtype: Set




.. staticmethod:: string_default([x = 'foo'])



   :param string x:
   :rtype: string




.. attribute:: string_property: string

   Property documentation string.

   :Attribute: Can be read or written
   :rtype: string




.. attribute:: string_property_private_get: string



   :Attribute: Write-only, cannot be read
   :rtype: string




.. attribute:: string_property_private_set: string



   :Attribute: Read-only, cannot be set
   :rtype: string




.. staticmethod:: string_to_bool(value)



   :param string value:
   :rtype: boolean




.. staticmethod:: string_to_double(value)



   :param string value:
   :rtype: number




.. staticmethod:: string_to_enum(value)



   :param string value:
   :rtype: :class:`TestService.TestEnum`




.. staticmethod:: string_to_float(value)



   :param string value:
   :rtype: number




.. staticmethod:: string_to_int32(value)



   :param string value:
   :rtype: number




.. staticmethod:: string_to_int64(value)



   :param string value:
   :rtype: number




.. staticmethod:: string_to_u_int32(value)



   :param string value:
   :rtype: number




.. staticmethod:: string_to_u_int64(value)



   :param string value:
   :rtype: number




.. staticmethod:: struct_default([x = {42, 'alpha', TestService.TestEnum.value_b, {1, 2, 3}}])



   :param TestService.TestStruct x:
   :rtype: :class:`TestService.TestStruct`




.. staticmethod:: struct_echo(x)



   :param TestService.TestStruct x:
   :rtype: :class:`TestService.TestStruct`




.. staticmethod:: struct_echo_nullable(x)



   :param x:
   :type x: :class:`TestService.TestStruct` or ``Types.none``
   :rtype: :class:`TestService.TestStruct` or ``Types.none``




.. staticmethod:: swap_object_tuple(t)



   :param Tuple t:
   :rtype: Tuple




.. staticmethod:: throw_argument_exception()



   :rtype: number




.. staticmethod:: throw_argument_null_exception(foo)



   :param string foo:
   :rtype: number




.. staticmethod:: throw_argument_out_of_range_exception(foo)



   :param number foo:
   :rtype: number




.. staticmethod:: throw_custom_exception()

   Throws a :class:`TestService.CustomException`.

   :rtype: number

   .. note::

      Links to each kind of member: :mod:`TestService`, :class:`TestService.TestClass`,
      :class:`TestService.TestEnum`, :class:`TestService.TestStruct`, :class:`KRPC.InvalidOperationException`,
      :meth:`TestService.float_to_string`, :attr:`TestService.string_property <TestService.TestService.string_property>`,
      :attr:`TestService.string_property_private_get <TestService.TestService.string_property_private_get>`, :meth:`TestService.TestClass.float_to_string`,
      :meth:`TestService.TestClass.static_method`, :attr:`TestService.TestClass.int_property`,
      :attr:`TestService.TestClass.string_property_private_get`, :attr:`TestService.TestEnum.value_a` and
      :attr:`TestService.TestStruct.int_field`.




.. staticmethod:: throw_custom_exception_later()



   :rtype: number




.. staticmethod:: throw_invalid_operation_exception()



   :rtype: number




.. staticmethod:: throw_invalid_operation_exception_later()



   :rtype: number




.. staticmethod:: throw_mapped_exception(name)

   Throws the system exception of the given name, which the server maps to the KRPC
   exception of that name.

   :param string name:
   :rtype: number




.. staticmethod:: tuple_default([x = {1, false}])



   :param Tuple x:
   :rtype: Tuple




.. staticmethod:: u_int32_to_string(value)



   :param number value:
   :rtype: string




.. staticmethod:: u_int64_to_string(value)



   :param number value:
   :rtype: string




.. staticmethod:: uint32_special_defaults([maximum = krpc.limits.UINT32_MAX])

   Procedure whose default is the largest uint.

   :param number maximum:
   :rtype: List




.. staticmethod:: uint64_special_defaults([maximum = krpc.limits.UINT64_MAX])

   Procedure whose default is the largest ulong.

   :param number maximum:
   :rtype: List





.. class:: TestClass

   Class documentation string.

   .. method:: echo_nullable_object(value)



      :param value:
      :type value: :class:`TestService.TestClass` or ``Types.none``
      :rtype: :class:`TestService.TestClass` or ``Types.none``

   .. method:: float_to_string(x)



      :param number x:
      :rtype: string

   .. method:: get_value()

      Method documentation string.

      :rtype: string

   .. attribute:: int_property: number

      Property documentation string.

      :Attribute: Can be read or written
      :rtype: number

   .. attribute:: object_property: TestService.TestClass

      A property of the object, holding another object.

      :Attribute: Can be read or written
      :returns: The other object, or ``nil``.
      :rtype: :class:`TestService.TestClass` or ``Types.none``

   .. method:: object_to_string(other)



      :param other:
      :type other: :class:`TestService.TestClass` or ``Types.none``
      :rtype: string

   .. method:: optional_arguments(x, [y = 'foo'], [z = 'bar'], [obj = Types.none])



      :param string x:
      :param string y:
      :param string z:
      :param obj:
      :type obj: :class:`TestService.TestClass` or ``Types.none``
      :rtype: string

   .. staticmethod:: static_method([a = ''], [b = ''])

      Returns static followed by *a* and *b*.

      :param string a: The first string.
      :param string b: The second string.
      :returns: The strings joined.
      :rtype: string

   .. staticmethod:: static_nullable_object(value)



      :param value:
      :type value: :class:`TestService.TestClass` or ``Types.none``
      :rtype: :class:`TestService.TestClass` or ``Types.none``

   .. staticmethod:: static_optional_arguments(x, [y = 'foo'], [z = 'bar'], [obj = Types.none])



      :param string x:
      :param string y:
      :param string z:
      :param obj:
      :type obj: :class:`TestService.TestClass` or ``Types.none``
      :rtype: string

   .. attribute:: string_property_private_get: string



      :Attribute: Write-only, cannot be read
      :rtype: string

   .. attribute:: string_property_private_set: string



      :Attribute: Read-only, cannot be set
      :rtype: string



.. class:: DeprecatedClass

   .. warning:: Deprecated. Use :class:`TestService.TestClass` instead.

   Deprecated class documentation string.

   .. method:: deprecated_method()

      .. warning:: Deprecated. Use :meth:`TestService.TestClass.get_value` instead.

      Deprecated method documentation string.

      :rtype: string



.. class:: TestEnum

   Enum documentation string.


   .. data:: value_a

      Enum ValueA documentation string.


   .. data:: value_b

      Enum ValueB documentation string.


   .. data:: value_c

      Enum ValueC documentation string, in :math:`\mathrm{m/s^{2}}`, equal to
      :math:`\frac{1}{2} \rho v^{2}`.



.. class:: DeprecatedEnum

   .. warning:: Deprecated. Use :class:`TestService.TestEnum` instead.

   Deprecated enum documentation string.


   .. data:: value_a

      Deprecated enum ValueA documentation string.


   .. data:: value_b

      .. warning:: Deprecated. Use :attr:`TestService.DeprecatedEnum.value_a` instead.

      Deprecated enum ValueB documentation string.



.. class:: TestStruct

   Struct documentation string.


   .. attribute:: int_field: number

      Struct IntField documentation string.

      :rtype: number

   .. attribute:: string_field: string



      :rtype: string

   .. attribute:: enum_field: TestService.TestEnum



      :rtype: :class:`TestService.TestEnum`

   .. attribute:: list_field: List



      :rtype: List


.. class:: TestNestedStruct

   Nested struct documentation string.


   .. attribute:: struct_field: TestService.TestStruct



      :rtype: :class:`TestService.TestStruct`

   .. attribute:: object_field: TestService.TestClass



      :rtype: :class:`TestService.TestClass`

   .. attribute:: string_field: string



      :rtype: string


.. class:: TestNullableStruct

   Nullable struct documentation string.


   .. attribute:: int_field: number



      :rtype: number

   .. attribute:: nullable_int_field: number



      :rtype: number or ``Types.none``

   .. attribute:: nullable_enum_field: TestService.TestEnum



      :rtype: :class:`TestService.TestEnum` or ``Types.none``

   .. attribute:: nullable_string_field: string



      :rtype: string or ``Types.none``

   .. attribute:: nullable_object_field: TestService.TestClass



      :rtype: :class:`TestService.TestClass` or ``Types.none``


.. class:: TestNestedNullableStruct

   Nested nullable struct documentation string.


   .. attribute:: list_field: List



      :rtype: List

   .. attribute:: string_field: string



      :rtype: string


.. class:: DeprecatedStruct

   .. warning:: Deprecated. Use :class:`TestService.TestStruct` instead.

   Deprecated struct documentation string.


   .. attribute:: value: number

      Deprecated struct Value documentation string.

      :rtype: number

   .. attribute:: old_value: number

      .. warning:: Deprecated. Use :attr:`TestService.DeprecatedStruct.value` instead.

      Deprecated struct OldValue documentation string.

      :rtype: number


.. class:: CustomException





.. class:: DeprecatedException

   .. warning:: Deprecated. Use :class:`TestService.CustomException` instead.

   Deprecated exception documentation string.
