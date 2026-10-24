.. default-domain:: c
.. highlight:: c




Service documentation string.


.. function:: krpc_error_t krpc_TestService_AddMultipleValues(krpc_connection_t connection, char * * returnValue, float x, int32_t y, int64_t z)



   :Parameters:





   




.. function:: krpc_error_t krpc_TestService_AddToObjectList(krpc_connection_t connection, krpc_list_object_t * returnValue, const krpc_list_object_t * l, const char * value)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_BlockingProcedure(krpc_connection_t connection, int32_t * returnValue, int32_t n, int32_t sum)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_BoolToString(krpc_connection_t connection, char * * returnValue, bool value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_BytesToHexString(krpc_connection_t connection, char * * returnValue, krpc_bytes_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Counter(krpc_connection_t connection, int32_t * returnValue, const char * id, int32_t divisor)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_CounterStruct(krpc_connection_t connection, krpc_TestService_TestStruct_t * returnValue, const char * id)

   Returns a struct whose IntField counts the number of times it has been called,
   so that a stream on it changes on every update.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_CreateTestObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_DeprecatedProcedure(krpc_connection_t connection, char * * returnValue, float value)

   .. warning:: Deprecated. Use :func:`krpc_TestService_FloatToString` instead.

   Deprecated procedure documentation string.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_DeprecatedProcedureNoMessage(krpc_connection_t connection, char * * returnValue, float value)

   .. warning:: Deprecated.

   Deprecated procedure with no reason documentation string.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_DeprecatedProperty(krpc_connection_t connection, char * * returnValue)
.. function:: krpc_error_t krpc_TestService_set_DeprecatedProperty(krpc_connection_t connection, const char * value)

   .. warning:: Deprecated. Use :func:`krpc_TestService_StringProperty` instead.

   Deprecated property documentation string.

   




.. function:: krpc_error_t krpc_TestService_DictionaryDefault(krpc_connection_t connection, krpc_dictionary_int32_bool_t * returnValue, const krpc_dictionary_int32_bool_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_DoubleSpecialDefaults(krpc_connection_t connection, krpc_list_double_t * returnValue, double nan, double infinity, double negativeInfinity, double maximum, double lowest)

   Procedure whose defaults are the special values of double.

   :Parameters:







   




.. function:: krpc_error_t krpc_TestService_DoubleToString(krpc_connection_t connection, char * * returnValue, double value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_DumpExpressionTree(krpc_connection_t connection, char * * returnValue, krpc_KRPC_Expression_t expression)

   The expression tree of a function, as text.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoDictionaryOfNullableObjects(krpc_connection_t connection, krpc_dictionary_string_nullable_object_t * returnValue, const krpc_dictionary_string_nullable_object_t * d)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoListOfNullableInts(krpc_connection_t connection, krpc_list_nullable_int32_t * returnValue, const krpc_list_nullable_int32_t * l)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoListOfNullableObjects(krpc_connection_t connection, krpc_list_nullable_object_t * returnValue, const krpc_list_nullable_object_t * l)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNestedListOfNullableObjects(krpc_connection_t connection, krpc_list_list_nullable_object_t * returnValue, const krpc_list_list_nullable_object_t * l)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNestedNullableStruct(krpc_connection_t connection, krpc_TestService_TestNestedNullableStruct_t * returnValue, const krpc_TestService_TestNestedNullableStruct_t * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableInt(krpc_connection_t connection, int32_t * returnValue, bool * returnValueIsNull, const int32_t * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableList(krpc_connection_t connection, krpc_list_int32_t * returnValue, bool * returnValueIsNull, const krpc_list_int32_t * l)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableString(krpc_connection_t connection, char * * returnValue, bool * returnValueIsNull, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoTestObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, krpc_TestService_TestClass_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoTupleWithANullableObject(krpc_connection_t connection, krpc_tuple_int32_nullable_object_t * returnValue, const krpc_tuple_int32_nullable_object_t * t)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EmptyListDefault(krpc_connection_t connection, krpc_list_string_t * returnValue, const krpc_list_string_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EnumDefaultArg(krpc_connection_t connection, krpc_TestService_TestEnum_t * returnValue, krpc_TestService_TestEnum_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EnumEcho(krpc_connection_t connection, krpc_TestService_TestEnum_t * returnValue, krpc_TestService_TestEnum_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EnumListDefault(krpc_connection_t connection, krpc_list_enum_t * returnValue, const krpc_list_enum_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EnumReturn(krpc_connection_t connection, krpc_TestService_TestEnum_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_FloatSpecialDefaults(krpc_connection_t connection, krpc_list_float_t * returnValue, float nan, float infinity, float negativeInfinity, float maximum, float lowest, float fraction)

   Procedure whose defaults are the special values of float, plus a finite fraction
   that no float can hold exactly.

   :Parameters:








   




.. function:: krpc_error_t krpc_TestService_FloatToString(krpc_connection_t connection, char * * returnValue, float value)

   Procedure documentation string.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_IncrementDictionary(krpc_connection_t connection, krpc_dictionary_string_int32_t * returnValue, const krpc_dictionary_string_int32_t * d)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_IncrementList(krpc_connection_t connection, krpc_list_int32_t * returnValue, const krpc_list_int32_t * l)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_IncrementListOfStructs(krpc_connection_t connection, krpc_list_TestService_TestStruct_t * returnValue, const krpc_list_TestService_TestStruct_t * l)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_IncrementNestedCollection(krpc_connection_t connection, krpc_dictionary_string_list_int32_t * returnValue, const krpc_dictionary_string_list_int32_t * d)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_IncrementSet(krpc_connection_t connection, krpc_set_int32_t * returnValue, const krpc_set_int32_t * h)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_IncrementTuple(krpc_connection_t connection, krpc_tuple_int32_int64_t * returnValue, const krpc_tuple_int32_int64_t * t)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Int32SpecialDefaults(krpc_connection_t connection, krpc_list_int32_t * returnValue, int32_t maximum, int32_t minimum)

   Procedure whose defaults are the extremes of int.

   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_Int32ToString(krpc_connection_t connection, char * * returnValue, int32_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Int64SpecialDefaults(krpc_connection_t connection, krpc_list_int64_t * returnValue, int64_t maximum, int64_t minimum)

   Procedure whose defaults are the extremes of long.

   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_Int64ToString(krpc_connection_t connection, char * * returnValue, int64_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ListDefault(krpc_connection_t connection, krpc_list_int32_t * returnValue, const krpc_list_int32_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_NestedStructEcho(krpc_connection_t connection, krpc_TestService_TestNestedStruct_t * returnValue, const krpc_TestService_TestNestedStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_NotNullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, krpc_TestService_TestClass_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_NullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue)
.. function:: krpc_error_t krpc_TestService_set_NullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t value)



   




.. function:: krpc_error_t krpc_TestService_NullableStructEcho(krpc_connection_t connection, krpc_TestService_TestNullableStruct_t * returnValue, const krpc_TestService_TestNullableStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ObjectProperty(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue)
.. function:: krpc_error_t krpc_TestService_set_ObjectProperty(krpc_connection_t connection, krpc_TestService_TestClass_t value)



   




.. function:: krpc_error_t krpc_TestService_OnTimer(krpc_connection_t connection, krpc_schema_Event * returnValue, uint32_t milliseconds, uint32_t repeats)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_OnTimerUsingLambda(krpc_connection_t connection, krpc_schema_Event * returnValue, uint32_t milliseconds)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_OptionalArguments(krpc_connection_t connection, char * * returnValue, const char * x, const char * y, const char * z, krpc_TestService_TestClass_t obj)



   :Parameters:






   




.. function:: krpc_error_t krpc_TestService_RemoveEventAfter(krpc_connection_t connection, krpc_schema_Event * returnValue, uint32_t milliseconds)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ResetCustomExceptionLater(krpc_connection_t connection)




   




.. function:: krpc_error_t krpc_TestService_ResetInvalidOperationExceptionLater(krpc_connection_t connection)




   




.. function:: krpc_error_t krpc_TestService_ReturnNullWhenNotAllowed(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_SetDefault(krpc_connection_t connection, krpc_set_int32_t * returnValue, const krpc_set_int32_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringProperty(krpc_connection_t connection, char * * returnValue)
.. function:: krpc_error_t krpc_TestService_set_StringProperty(krpc_connection_t connection, const char * value)

   Property documentation string.

   




.. function:: krpc_error_t krpc_TestService_set_StringPropertyPrivateGet(krpc_connection_t connection, const char * value)



   




.. function:: krpc_error_t krpc_TestService_StringPropertyPrivateSet(krpc_connection_t connection, char * * returnValue)



   




.. function:: krpc_error_t krpc_TestService_StringToInt32(krpc_connection_t connection, int32_t * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StructDefault(krpc_connection_t connection, krpc_TestService_TestStruct_t * returnValue, const krpc_TestService_TestStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StructEcho(krpc_connection_t connection, krpc_TestService_TestStruct_t * returnValue, const krpc_TestService_TestStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StructEchoNullable(krpc_connection_t connection, krpc_TestService_TestStruct_t * returnValue, bool * returnValueIsNull, const krpc_TestService_TestStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ThrowArgumentException(krpc_connection_t connection, int32_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_ThrowArgumentNullException(krpc_connection_t connection, int32_t * returnValue, const char * foo)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ThrowArgumentOutOfRangeException(krpc_connection_t connection, int32_t * returnValue, int32_t foo)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ThrowCustomException(krpc_connection_t connection, int32_t * returnValue)

   Throws a ``TestService.CustomException``.


   




.. function:: krpc_error_t krpc_TestService_ThrowCustomExceptionLater(krpc_connection_t connection, int32_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_ThrowInvalidOperationException(krpc_connection_t connection, int32_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_ThrowInvalidOperationExceptionLater(krpc_connection_t connection, int32_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_TupleDefault(krpc_connection_t connection, krpc_tuple_int32_bool_t * returnValue, const krpc_tuple_int32_bool_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Uint32SpecialDefaults(krpc_connection_t connection, krpc_list_uint32_t * returnValue, uint32_t maximum)

   Procedure whose default is the largest uint.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Uint64SpecialDefaults(krpc_connection_t connection, krpc_list_uint64_t * returnValue, uint64_t maximum)

   Procedure whose default is the largest ulong.

   :Parameters:



   





.. type:: krpc_TestService_TestClass_t

   Class documentation string.

   .. function:: krpc_error_t krpc_TestService_TestClass_EchoNullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, krpc_TestService_TestClass_t instance, krpc_TestService_TestClass_t value)



      :Parameters:



   

   .. function:: krpc_error_t krpc_TestService_TestClass_FloatToString(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t instance, float x)



      :Parameters:



   

   .. function:: krpc_error_t krpc_TestService_TestClass_GetValue(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t instance)

      Method documentation string.


   

   .. function:: krpc_error_t krpc_TestService_TestClass_IntProperty(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_TestClass_t instance)
   .. function:: krpc_error_t krpc_TestService_TestClass_set_IntProperty(krpc_connection_t connection, krpc_TestService_TestClass_t instance, int32_t value)

      Property documentation string.

   

   .. function:: krpc_error_t krpc_TestService_TestClass_ObjectProperty(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, krpc_TestService_TestClass_t instance)
   .. function:: krpc_error_t krpc_TestService_TestClass_set_ObjectProperty(krpc_connection_t connection, krpc_TestService_TestClass_t instance, krpc_TestService_TestClass_t value)



   

   .. function:: krpc_error_t krpc_TestService_TestClass_ObjectToString(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t instance, krpc_TestService_TestClass_t other)



      :Parameters:



   

   .. function:: krpc_error_t krpc_TestService_TestClass_OptionalArguments(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t instance, const char * x, const char * y, const char * z, krpc_TestService_TestClass_t obj)



      :Parameters:






   

   .. function:: krpc_error_t krpc_TestService_TestClass_StaticMethod(krpc_connection_t connection, char * * returnValue, const char * a, const char * b)



      :Parameters:




   

   .. function:: krpc_error_t krpc_TestService_TestClass_StaticNullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, krpc_TestService_TestClass_t value)



      :Parameters:



   

   .. function:: krpc_error_t krpc_TestService_TestClass_set_StringPropertyPrivateGet(krpc_connection_t connection, krpc_TestService_TestClass_t instance, const char * value)



   

   .. function:: krpc_error_t krpc_TestService_TestClass_StringPropertyPrivateSet(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t instance)



   



.. type:: krpc_TestService_DeprecatedClass_t

   .. warning:: Deprecated. Use :type:`krpc_TestService_TestClass_t` instead.

   Deprecated class documentation string.

   .. function:: krpc_error_t krpc_TestService_DeprecatedClass_DeprecatedMethod(krpc_connection_t connection, char * * returnValue, krpc_TestService_DeprecatedClass_t instance)

      .. warning:: Deprecated. Use :func:`krpc_TestService_TestClass_GetValue` instead.

      Deprecated method documentation string.


   



.. type:: krpc_TestService_TestEnum_t

   Enum documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEA

      Enum ValueA documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEB

      Enum ValueB documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEC

      Enum ValueC documentation string, in :math:`\mathrm{m/s^{2}}`, equal to
      :math:`\frac{1}{2} \rho v^{2}`.



.. type:: krpc_TestService_DeprecatedEnum_t

   .. warning:: Deprecated. Use :type:`krpc_TestService_TestEnum_t` instead.

   Deprecated enum documentation string.


   .. macro:: KRPC_TESTSERVICE_DEPRECATEDENUM_VALUEA

      Deprecated enum ValueA documentation string.


   .. macro:: KRPC_TESTSERVICE_DEPRECATEDENUM_VALUEB

      .. warning:: Deprecated. Use :macro:`KRPC_TESTSERVICE_DEPRECATEDENUM_VALUEA` instead.

      Deprecated enum ValueB documentation string.



.. type:: krpc_TestService_TestStruct_t

   Struct documentation string.


   .. member:: int32_t int_field

      Struct IntField documentation string.

   .. member:: char * string_field



   .. member:: krpc_TestService_TestEnum_t enum_field



   .. member:: krpc_list_int32_t list_field




.. type:: krpc_TestService_TestNestedStruct_t

   Nested struct documentation string.


   .. member:: krpc_TestService_TestStruct_t struct_field



   .. member:: krpc_TestService_TestClass_t object_field



   .. member:: char * string_field




.. type:: krpc_TestService_TestNullableStruct_t

   Nullable struct documentation string.


   .. member:: int32_t int_field



   .. member:: krpc_nullable_int32_t nullable_int_field



   .. member:: krpc_nullable_enum_t nullable_enum_field



   .. member:: krpc_nullable_string_t nullable_string_field



   .. member:: krpc_nullable_object_t nullable_object_field




.. type:: krpc_TestService_TestNestedNullableStruct_t

   Nested nullable struct documentation string.


   .. member:: krpc_list_nullable_object_t list_field



   .. member:: char * string_field




.. type:: krpc_TestService_DeprecatedStruct_t

   .. warning:: Deprecated. Use :type:`krpc_TestService_TestStruct_t` instead.

   Deprecated struct documentation string.


   .. member:: int32_t value

      Deprecated struct Value documentation string.

   .. member:: int32_t old_value

      .. warning:: Deprecated. Use :member:`krpc_TestService_DeprecatedStruct_t.value` instead.

      Deprecated struct OldValue documentation string.


Exception class CustomException





Exception class DeprecatedException

.. warning:: Deprecated. Use ``TestService.CustomException`` instead.

Deprecated exception documentation string.
