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




.. staticmethod:: add_to_object_list(l, value)



   :param List l:
   :param string value:
   :rtype: List of :class:`TestService.TestClass`




.. staticmethod:: blocking_procedure(n, [sum = 0])



   :param number n:
   :param number sum:
   :rtype: number




.. staticmethod:: bool_to_string(value)



   :param boolean value:
   :rtype: string




.. staticmethod:: bytes_to_hex_string(value)



   :param string value:
   :rtype: string




.. staticmethod:: counter([id = ''], [divisor = 1])



   :param string id:
   :param number divisor:
   :rtype: number




.. staticmethod:: create_test_object(value)



   :param string value:
   :rtype: :class:`TestService.TestClass`




.. staticmethod:: dictionary_default([x = {1: False, 2: True}])



   :param Map x:
   :rtype: Map from number to boolean




.. staticmethod:: double_to_string(value)



   :param number value:
   :rtype: string




.. staticmethod:: echo_test_object(value)



   :param TestService.TestClass value:
   :rtype: :class:`TestService.TestClass`




.. staticmethod:: enum_default_arg([x = 2])



   :param TestService.TestEnum x:
   :rtype: :class:`TestService.TestEnum`




.. staticmethod:: enum_echo(x)



   :param TestService.TestEnum x:
   :rtype: :class:`TestService.TestEnum`




.. staticmethod:: enum_return()



   :rtype: :class:`TestService.TestEnum`




.. staticmethod:: float_to_string(value)

   Procedure documentation string.

   :param number value:
   :rtype: string




.. staticmethod:: increment_dictionary(d)



   :param Map d:
   :rtype: Map from string to number




.. staticmethod:: increment_list(l)



   :param List l:
   :rtype: List of number




.. staticmethod:: increment_nested_collection(d)



   :param Map d:
   :rtype: Map from string to List of number




.. staticmethod:: increment_set(h)



   :param Set h:
   :rtype: Set of number




.. staticmethod:: increment_tuple(t)



   :param Tuple t:
   :rtype: Tuple of (number, number)




.. staticmethod:: int32_to_string(value)



   :param number value:
   :rtype: string




.. staticmethod:: int64_to_string(value)



   :param number value:
   :rtype: string




.. staticmethod:: list_default([x = [1, 2, 3]])



   :param List x:
   :rtype: List of number




.. attribute:: object_property



   :Attribute: Can be read or written
   :rtype: :class:`TestService.TestClass`




.. staticmethod:: on_timer(milliseconds, [repeats = 1])



   :param number milliseconds:
   :param number repeats:
   :rtype: :class:`krpc.schema.KRPC.Event`




.. staticmethod:: on_timer_using_lambda(milliseconds)



   :param number milliseconds:
   :rtype: :class:`krpc.schema.KRPC.Event`




.. staticmethod:: optional_arguments(x, [y = 'foo'], [z = 'bar'], [obj = None])



   :param string x:
   :param string y:
   :param string z:
   :param TestService.TestClass obj:
   :rtype: string




.. staticmethod:: reset_custom_exception_later()







.. staticmethod:: reset_invalid_operation_exception_later()







.. staticmethod:: return_null_when_not_allowed()



   :rtype: :class:`TestService.TestClass`




.. staticmethod:: set_default([x = {1, 2, 3}])



   :param Set x:
   :rtype: Set of number




.. attribute:: string_property

   Property documentation string.

   :Attribute: Can be read or written
   :rtype: string




.. attribute:: string_property_private_get



   :Attribute: Write-only, cannot be read
   :rtype: string




.. attribute:: string_property_private_set



   :Attribute: Read-only, cannot be set
   :rtype: string




.. staticmethod:: string_to_int32(value)



   :param string value:
   :rtype: number




.. staticmethod:: throw_argument_exception()



   :rtype: number




.. staticmethod:: throw_argument_null_exception(foo)



   :param string foo:
   :rtype: number




.. staticmethod:: throw_argument_out_of_range_exception(foo)



   :param number foo:
   :rtype: number




.. staticmethod:: throw_custom_exception()



   :rtype: number




.. staticmethod:: throw_custom_exception_later()



   :rtype: number




.. staticmethod:: throw_invalid_operation_exception()



   :rtype: number




.. staticmethod:: throw_invalid_operation_exception_later()



   :rtype: number




.. staticmethod:: tuple_default([x = (1, False)])



   :param Tuple x:
   :rtype: Tuple of (number, boolean)





.. class:: TestClass

   Class documentation string.

   .. method:: float_to_string(x)



      :param number x:
      :rtype: string

   .. method:: get_value()

      Method documentation string.

      :rtype: string

   .. attribute:: int_property

      Property documentation string.

      :Attribute: Can be read or written
      :rtype: number

   .. attribute:: object_property



      :Attribute: Can be read or written
      :rtype: :class:`TestService.TestClass`

   .. method:: object_to_string(other)



      :param TestService.TestClass other:
      :rtype: string

   .. method:: optional_arguments(x, [y = 'foo'], [z = 'bar'], [obj = None])



      :param string x:
      :param string y:
      :param string z:
      :param TestService.TestClass obj:
      :rtype: string

   .. staticmethod:: static_method([a = ''], [b = ''])



      :param string a:
      :param string b:
      :rtype: string



.. class:: TestEnum

   Enum documentation string.


   .. data:: value_a

      Enum ValueA documentation string.


   .. data:: value_b

      Enum ValueB documentation string.


   .. data:: value_c

      Enum ValueC documentation string.



.. class:: CustomException
