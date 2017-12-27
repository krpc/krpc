.. default-domain:: c
.. highlight:: c




Service TestService

   Service documentation string.

   .. function:: krpc_error_t krpc_TestService_AddMultipleValues(krpc_connection_t connection, char * * result, float x, int32_t y, int64_t z)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_AddToObjectList(krpc_connection_t connection, krpc_list_object_t * result, const krpc_list_object_t * l, const char * value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_BlockingProcedure(krpc_connection_t connection, int32_t * result, int32_t n, int32_t sum)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_BoolToString(krpc_connection_t connection, char * * result, bool value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_BytesToHexString(krpc_connection_t connection, char * * result, krpc_bytes_t value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_Counter(krpc_connection_t connection, int32_t * result, const char * id, int32_t divisor)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_CreateTestObject(krpc_connection_t connection, krpc_TestService_TestClass_t * result, const char * value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_DictionaryDefault(krpc_connection_t connection, krpc_dictionary_int32_bool_t * result, const krpc_dictionary_int32_bool_t * x)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_DoubleToString(krpc_connection_t connection, char * * result, double value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_EchoTestObject(krpc_connection_t connection, krpc_TestService_TestClass_t * result, krpc_TestService_TestClass_t value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_EnumDefaultArg(krpc_connection_t connection, krpc_TestService_TestEnum_t * result, krpc_TestService_TestEnum_t x)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_EnumEcho(krpc_connection_t connection, krpc_TestService_TestEnum_t * result, krpc_TestService_TestEnum_t x)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_EnumReturn(krpc_connection_t connection, krpc_TestService_TestEnum_t * result)

   .. function:: krpc_error_t krpc_TestService_FloatToString(krpc_connection_t connection, char * * result, float value)

      Procedure documentation string.

      :Parameters:

   .. function:: krpc_error_t krpc_TestService_IncrementDictionary(krpc_connection_t connection, krpc_dictionary_string_int32_t * result, const krpc_dictionary_string_int32_t * d)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_IncrementList(krpc_connection_t connection, krpc_list_int32_t * result, const krpc_list_int32_t * l)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_IncrementNestedCollection(krpc_connection_t connection, krpc_dictionary_string_list_int32_t * result, const krpc_dictionary_string_list_int32_t * d)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_IncrementSet(krpc_connection_t connection, krpc_set_int32_t * result, const krpc_set_int32_t * h)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_IncrementTuple(krpc_connection_t connection, krpc_tuple_int32_int64_t * result, const krpc_tuple_int32_int64_t * t)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_Int32ToString(krpc_connection_t connection, char * * result, int32_t value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_Int64ToString(krpc_connection_t connection, char * * result, int64_t value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_ListDefault(krpc_connection_t connection, krpc_list_int32_t * result, const krpc_list_int32_t * x)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_ObjectProperty(krpc_connection_t connection, krpc_TestService_TestClass_t * result)
   .. function:: void krpc_TestService_set_ObjectProperty(krpc_TestService_TestClass_t value)

   .. function:: krpc_error_t krpc_TestService_OnTimer(krpc_connection_t connection, krpc_schema_Event * result, uint32_t milliseconds, uint32_t repeats)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_OnTimerUsingLambda(krpc_connection_t connection, krpc_schema_Event * result, uint32_t milliseconds)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_OptionalArguments(krpc_connection_t connection, char * * result, const char * x, const char * y, const char * z, krpc_TestService_TestClass_t obj)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_ResetCustomExceptionLater(krpc_connection_t connection)

   .. function:: krpc_error_t krpc_TestService_ResetInvalidOperationExceptionLater(krpc_connection_t connection)

   .. function:: krpc_error_t krpc_TestService_ReturnNullWhenNotAllowed(krpc_connection_t connection, krpc_TestService_TestClass_t * result)

   .. function:: krpc_error_t krpc_TestService_SetDefault(krpc_connection_t connection, krpc_set_int32_t * result, const krpc_set_int32_t * x)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_StringProperty(krpc_connection_t connection, char * * result)
   .. function:: void krpc_TestService_set_StringProperty(const char * value)

      Property documentation string.

   .. function:: void krpc_TestService_set_StringPropertyPrivateGet(const char * value)

   .. function:: krpc_error_t krpc_TestService_StringPropertyPrivateSet(krpc_connection_t connection, char * * result)

   .. function:: krpc_error_t krpc_TestService_StringToInt32(krpc_connection_t connection, int32_t * result, const char * value)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_ThrowArgumentException(krpc_connection_t connection, int32_t * result)

   .. function:: krpc_error_t krpc_TestService_ThrowArgumentNullException(krpc_connection_t connection, int32_t * result, const char * foo)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_ThrowArgumentOutOfRangeException(krpc_connection_t connection, int32_t * result, int32_t foo)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_ThrowCustomException(krpc_connection_t connection, int32_t * result)

   .. function:: krpc_error_t krpc_TestService_ThrowCustomExceptionLater(krpc_connection_t connection, int32_t * result)

   .. function:: krpc_error_t krpc_TestService_ThrowInvalidOperationException(krpc_connection_t connection, int32_t * result)

   .. function:: krpc_error_t krpc_TestService_ThrowInvalidOperationExceptionLater(krpc_connection_t connection, int32_t * result)

   .. function:: krpc_error_t krpc_TestService_TupleDefault(krpc_connection_t connection, krpc_tuple_int32_bool_t * result, const krpc_tuple_int32_bool_t * x)



      :Parameters:



.. type:: krpc_TestService_TestClass_t

   Class documentation string.

   .. function:: krpc_error_t krpc_TestService_TestClass_FloatToString(krpc_connection_t connection, char * * result, float x)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_TestClass_GetValue(krpc_connection_t connection, char * * result)

      Method documentation string.

   .. function:: krpc_error_t krpc_TestService_TestClass_IntProperty(krpc_connection_t connection, int32_t * result)
   .. function:: void krpc_TestService_TestClass_set_IntProperty(int32_t value)

      Property documentation string.

   .. function:: krpc_error_t krpc_TestService_TestClass_ObjectProperty(krpc_connection_t connection, krpc_TestService_TestClass_t * result)
   .. function:: void krpc_TestService_TestClass_set_ObjectProperty(krpc_TestService_TestClass_t value)

   .. function:: krpc_error_t krpc_TestService_TestClass_ObjectToString(krpc_connection_t connection, char * * result, krpc_TestService_TestClass_t other)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_TestClass_OptionalArguments(krpc_connection_t connection, char * * result, const char * x, const char * y, const char * z, krpc_TestService_TestClass_t obj)



      :Parameters:

   .. function:: krpc_error_t krpc_TestService_TestClass_StaticMethod(krpc_connection_t connection, char * * result, const char * a, const char * b)



      :Parameters:



.. type:: krpc_TestService_TestEnum_t

   Enum documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEA

      Enum ValueA documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEB

      Enum ValueB documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEC

      Enum ValueC documentation string.



Exception class CustomException
