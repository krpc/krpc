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

   .. function:: TestClass create_test_object(std::string value)



      :Parameters:

   .. function:: std::map<int32_t, bool> dictionary_default(std::map<int32_t, bool> x = ((1, false), (2, true)))



      :Parameters:

   .. function:: std::string double_to_string(double value)



      :Parameters:

   .. function:: TestClass echo_test_object(TestClass value)



      :Parameters:

   .. function:: TestEnum enum_default_arg(TestEnum x = static_cast<TestEnum>(2))



      :Parameters:

   .. function:: TestEnum enum_echo(TestEnum x)



      :Parameters:

   .. function:: TestEnum enum_return()

   .. function:: std::string float_to_string(float value)

      Procedure documentation string.

      :Parameters:

   .. function:: std::map<std::string, int32_t> increment_dictionary(std::map<std::string, int32_t> d)



      :Parameters:

   .. function:: std::vector<int32_t> increment_list(std::vector<int32_t> l)



      :Parameters:

   .. function:: std::map<std::string, std::vector<int32_t>> increment_nested_collection(std::map<std::string, std::vector<int32_t>> d)



      :Parameters:

   .. function:: std::set<int32_t> increment_set(std::set<int32_t> h)



      :Parameters:

   .. function:: std::tuple<int32_t, int64_t> increment_tuple(std::tuple<int32_t, int64_t> t)



      :Parameters:

   .. function:: std::string int32_to_string(int32_t value)



      :Parameters:

   .. function:: std::string int64_to_string(int64_t value)



      :Parameters:

   .. function:: std::vector<int32_t> list_default(std::vector<int32_t> x = (1, 2, 3))



      :Parameters:

   .. function:: TestClass object_property()
   .. function:: void set_object_property(TestClass value)

   .. function:: ::krpc::Event on_timer(uint32_t milliseconds, uint32_t repeats = 1)



      :Parameters:

   .. function:: ::krpc::Event on_timer_using_lambda(uint32_t milliseconds)



      :Parameters:

   .. function:: std::string optional_arguments(std::string x, std::string y = "foo", std::string z = "bar", TestClass obj = TestClass())



      :Parameters:

   .. function:: void reset_custom_exception_later()

   .. function:: void reset_invalid_operation_exception_later()

   .. function:: TestClass return_null_when_not_allowed()

   .. function:: std::set<int32_t> set_default(std::set<int32_t> x = (1, 2, 3))



      :Parameters:

   .. function:: std::string string_property()
   .. function:: void set_string_property(std::string value)

      Property documentation string.

   .. function:: void set_string_property_private_get(std::string value)

   .. function:: std::string string_property_private_set()

   .. function:: int32_t string_to_int32(std::string value)



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

   .. function:: std::tuple<int32_t, bool> tuple_default(std::tuple<int32_t, bool> x = (1, false))



      :Parameters:



.. class:: TestClass

   Class documentation string.

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

   .. function:: std::string optional_arguments(std::string x, std::string y = "foo", std::string z = "bar", TestClass obj = TestClass())



      :Parameters:

   .. function:: static std::string static_method(Client& connection, std::string a = "", std::string b = "")



      :Parameters:



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
.. class:: CustomException
