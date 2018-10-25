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





      :Game Scenes: All

   .. function:: std::vector<TestClass> add_to_object_list(std::vector<TestClass> l, std::string value)



      :Parameters:




      :Game Scenes: All

   .. function:: int32_t blocking_procedure(int32_t n, int32_t sum = 0)



      :Parameters:




      :Game Scenes: All

   .. function:: std::string bool_to_string(bool value)



      :Parameters:



      :Game Scenes: All

   .. function:: std::string bytes_to_hex_string(std::string value)



      :Parameters:



      :Game Scenes: All

   .. function:: int32_t counter(std::string id = "", int32_t divisor = 1)



      :Parameters:




      :Game Scenes: All

   .. function:: TestClass create_test_object(std::string value)



      :Parameters:



      :Game Scenes: All

   .. function:: std::map<int32_t, bool> dictionary_default(std::map<int32_t, bool> x = std::map<int32_t, bool>({1, false}, {2, true}))



      :Parameters:



      :Game Scenes: All

   .. function:: std::string double_to_string(double value)



      :Parameters:



      :Game Scenes: All

   .. function:: TestClass echo_test_object(TestClass value)



      :Parameters:



      :Game Scenes: All

   .. function:: TestEnum enum_default_arg(TestEnum x = static_cast<TestEnum>(2))



      :Parameters:



      :Game Scenes: All

   .. function:: TestEnum enum_echo(TestEnum x)



      :Parameters:



      :Game Scenes: All

   .. function:: TestEnum enum_return()




      :Game Scenes: All

   .. function:: std::string float_to_string(float value)

      Procedure documentation string.

      :Parameters:



      :Game Scenes: All

   .. function:: std::map<std::string, int32_t> increment_dictionary(std::map<std::string, int32_t> d)



      :Parameters:



      :Game Scenes: All

   .. function:: std::vector<int32_t> increment_list(std::vector<int32_t> l)



      :Parameters:



      :Game Scenes: All

   .. function:: std::map<std::string, std::vector<int32_t>> increment_nested_collection(std::map<std::string, std::vector<int32_t>> d)



      :Parameters:



      :Game Scenes: All

   .. function:: std::set<int32_t> increment_set(std::set<int32_t> h)



      :Parameters:



      :Game Scenes: All

   .. function:: std::tuple<int32_t, int64_t> increment_tuple(std::tuple<int32_t, int64_t> t)



      :Parameters:



      :Game Scenes: All

   .. function:: std::string int32_to_string(int32_t value)



      :Parameters:



      :Game Scenes: All

   .. function:: std::string int64_to_string(int64_t value)



      :Parameters:



      :Game Scenes: All

   .. function:: std::vector<int32_t> list_default(std::vector<int32_t> x = std::vector<int32_t>(1, 2, 3))



      :Parameters:



      :Game Scenes: All

   .. function:: TestClass object_property()
   .. function:: void set_object_property(TestClass value)



      :Game Scenes: All

   .. function:: ::krpc::Event on_timer(uint32_t milliseconds, uint32_t repeats = 1)



      :Parameters:




      :Game Scenes: All

   .. function:: ::krpc::Event on_timer_using_lambda(uint32_t milliseconds)



      :Parameters:



      :Game Scenes: All

   .. function:: std::string optional_arguments(std::string x, std::string y = "foo", std::string z = "bar", TestClass obj = TestClass())



      :Parameters:






      :Game Scenes: All

   .. function:: void reset_custom_exception_later()




      :Game Scenes: All

   .. function:: void reset_invalid_operation_exception_later()




      :Game Scenes: All

   .. function:: TestClass return_null_when_not_allowed()




      :Game Scenes: All

   .. function:: std::set<int32_t> set_default(std::set<int32_t> x = std::set<int32_t>(1, 2, 3))



      :Parameters:



      :Game Scenes: All

   .. function:: std::string string_property()
   .. function:: void set_string_property(std::string value)

      Property documentation string.

      :Game Scenes: All

   .. function:: void set_string_property_private_get(std::string value)



      :Game Scenes: All

   .. function:: std::string string_property_private_set()



      :Game Scenes: All

   .. function:: int32_t string_to_int32(std::string value)



      :Parameters:



      :Game Scenes: All

   .. function:: int32_t throw_argument_exception()




      :Game Scenes: All

   .. function:: int32_t throw_argument_null_exception(std::string foo)



      :Parameters:



      :Game Scenes: All

   .. function:: int32_t throw_argument_out_of_range_exception(int32_t foo)



      :Parameters:



      :Game Scenes: All

   .. function:: int32_t throw_custom_exception()




      :Game Scenes: All

   .. function:: int32_t throw_custom_exception_later()




      :Game Scenes: All

   .. function:: int32_t throw_invalid_operation_exception()




      :Game Scenes: All

   .. function:: int32_t throw_invalid_operation_exception_later()




      :Game Scenes: All

   .. function:: std::tuple<int32_t, bool> tuple_default(std::tuple<int32_t, bool> x = std::tuple<int32_t, bool>(1, false))



      :Parameters:



      :Game Scenes: All



.. class:: TestClass

   Class documentation string.

   .. function:: std::string float_to_string(float x)



      :Parameters:



      :Game Scenes: All

   .. function:: std::string get_value()

      Method documentation string.


      :Game Scenes: All

   .. function:: int32_t int_property()
   .. function:: void set_int_property(int32_t value)

      Property documentation string.

      :Game Scenes: All

   .. function:: TestClass object_property()
   .. function:: void set_object_property(TestClass value)



      :Game Scenes: All

   .. function:: std::string object_to_string(TestClass other)



      :Parameters:



      :Game Scenes: All

   .. function:: std::string optional_arguments(std::string x, std::string y = "foo", std::string z = "bar", TestClass obj = TestClass())



      :Parameters:






      :Game Scenes: All

   .. function:: static std::string static_method(Client& connection, std::string a = "", std::string b = "")



      :Parameters:




      :Game Scenes: All



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
