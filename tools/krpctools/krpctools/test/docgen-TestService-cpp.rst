.. default-domain:: cpp
.. highlight:: cpp

.. namespace:: krpc::services::TestService


.. namespace:: krpc::services
.. class:: TestService : public krpc::Service

   Service documentation string.

   .. function:: TestService(krpc::Client* client)

      Construct an instance of this service.

   .. function:: std::string add_multiple_values(float x, int32_t y, int64_t z)



      :Parameters:





   

   .. function:: std::vector<TestClass> add_to_object_list(std::vector<TestClass> l, std::string value)



      :Parameters:




   

   .. function:: int32_t blocking_procedure(int32_t n, int32_t sum = 0)



      :Parameters:




   

   .. function:: std::string bool_to_string(bool value)



      :Parameters:



   

   .. function:: std::string bytes_to_hex_string(std::string value)



      :Parameters:



   

   .. function:: int32_t counter(std::string id = "", int32_t divisor = 1)



      :Parameters:




   

   .. function:: TestStruct counter_struct(std::string id = "")

      Returns a struct whose IntField counts the number of times it has been called,
      so that a stream on it changes on every update.

      :Parameters:



   

   .. function:: TestClass create_test_object(std::string value)



      :Parameters:



   

   .. function:: std::string deprecated_procedure(float value)

      .. warning:: Deprecated. Use :func:`float_to_string` instead.

      Deprecated procedure documentation string.

      :Parameters:



   

   .. function:: std::string deprecated_procedure_no_message(float value)

      .. warning:: Deprecated.

      Deprecated procedure with no reason documentation string.

      :Parameters:



   

   .. function:: std::string deprecated_property()
   .. function:: void set_deprecated_property(std::string value)

      .. warning:: Deprecated. Use :func:`string_property` instead.

      Deprecated property documentation string.

   

   .. function:: std::map<int32_t, bool> dictionary_default(std::map<int32_t, bool> x = std::map<int32_t, bool>{{1, false}, {2, true}})



      :Parameters:



   

   .. function:: std::vector<double> double_special_defaults(double nan = std::numeric_limits<double>::quiet_NaN(), double infinity = std::numeric_limits<double>::infinity(), double negative_infinity = -std::numeric_limits<double>::infinity(), double maximum = (std::numeric_limits<double>::max)(), double lowest = std::numeric_limits<double>::lowest())

      Procedure whose defaults are the special values of double.

      :Parameters:







   

   .. function:: std::string double_to_string(double value)



      :Parameters:



   

   .. function:: int32_t echo_nullable_int(int32_t value)



      :Parameters:



   

   .. function:: std::vector<int32_t> echo_nullable_list(std::vector<int32_t> l)



      :Parameters:



   

   .. function:: std::string echo_nullable_string(std::string value)



      :Parameters:



   

   .. function:: TestClass echo_test_object(TestClass value)



      :Parameters:



   

   .. function:: std::vector<std::string> empty_list_default(std::vector<std::string> x = std::vector<std::string>{})



      :Parameters:



   

   .. function:: TestEnum enum_default_arg(TestEnum x = TestEnum::value_c)



      :Parameters:



   

   .. function:: TestEnum enum_echo(TestEnum x)



      :Parameters:



   

   .. function:: std::vector<TestEnum> enum_list_default(std::vector<TestEnum> x = std::vector<TestEnum>{TestEnum::value_b, TestEnum::value_c})



      :Parameters:



   

   .. function:: TestEnum enum_return()




   

   .. function:: std::vector<float> float_special_defaults(float nan = std::numeric_limits<float>::quiet_NaN(), float infinity = std::numeric_limits<float>::infinity(), float negative_infinity = -std::numeric_limits<float>::infinity(), float maximum = (std::numeric_limits<float>::max)(), float lowest = std::numeric_limits<float>::lowest(), float fraction = 0.1f)

      Procedure whose defaults are the special values of float, plus a finite fraction
      that no float can hold exactly.

      :Parameters:








   

   .. function:: std::string float_to_string(float value)

      Procedure documentation string.

      :Parameters:



   

   .. function:: std::map<std::string, int32_t> increment_dictionary(std::map<std::string, int32_t> d)



      :Parameters:



   

   .. function:: std::vector<int32_t> increment_list(std::vector<int32_t> l)



      :Parameters:



   

   .. function:: std::vector<TestStruct> increment_list_of_structs(std::vector<TestStruct> l)



      :Parameters:



   

   .. function:: std::map<std::string, std::vector<int32_t>> increment_nested_collection(std::map<std::string, std::vector<int32_t>> d)



      :Parameters:



   

   .. function:: std::set<int32_t> increment_set(std::set<int32_t> h)



      :Parameters:



   

   .. function:: std::tuple<int32_t, int64_t> increment_tuple(std::tuple<int32_t, int64_t> t)



      :Parameters:



   

   .. function:: std::vector<int32_t> int32_special_defaults(int32_t maximum = (std::numeric_limits<int32_t>::max)(), int32_t minimum = (std::numeric_limits<int32_t>::min)())

      Procedure whose defaults are the extremes of int.

      :Parameters:




   

   .. function:: std::string int32_to_string(int32_t value)



      :Parameters:



   

   .. function:: std::vector<int64_t> int64_special_defaults(int64_t maximum = (std::numeric_limits<int64_t>::max)(), int64_t minimum = (std::numeric_limits<int64_t>::min)())

      Procedure whose defaults are the extremes of long.

      :Parameters:




   

   .. function:: std::string int64_to_string(int64_t value)



      :Parameters:



   

   .. function:: std::vector<int32_t> list_default(std::vector<int32_t> x = std::vector<int32_t>{1, 2, 3})



      :Parameters:



   

   .. function:: TestNestedStruct nested_struct_echo(TestNestedStruct x)



      :Parameters:



   

   .. function:: TestClass not_nullable_object(TestClass value)



      :Parameters:



   

   .. function:: TestClass nullable_object()
   .. function:: void set_nullable_object(TestClass value)



   

   .. function:: TestClass object_property()
   .. function:: void set_object_property(TestClass value)



   

   .. function:: ::krpc::Event on_timer(uint32_t milliseconds, uint32_t repeats = 1)



      :Parameters:




   

   .. function:: ::krpc::Event on_timer_using_lambda(uint32_t milliseconds)



      :Parameters:



   

   .. function:: std::string optional_arguments(std::string x, std::string y = "foo", std::string z = "bar", TestClass obj)



      :Parameters:






   

   .. function:: void reset_custom_exception_later()




   

   .. function:: void reset_invalid_operation_exception_later()




   

   .. function:: TestClass return_null_when_not_allowed()




   

   .. function:: std::set<int32_t> set_default(std::set<int32_t> x = std::set<int32_t>{1, 2, 3})



      :Parameters:



   

   .. function:: std::string string_property()
   .. function:: void set_string_property(std::string value)

      Property documentation string.

   

   .. function:: void set_string_property_private_get(std::string value)



   

   .. function:: std::string string_property_private_set()



   

   .. function:: int32_t string_to_int32(std::string value)



      :Parameters:



   

   .. function:: TestStruct struct_default(TestStruct x = TestStruct{42, "jeb", TestEnum::value_b, std::vector<int32_t>{1, 2, 3}})



      :Parameters:



   

   .. function:: TestStruct struct_echo(TestStruct x)



      :Parameters:



   

   .. function:: TestStruct struct_echo_nullable(TestStruct x)



      :Parameters:



   

   .. function:: int32_t throw_argument_exception()




   

   .. function:: int32_t throw_argument_null_exception(std::string foo)



      :Parameters:



   

   .. function:: int32_t throw_argument_out_of_range_exception(int32_t foo)



      :Parameters:



   

   .. function:: int32_t throw_custom_exception()




   

   .. function:: int32_t throw_custom_exception_later()




   

   .. function:: int32_t throw_invalid_operation_exception()




   

   .. function:: int32_t throw_invalid_operation_exception_later()




   

   .. function:: std::tuple<int32_t, bool> tuple_default(std::tuple<int32_t, bool> x = std::tuple<int32_t, bool>{1, false})



      :Parameters:



   

   .. function:: std::vector<uint32_t> uint32_special_defaults(uint32_t maximum = (std::numeric_limits<uint32_t>::max)())

      Procedure whose default is the largest uint.

      :Parameters:



   

   .. function:: std::vector<uint64_t> uint64_special_defaults(uint64_t maximum = (std::numeric_limits<uint64_t>::max)())

      Procedure whose default is the largest ulong.

      :Parameters:



   



.. class:: TestClass

   Class documentation string.

   .. function:: TestClass echo_nullable_object(TestClass value)



      :Parameters:



   

   .. function:: std::string float_to_string(float x)



      :Parameters:



   

   .. function:: std::string get_value()

      Method documentation string.


   

   .. function:: int32_t int_property()
   .. function:: void set_int_property(int32_t value)

      Property documentation string.

   

   .. function:: TestClass object_property()
   .. function:: void set_object_property(TestClass value)



   

   .. function:: std::string object_to_string(TestClass other)



      :Parameters:



   

   .. function:: std::string optional_arguments(std::string x, std::string y = "foo", std::string z = "bar", TestClass obj)



      :Parameters:






   

   .. function:: static std::string static_method(Client& connection, std::string a = "", std::string b = "")



      :Parameters:




   

   .. function:: static TestClass static_nullable_object(Client& connection, TestClass value)



      :Parameters:



   

   .. function:: void set_string_property_private_get(std::string value)



   

   .. function:: std::string string_property_private_set()



   



.. class:: DeprecatedClass

   .. warning:: Deprecated. Use :class:`TestClass` instead.

   Deprecated class documentation string.

   .. function:: std::string deprecated_method()

      .. warning:: Deprecated. Use :func:`TestClass::get_value` instead.

      Deprecated method documentation string.


   



.. namespace:: krpc::services::TestService
.. enum-struct:: TestEnum

   Enum documentation string.


   .. enumerator:: value_a

      Enum ValueA documentation string.


   .. enumerator:: value_b

      Enum ValueB documentation string.


   .. enumerator:: value_c

      Enum ValueC documentation string.



.. namespace:: krpc::services::TestService
.. enum-struct:: DeprecatedEnum

   .. warning:: Deprecated. Use :enum:`TestEnum` instead.

   Deprecated enum documentation string.


   .. enumerator:: value_a

      Deprecated enum ValueA documentation string.


   .. enumerator:: value_b

      .. warning:: Deprecated. Use :enumerator:`DeprecatedEnum::value_a` instead.

      Deprecated enum ValueB documentation string.



.. namespace:: krpc::services::TestService
.. struct:: TestStruct

   Struct documentation string.


   .. member:: int32_t int_field

      Struct IntField documentation string.

   .. member:: std::string string_field



   .. member:: TestEnum enum_field



   .. member:: std::vector<int32_t> list_field




.. namespace:: krpc::services::TestService
.. struct:: TestNestedStruct

   Nested struct documentation string.


   .. member:: TestStruct struct_field



   .. member:: TestClass object_field



   .. member:: std::string string_field




.. namespace:: krpc::services::TestService
.. struct:: DeprecatedStruct

   .. warning:: Deprecated. Use :struct:`TestStruct` instead.

   Deprecated struct documentation string.


   .. member:: int32_t value

      Deprecated struct Value documentation string.

   .. member:: int32_t old_value

      .. warning:: Deprecated. Use :member:`DeprecatedStruct::value` instead.

      Deprecated struct OldValue documentation string.


.. namespace:: krpc::services::TestService
.. class:: CustomException





.. namespace:: krpc::services::TestService
.. class:: DeprecatedException

   .. warning:: Deprecated. Use CustomException instead.

   Deprecated exception documentation string.
