.. default-domain:: py
.. highlight:: py

.. currentmodule:: TestService


.. module:: TestService

Service documentation string.


.. staticmethod:: add_multiple_values(x, y, z)



   :param float x:
   :param int y:
   :param long z:
   :rtype: str
   :Game Scenes: All





.. staticmethod:: add_to_object_list(l, value)



   :param list l:
   :param str value:
   :rtype: list(:class:`TestClass`)
   :Game Scenes: All





.. staticmethod:: blocking_procedure(n, [sum = 0])



   :param int n:
   :param int sum:
   :rtype: int
   :Game Scenes: All





.. staticmethod:: bool_to_string(value)



   :param bool value:
   :rtype: str
   :Game Scenes: All





.. staticmethod:: bytes_to_hex_string(value)



   :param bytes value:
   :rtype: str
   :Game Scenes: All





.. staticmethod:: counter([id = ''], [divisor = 1])



   :param str id:
   :param int divisor:
   :rtype: int
   :Game Scenes: All





.. staticmethod:: create_test_object(value)



   :param str value:
   :rtype: :class:`TestClass`
   :Game Scenes: All





.. staticmethod:: dictionary_default([x = {1: False, 2: True}])



   :param dict x:
   :rtype: dict(int, bool)
   :Game Scenes: All





.. staticmethod:: double_to_string(value)



   :param double value:
   :rtype: str
   :Game Scenes: All





.. staticmethod:: echo_test_object(value)



   :param TestClass value:
   :rtype: :class:`TestClass`
   :Game Scenes: All





.. staticmethod:: enum_default_arg([x = 2])



   :param TestEnum x:
   :rtype: :class:`TestEnum`
   :Game Scenes: All





.. staticmethod:: enum_echo(x)



   :param TestEnum x:
   :rtype: :class:`TestEnum`
   :Game Scenes: All





.. staticmethod:: enum_return()



   :rtype: :class:`TestEnum`
   :Game Scenes: All





.. staticmethod:: float_to_string(value)

   Procedure documentation string.

   :param float value:
   :rtype: str
   :Game Scenes: All





.. staticmethod:: increment_dictionary(d)



   :param dict d:
   :rtype: dict(str, int)
   :Game Scenes: All





.. staticmethod:: increment_list(l)



   :param list l:
   :rtype: list(int)
   :Game Scenes: All





.. staticmethod:: increment_nested_collection(d)



   :param dict d:
   :rtype: dict(str, list(int))
   :Game Scenes: All





.. staticmethod:: increment_set(h)



   :param set h:
   :rtype: set(int)
   :Game Scenes: All





.. staticmethod:: increment_tuple(t)



   :param tuple t:
   :rtype: tuple(int, long)
   :Game Scenes: All





.. staticmethod:: int32_to_string(value)



   :param int value:
   :rtype: str
   :Game Scenes: All





.. staticmethod:: int64_to_string(value)



   :param long value:
   :rtype: str
   :Game Scenes: All





.. staticmethod:: list_default([x = [1, 2, 3]])



   :param list x:
   :rtype: list(int)
   :Game Scenes: All





.. attribute:: object_property



   :Attribute: Can be read or written
   :rtype: :class:`TestClass`
   :Game Scenes: All





.. staticmethod:: on_timer(milliseconds, [repeats = 1])



   :param int milliseconds:
   :param int repeats:
   :rtype: :class:`krpc.schema.KRPC.Event`
   :Game Scenes: All





.. staticmethod:: on_timer_using_lambda(milliseconds)



   :param int milliseconds:
   :rtype: :class:`krpc.schema.KRPC.Event`
   :Game Scenes: All





.. staticmethod:: optional_arguments(x, [y = 'foo'], [z = 'bar'], [obj = None])



   :param str x:
   :param str y:
   :param str z:
   :param TestClass obj:
   :rtype: str
   :Game Scenes: All





.. staticmethod:: reset_custom_exception_later()



   :Game Scenes: All





.. staticmethod:: reset_invalid_operation_exception_later()



   :Game Scenes: All





.. staticmethod:: return_null_when_not_allowed()



   :rtype: :class:`TestClass`
   :Game Scenes: All





.. staticmethod:: set_default([x = {1, 2, 3}])



   :param set x:
   :rtype: set(int)
   :Game Scenes: All





.. attribute:: string_property

   Property documentation string.

   :Attribute: Can be read or written
   :rtype: str
   :Game Scenes: All





.. attribute:: string_property_private_get



   :Attribute: Write-only, cannot be read
   :rtype: str
   :Game Scenes: All





.. attribute:: string_property_private_set



   :Attribute: Read-only, cannot be set
   :rtype: str
   :Game Scenes: All





.. staticmethod:: string_to_int32(value)



   :param str value:
   :rtype: int
   :Game Scenes: All





.. staticmethod:: throw_argument_exception()



   :rtype: int
   :Game Scenes: All





.. staticmethod:: throw_argument_null_exception(foo)



   :param str foo:
   :rtype: int
   :Game Scenes: All





.. staticmethod:: throw_argument_out_of_range_exception(foo)



   :param int foo:
   :rtype: int
   :Game Scenes: All





.. staticmethod:: throw_custom_exception()



   :rtype: int
   :Game Scenes: All





.. staticmethod:: throw_custom_exception_later()



   :rtype: int
   :Game Scenes: All





.. staticmethod:: throw_invalid_operation_exception()



   :rtype: int
   :Game Scenes: All





.. staticmethod:: throw_invalid_operation_exception_later()



   :rtype: int
   :Game Scenes: All





.. staticmethod:: tuple_default([x = (1, False)])



   :param tuple x:
   :rtype: tuple(int, bool)
   :Game Scenes: All






.. class:: TestClass

   Class documentation string.

   .. method:: float_to_string(x)



      :param float x:
      :rtype: str
      :Game Scenes: All

   .. method:: get_value()

      Method documentation string.

      :rtype: str
      :Game Scenes: All

   .. attribute:: int_property

      Property documentation string.

      :Attribute: Can be read or written
      :rtype: int
      :Game Scenes: All

   .. attribute:: object_property



      :Attribute: Can be read or written
      :rtype: :class:`TestClass`
      :Game Scenes: All

   .. method:: object_to_string(other)



      :param TestClass other:
      :rtype: str
      :Game Scenes: All

   .. method:: optional_arguments(x, [y = 'foo'], [z = 'bar'], [obj = None])



      :param str x:
      :param str y:
      :param str z:
      :param TestClass obj:
      :rtype: str
      :Game Scenes: All

   .. staticmethod:: static_method([a = ''], [b = ''])



      :param str a:
      :param str b:
      :rtype: str
      :Game Scenes: All



.. class:: TestEnum

   Enum documentation string.


   .. data:: value_a

      Enum ValueA documentation string.


   .. data:: value_b

      Enum ValueB documentation string.


   .. data:: value_c

      Enum ValueC documentation string.



.. class:: CustomException
