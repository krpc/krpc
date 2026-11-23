.. default-domain:: c
.. highlight:: c




Service documentation string.


.. function:: krpc_error_t krpc_TestService_AddMultipleValues(krpc_connection_t connection, char * * returnValue, float x, int32_t y, int64_t z)



   :Parameters:





   




.. function:: krpc_error_t krpc_TestService_AddToObjectDictionary(krpc_connection_t connection, krpc_dictionary_string_object_t * returnValue, const krpc_dictionary_string_object_t * d, const char * key)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_AddToObjectList(krpc_connection_t connection, krpc_list_object_t * returnValue, const krpc_list_object_t * l, const char * value)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_AddToObjectSet(krpc_connection_t connection, krpc_set_object_t * returnValue, const krpc_set_object_t * h, const char * value)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_BlockingProcedure(krpc_connection_t connection, int32_t * returnValue, int32_t n, int32_t sum)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_BoolToString(krpc_connection_t connection, char * * returnValue, bool value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_BytesDefault(krpc_connection_t connection, krpc_bytes_t * returnValue, krpc_bytes_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_BytesToHexString(krpc_connection_t connection, char * * returnValue, krpc_bytes_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Counter(krpc_connection_t connection, int32_t * returnValue, const char * id, int32_t divisor)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_CounterStruct(krpc_connection_t connection, krpc_TestService_TestStruct_t * returnValue, const char * id)

   Returns a struct whose IntField counts the number of times it has been called,
   so that a stream on it changes on every update.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_CreateDeprecatedClass(krpc_connection_t connection, krpc_TestService_DeprecatedClass_t * returnValue)

   .. warning:: Deprecated. Use :func:`krpc_TestService_CreateTestObject` instead.

   Create an instance of a deprecated class documentation string.


   




.. function:: krpc_error_t krpc_TestService_CreateObject(krpc_connection_t connection, krpc_TestService_Object_t * returnValue, const char * name)

   A new object of the class named Object.

   :Parameters:

    * **name** -- The name of the object.

   :returns: The object.
   




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

   




.. function:: krpc_error_t krpc_TestService_DeprecatedStructProcedure(krpc_connection_t connection, krpc_TestService_DeprecatedStruct_t * returnValue)

   .. warning:: Deprecated. Use :type:`krpc_TestService_TestStruct_t` instead.

   Procedure returning a deprecated struct documentation string.


   




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



   




.. function:: krpc_error_t krpc_TestService_EchoNullableBool(krpc_connection_t connection, bool * returnValue, bool * returnValueIsNull, const bool * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableBytes(krpc_connection_t connection, krpc_bytes_t * returnValue, bool * returnValueIsNull, const krpc_bytes_t * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableDictionary(krpc_connection_t connection, krpc_dictionary_string_int32_t * returnValue, bool * returnValueIsNull, const krpc_dictionary_string_int32_t * d)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableDouble(krpc_connection_t connection, double * returnValue, bool * returnValueIsNull, const double * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableEnum(krpc_connection_t connection, krpc_TestService_TestEnum_t * returnValue, bool * returnValueIsNull, const krpc_TestService_TestEnum_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableFlags(krpc_connection_t connection, krpc_TestService_TestFlags_t * returnValue, bool * returnValueIsNull, const krpc_TestService_TestFlags_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableFloat(krpc_connection_t connection, float * returnValue, bool * returnValueIsNull, const float * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableInt(krpc_connection_t connection, int32_t * returnValue, bool * returnValueIsNull, const int32_t * value)

   Returns the given value.

   :Parameters:

    * **value** -- The value, or ``nullptr``.

   :returns: The value.
   




.. function:: krpc_error_t krpc_TestService_EchoNullableList(krpc_connection_t connection, krpc_list_int32_t * returnValue, bool * returnValueIsNull, const krpc_list_int32_t * l)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableLong(krpc_connection_t connection, int64_t * returnValue, bool * returnValueIsNull, const int64_t * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableSet(krpc_connection_t connection, krpc_set_int32_t * returnValue, bool * returnValueIsNull, const krpc_set_int32_t * h)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableString(krpc_connection_t connection, char * * returnValue, bool * returnValueIsNull, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableTuple(krpc_connection_t connection, krpc_tuple_int32_int64_t * returnValue, bool * returnValueIsNull, const krpc_tuple_int32_int64_t * t)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableUInt(krpc_connection_t connection, uint32_t * returnValue, bool * returnValueIsNull, const uint32_t * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoNullableULong(krpc_connection_t connection, uint64_t * returnValue, bool * returnValueIsNull, const uint64_t * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoStringSet(krpc_connection_t connection, krpc_set_string_t * returnValue, const krpc_set_string_t * h)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoTestObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, krpc_TestService_TestClass_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EchoTupleWithANullableObject(krpc_connection_t connection, krpc_tuple_int32_nullable_object_t * returnValue, const krpc_tuple_int32_nullable_object_t * t)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EmptyListDefault(krpc_connection_t connection, krpc_list_string_t * returnValue, const krpc_list_string_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EnumDefault(krpc_connection_t connection, krpc_TestService_TestEnum_t * returnValue, krpc_TestService_TestEnum_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EnumEcho(krpc_connection_t connection, krpc_TestService_TestEnum_t * returnValue, krpc_TestService_TestEnum_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EnumListDefault(krpc_connection_t connection, krpc_list_enum_t * returnValue, const krpc_list_enum_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_EnumToString(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestEnum_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_FlagsDefault(krpc_connection_t connection, krpc_TestService_TestFlags_t * returnValue, krpc_TestService_TestFlags_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_FlagsEcho(krpc_connection_t connection, krpc_TestService_TestFlags_t * returnValue, krpc_TestService_TestFlags_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_FlagsListEcho(krpc_connection_t connection, krpc_list_enum_t * returnValue, const krpc_list_enum_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_FlagsStructEcho(krpc_connection_t connection, krpc_TestService_TestFlagsStruct_t * returnValue, const krpc_TestService_TestFlagsStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_FlagsToInt(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_TestFlags_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_FloatSpecialDefaults(krpc_connection_t connection, krpc_list_float_t * returnValue, float nan, float infinity, float negativeInfinity, float maximum, float lowest, float fraction)

   Procedure whose defaults are the special values of float, plus a finite fraction
   that no float can hold exactly.

   :Parameters:








   




.. function:: krpc_error_t krpc_TestService_FloatToString(krpc_connection_t connection, char * * returnValue, float value)

   Procedure documentation string.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_HexStringToBytes(krpc_connection_t connection, krpc_bytes_t * returnValue, const char * value)



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



   




.. function:: krpc_error_t krpc_TestService_IntToFlags(krpc_connection_t connection, krpc_TestService_TestFlags_t * returnValue, int32_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_JoinWithAtSign(krpc_connection_t connection, char * * returnValue, const char * firstValue, const char * from)

   Returns *firstValue* and *from* joined by an at
   sign, e.g. ``name@host``. The parameter names are:

   * a name in camel case, and
   * a name that is a keyword in some languages.

   :Parameters:

    * **firstValue** -- The string before the at sign.
    * **from** -- The string after the at sign.

   :returns: The strings joined.
   




.. function:: krpc_error_t krpc_TestService_ListDefault(krpc_connection_t connection, krpc_list_int32_t * returnValue, const krpc_list_int32_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_NestedStructEcho(krpc_connection_t connection, krpc_TestService_TestNestedStruct_t * returnValue, const krpc_TestService_TestNestedStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_NotNullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, krpc_TestService_TestClass_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_NullDefault(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t obj)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_NullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue)
.. function:: krpc_error_t krpc_TestService_set_NullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t value)



   




.. function:: krpc_error_t krpc_TestService_NullableStructEcho(krpc_connection_t connection, krpc_TestService_TestNullableStruct_t * returnValue, const krpc_TestService_TestNullableStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ObjectProperty(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue)
.. function:: krpc_error_t krpc_TestService_set_ObjectProperty(krpc_connection_t connection, krpc_TestService_TestClass_t value)

   A property holding an object.

   :returns: The object, or ``nullptr``.
   




.. function:: krpc_error_t krpc_TestService_OnTimer(krpc_connection_t connection, uint64_t * returnValue, uint32_t milliseconds, uint32_t repeats)



   :Parameters:




   




.. function:: krpc_error_t krpc_TestService_OptionalArguments(krpc_connection_t connection, char * * returnValue, const char * x, const char * y, const char * z, krpc_TestService_TestClass_t obj)



   :Parameters:






   




.. function:: krpc_error_t krpc_TestService_PartlyDeprecatedStructProcedure(krpc_connection_t connection, krpc_TestService_PartlyDeprecatedStruct_t * returnValue)

   Procedure returning a partly deprecated struct documentation string.


   




.. function:: krpc_error_t krpc_TestService_RemoveEventAfter(krpc_connection_t connection, uint64_t * returnValue, uint32_t milliseconds)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_RemoveTestObject(krpc_connection_t connection, krpc_TestService_TestClass_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ResetCustomExceptionLater(krpc_connection_t connection)




   




.. function:: krpc_error_t krpc_TestService_ResetInvalidOperationExceptionLater(krpc_connection_t connection)




   




.. function:: krpc_error_t krpc_TestService_ReturnNullWhenNotAllowed(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_ScalarDefault(krpc_connection_t connection, int32_t * returnValue, int32_t x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Service(krpc_connection_t connection, int32_t * returnValue)

   A procedure named Service, as the C# client names the class of a service.


   




.. function:: krpc_error_t krpc_TestService_SetDefault(krpc_connection_t connection, krpc_set_int32_t * returnValue, const krpc_set_int32_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringDefault(krpc_connection_t connection, char * * returnValue, const char * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringProperty(krpc_connection_t connection, char * * returnValue)
.. function:: krpc_error_t krpc_TestService_set_StringProperty(krpc_connection_t connection, const char * value)

   Property documentation string.

   




.. function:: krpc_error_t krpc_TestService_set_StringPropertyPrivateGet(krpc_connection_t connection, const char * value)



   




.. function:: krpc_error_t krpc_TestService_StringPropertyPrivateSet(krpc_connection_t connection, char * * returnValue)



   




.. function:: krpc_error_t krpc_TestService_StringToBool(krpc_connection_t connection, bool * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringToDouble(krpc_connection_t connection, double * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringToEnum(krpc_connection_t connection, krpc_TestService_TestEnum_t * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringToFloat(krpc_connection_t connection, float * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringToInt32(krpc_connection_t connection, int32_t * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringToInt64(krpc_connection_t connection, int64_t * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringToUInt32(krpc_connection_t connection, uint32_t * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StringToUInt64(krpc_connection_t connection, uint64_t * returnValue, const char * value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StructDefault(krpc_connection_t connection, krpc_TestService_TestStruct_t * returnValue, const krpc_TestService_TestStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StructEcho(krpc_connection_t connection, krpc_TestService_TestStruct_t * returnValue, const krpc_TestService_TestStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_StructEchoNullable(krpc_connection_t connection, krpc_TestService_TestStruct_t * returnValue, bool * returnValueIsNull, const krpc_TestService_TestStruct_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_SwapObjectTuple(krpc_connection_t connection, krpc_tuple_object_object_t * returnValue, const krpc_tuple_object_object_t * t)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ThrowArgumentException(krpc_connection_t connection, int32_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_ThrowArgumentNullException(krpc_connection_t connection, int32_t * returnValue, const char * foo)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ThrowArgumentOutOfRangeException(krpc_connection_t connection, int32_t * returnValue, int32_t foo)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_ThrowCustomException(krpc_connection_t connection, int32_t * returnValue)

   Throws a :macro:`KRPC_EXCEPTION_TESTSERVICE_CUSTOMEXCEPTION`.


   

   .. note::

      Links to each kind of member: TestService, :type:`krpc_TestService_TestClass_t`,
      :type:`krpc_TestService_TestEnum_t`, :type:`krpc_TestService_TestStruct_t`, :macro:`KRPC_EXCEPTION_KRPC_INVALIDOPERATIONEXCEPTION`,
      :func:`krpc_TestService_FloatToString`, :func:`krpc_TestService_StringProperty`,
      :func:`krpc_TestService_set_StringPropertyPrivateGet`, :func:`krpc_TestService_TestClass_FloatToString`,
      :func:`krpc_TestService_TestClass_StaticMethod`, :func:`krpc_TestService_TestClass_IntProperty`,
      :func:`krpc_TestService_TestClass_set_StringPropertyPrivateGet`, :macro:`KRPC_TESTSERVICE_TESTENUM_VALUEA` and
      :member:`krpc_TestService_TestStruct_t.int_field`.




.. function:: krpc_error_t krpc_TestService_ThrowCustomExceptionLater(krpc_connection_t connection, int32_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_ThrowInvalidOperationException(krpc_connection_t connection, int32_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_ThrowInvalidOperationExceptionLater(krpc_connection_t connection, int32_t * returnValue)




   




.. function:: krpc_error_t krpc_TestService_ThrowMappedException(krpc_connection_t connection, int32_t * returnValue, const char * name)

   Throws the system exception of the given name, which the server maps to the KRPC
   exception of that name.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_TupleDefault(krpc_connection_t connection, krpc_tuple_int32_bool_t * returnValue, const krpc_tuple_int32_bool_t * x)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_UInt32ToString(krpc_connection_t connection, char * * returnValue, uint32_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_UInt64ToString(krpc_connection_t connection, char * * returnValue, uint64_t value)



   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Uint32SpecialDefaults(krpc_connection_t connection, krpc_list_uint32_t * returnValue, uint32_t maximum)

   Procedure whose default is the largest uint.

   :Parameters:



   




.. function:: krpc_error_t krpc_TestService_Uint64SpecialDefaults(krpc_connection_t connection, krpc_list_uint64_t * returnValue, uint64_t maximum)

   Procedure whose default is the largest ulong.

   :Parameters:



   





.. type:: krpc_TestService_ByteString_t

   A class named ByteString.

   .. macro:: KRPC_CLASS_TESTSERVICE_BYTESTRING

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_ClassBase_t

   A class named ClassBase.

   .. macro:: KRPC_CLASS_TESTSERVICE_CLASSBASE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Client_t

   A class named Client.

   .. macro:: KRPC_CLASS_TESTSERVICE_CLIENT

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_ClientService_t

   A class named after the alias of the service in the generated JavaScript.

   .. macro:: KRPC_CLASS_TESTSERVICE_CLIENTSERVICE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Connection_t

   A class named Connection.

   .. macro:: KRPC_CLASS_TESTSERVICE_CONNECTION

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_DeprecatedClass_t

   .. warning:: Deprecated. Use :type:`krpc_TestService_TestClass_t` instead.

   Deprecated class documentation string.

   .. macro:: KRPC_CLASS_TESTSERVICE_DEPRECATEDCLASS

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.

   .. function:: krpc_error_t krpc_TestService_DeprecatedClass_DeprecatedMethod(krpc_connection_t connection, char * * returnValue, krpc_TestService_DeprecatedClass_t instance)

      .. warning:: Deprecated. Use :func:`krpc_TestService_TestClass_GetValue` instead.

      Deprecated method documentation string.


   

   .. function:: krpc_error_t krpc_TestService_DeprecatedClass_InheritedMethod(krpc_connection_t connection, char * * returnValue, krpc_TestService_DeprecatedClass_t instance)

      Method with no deprecation of its own documentation string.


   

   .. function:: krpc_error_t krpc_TestService_DeprecatedClass_InheritedProperty(krpc_connection_t connection, char * * returnValue, krpc_TestService_DeprecatedClass_t instance)
   .. function:: krpc_error_t krpc_TestService_DeprecatedClass_set_InheritedProperty(krpc_connection_t connection, krpc_TestService_DeprecatedClass_t instance, const char * value)

      Property with no deprecation of its own documentation string.

   

   .. function:: krpc_error_t krpc_TestService_DeprecatedClass_InheritedStaticMethod(krpc_connection_t connection, char * * returnValue)

      Static method with no deprecation of its own documentation string.


   



.. type:: krpc_TestService_DocEnum_t

   A class named DocEnum.

   .. macro:: KRPC_CLASS_TESTSERVICE_DOCENUM

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Encoder_t

   A class named Encoder.

   .. macro:: KRPC_CLASS_TESTSERVICE_ENCODER

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Event_t

   A class named Event.

   .. macro:: KRPC_CLASS_TESTSERVICE_EVENT

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Exception_t

   A class named Exception.

   .. macro:: KRPC_CLASS_TESTSERVICE_EXCEPTION

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_ExtensionMethods_t

   A class named after the class that reaches a service in the generated C# code.

   .. macro:: KRPC_CLASS_TESTSERVICE_EXTENSIONMETHODS

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_IConnection_t

   A class named IConnection.

   .. macro:: KRPC_CLASS_TESTSERVICE_ICONNECTION

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_IEquatable_t

   A class named IEquatable.

   .. macro:: KRPC_CLASS_TESTSERVICE_IEQUATABLE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_List_t

   A class named List.

   .. macro:: KRPC_CLASS_TESTSERVICE_LIST

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_NamedTuple_t

   A class named NamedTuple.

   .. macro:: KRPC_CLASS_TESTSERVICE_NAMEDTUPLE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Names_t

   A class whose members are named after names in the generated code.

   .. macro:: KRPC_CLASS_TESTSERVICE_NAMES

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.

   .. function:: krpc_error_t krpc_TestService_Names_Bind(krpc_connection_t connection, int32_t * returnValue)

      A static method named after the method that binds the static methods of a class
      to a connection.


   

   .. function:: krpc_error_t krpc_TestService_Names_Bound(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_Names_t instance)

      A method named after the class that holds the bound static methods of a class.


   

   .. function:: krpc_error_t krpc_TestService_Names_ClassOf(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_Names_t instance)

      A method named after a method of a Penlight class.


   

   .. function:: krpc_error_t krpc_TestService_Names_Float(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_Names_t instance)

      A method named after a Python builtin.


   

   .. function:: krpc_error_t krpc_TestService_Names_HashCode(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_Names_t instance)

      A method named after a method of java.lang.Object.


   

   .. function:: krpc_error_t krpc_TestService_Names_InstanceParameters(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_Names_t instance, int32_t client, int32_t connection, int32_t instance, int32_t returnValue, int32_t returnValueIsNull, int32_t NULL, int32_t krpc, int32_t java, int32_t com, int32_t org, int32_t nameof, int32_t self, int32_t cls, int32_t invoke, int32_t procedureCall)

      A method with parameters named after names in the generated code.

      :Parameters:

















   

   .. function:: krpc_error_t krpc_TestService_Names_Krpc(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_Names_t instance)

      A method named after the Python client package.


   

   .. function:: krpc_error_t krpc_TestService_Names_MakeStruct(krpc_connection_t connection, krpc_TestService_NamesStruct_t * returnValue, int32_t bound)

      A structure with the given value in its field named Bound.

      :Parameters:

       * **bound** -- The value.

   

   .. function:: krpc_error_t krpc_TestService_Names_Parameters(krpc_connection_t connection, int32_t * returnValue, int32_t client, int32_t connection, int32_t instance, int32_t returnValue, int32_t returnValueIsNull, int32_t NULL, int32_t krpc, int32_t java, int32_t com, int32_t org, int32_t nameof, int32_t self, int32_t cls, int32_t invoke, int32_t procedureCall)

      A static method with parameters named after names in the generated code.

      :Parameters:

















   

   .. function:: krpc_error_t krpc_TestService_Names_Property(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_Names_t instance)
   .. function:: krpc_error_t krpc_TestService_Names_set_Property(krpc_connection_t connection, krpc_TestService_Names_t instance, int32_t value)

      A property named after a Python builtin.

   

   .. function:: krpc_error_t krpc_TestService_Names_ToString(krpc_connection_t connection, char * * returnValue, krpc_TestService_Names_t instance, int32_t value)

      A method named after a method of java.lang.Object.

      :Parameters:



   

   .. function:: krpc_error_t krpc_TestService_Names_Uint64T(krpc_connection_t connection, int32_t * returnValue)

      A method named after a C++ integer type.


   

   .. function:: krpc_error_t krpc_TestService_Names_Value(krpc_connection_t connection, int32_t * returnValue, krpc_TestService_Names_t instance)
   .. function:: krpc_error_t krpc_TestService_Names_set_Value(krpc_connection_t connection, krpc_TestService_Names_t instance, int32_t value)

      A property declared after the property named Property.

   

   .. function:: krpc_error_t krpc_TestService_Names_WithDefaults(krpc_connection_t connection, double * returnValue, krpc_TestService_Names_t instance, double x, int32_t y)

      A method declared after methods named after Python builtins, with defaults that
      name them.

      :Parameters:




   



.. type:: krpc_TestService_None_t

   A class named after a keyword of Python.

   .. macro:: KRPC_CLASS_TESTSERVICE_NONE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Object_t

   A class named Object.

   .. macro:: KRPC_CLASS_TESTSERVICE_OBJECT

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.

   .. function:: krpc_error_t krpc_TestService_Object_Name(krpc_connection_t connection, char * * returnValue, krpc_TestService_Object_t instance)

      The name of the object.

   



.. type:: krpc_TestService_Optional_t

   A class named Optional.

   .. macro:: KRPC_CLASS_TESTSERVICE_OPTIONAL

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Override_t

   A class named Override.

   .. macro:: KRPC_CLASS_TESTSERVICE_OVERRIDE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_RPCException_t

   A class named RPCException.

   .. macro:: KRPC_CLASS_TESTSERVICE_RPCEXCEPTION

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_RemoteObject_t

   A class named RemoteObject.

   .. macro:: KRPC_CLASS_TESTSERVICE_REMOTEOBJECT

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Serializable_t

   A class named Serializable.

   .. macro:: KRPC_CLASS_TESTSERVICE_SERIALIZABLE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Server_t

   A class named after the namespace of server forms in the generated JavaScript.

   .. macro:: KRPC_CLASS_TESTSERVICE_SERVER

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Service_t

   A class named after the class a generated module or namespace declares for its service.

   .. macro:: KRPC_CLASS_TESTSERVICE_SERVICE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Stream_t

   A class named Stream.

   .. macro:: KRPC_CLASS_TESTSERVICE_STREAM

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_String_t

   A class named String.

   .. macro:: KRPC_CLASS_TESTSERVICE_STRING

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_TestClass_t

   Class documentation string.

   .. macro:: KRPC_CLASS_TESTSERVICE_TESTCLASS

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.

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

      A property of the object, holding another object.

      :returns: The other object, or ``nullptr``.
   

   .. function:: krpc_error_t krpc_TestService_TestClass_ObjectToString(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t instance, krpc_TestService_TestClass_t other)



      :Parameters:



   

   .. function:: krpc_error_t krpc_TestService_TestClass_OptionalArguments(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t instance, const char * x, const char * y, const char * z, krpc_TestService_TestClass_t obj)



      :Parameters:






   

   .. function:: krpc_error_t krpc_TestService_TestClass_StaticMethod(krpc_connection_t connection, char * * returnValue, const char * a, const char * b)

      Returns static followed by *a* and *b*.

      :Parameters:

       * **a** -- The first string.
       * **b** -- The second string.

      :returns: The strings joined.
   

   .. function:: krpc_error_t krpc_TestService_TestClass_StaticNullableObject(krpc_connection_t connection, krpc_TestService_TestClass_t * returnValue, krpc_TestService_TestClass_t value)



      :Parameters:



   

   .. function:: krpc_error_t krpc_TestService_TestClass_StaticOptionalArguments(krpc_connection_t connection, char * * returnValue, const char * x, const char * y, const char * z, krpc_TestService_TestClass_t obj)



      :Parameters:






   

   .. function:: krpc_error_t krpc_TestService_TestClass_set_StringPropertyPrivateGet(krpc_connection_t connection, krpc_TestService_TestClass_t instance, const char * value)



   

   .. function:: krpc_error_t krpc_TestService_TestClass_StringPropertyPrivateSet(krpc_connection_t connection, char * * returnValue, krpc_TestService_TestClass_t instance)



   



.. type:: krpc_TestService_TestServiceConnection_t

   A class named after the connection type of the generated Python module.

   .. macro:: KRPC_CLASS_TESTSERVICE_TESTSERVICECONNECTION

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_Tuple_t

   A class named Tuple.

   .. macro:: KRPC_CLASS_TESTSERVICE_TUPLE

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_TypeSpecs_t

   A class named TypeSpecs.

   .. macro:: KRPC_CLASS_TESTSERVICE_TYPESPECS

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_UInt64_t

   A class named UInt64.

   .. macro:: KRPC_CLASS_TESTSERVICE_UINT64

      The service and class ids of the class, for :func:`krpc_KRPC_Type_ClassTypeById`.



.. type:: krpc_TestService_DeprecatedEnum_t

   .. warning:: Deprecated. Use :type:`krpc_TestService_TestEnum_t` instead.

   Deprecated enum documentation string.

   .. macro:: KRPC_ENUMERATION_TESTSERVICE_DEPRECATEDENUM

      The service and enumeration ids of the enumeration, for
      :func:`krpc_KRPC_Type_EnumerationTypeById` and :func:`krpc_KRPC_Expression_ConstantEnumById`.


   .. macro:: KRPC_TESTSERVICE_DEPRECATEDENUM_VALUEA

      Deprecated enum ValueA documentation string.


   .. macro:: KRPC_TESTSERVICE_DEPRECATEDENUM_VALUEB

      .. warning:: Deprecated. Use :macro:`KRPC_TESTSERVICE_DEPRECATEDENUM_VALUEA` instead.

      Deprecated enum ValueB documentation string.



.. type:: krpc_TestService_NamesEnum_t

   An enumeration whose values are named after names in the generated code.

   .. macro:: KRPC_ENUMERATION_TESTSERVICE_NAMESENUM

      The service and enumeration ids of the enumeration, for
      :func:`krpc_KRPC_Type_EnumerationTypeById` and :func:`krpc_KRPC_Expression_ConstantEnumById`.


   .. macro:: KRPC_TESTSERVICE_NAMESENUM_MRO

      A value named after the method of a Python class that Enum reserves.


   .. macro:: KRPC_TESTSERVICE_NAMESENUM_TOSTRING

      A value named after a member of a JavaScript object.



.. type:: krpc_TestService_PartlyDeprecatedEnum_t

   Partly deprecated enum documentation string.

   .. macro:: KRPC_ENUMERATION_TESTSERVICE_PARTLYDEPRECATEDENUM

      The service and enumeration ids of the enumeration, for
      :func:`krpc_KRPC_Type_EnumerationTypeById` and :func:`krpc_KRPC_Expression_ConstantEnumById`.


   .. macro:: KRPC_TESTSERVICE_PARTLYDEPRECATEDENUM_VALUEA

      Partly deprecated enum ValueA documentation string.


   .. macro:: KRPC_TESTSERVICE_PARTLYDEPRECATEDENUM_VALUEB

      .. warning:: Deprecated. Use :macro:`KRPC_TESTSERVICE_PARTLYDEPRECATEDENUM_VALUEA` instead.

      Partly deprecated enum ValueB documentation string.



.. type:: krpc_TestService_TestEnum_t

   Enum documentation string.

   .. macro:: KRPC_ENUMERATION_TESTSERVICE_TESTENUM

      The service and enumeration ids of the enumeration, for
      :func:`krpc_KRPC_Type_EnumerationTypeById` and :func:`krpc_KRPC_Expression_ConstantEnumById`.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEA

      Enum ValueA documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEB

      Enum ValueB documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTENUM_VALUEC

      Enum ValueC documentation string, in :math:`\mathrm{m/s^{2}}`, equal to
      :math:`\frac{1}{2} \rho v^{2}`.



.. type:: krpc_TestService_TestFlags_t

   Flags documentation string.

   A flags enumeration. :std:ref:`cnano-client-flags` shows how to combine its values.

   .. macro:: KRPC_ENUMERATION_TESTSERVICE_TESTFLAGS

      The service and enumeration ids of the enumeration, for
      :func:`krpc_KRPC_Type_EnumerationTypeById` and :func:`krpc_KRPC_Expression_ConstantEnumById`.


   .. macro:: KRPC_TESTSERVICE_TESTFLAGS_NONE

      Flags None documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTFLAGS_VALUEA

      Flags ValueA documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTFLAGS_VALUEAB

      Flags ValueAB documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTFLAGS_VALUEB

      Flags ValueB documentation string.


   .. macro:: KRPC_TESTSERVICE_TESTFLAGS_VALUEC

      Flags ValueC documentation string.



.. type:: krpc_TestService_DeprecatedStruct_t

   .. warning:: Deprecated. Use :type:`krpc_TestService_TestStruct_t` instead.

   Deprecated struct documentation string.

   .. macro:: KRPC_STRUCT_TESTSERVICE_DEPRECATEDSTRUCT

      The service and structure ids of the structure, for :func:`krpc_KRPC_Type_StructTypeById`.


   .. member:: int32_t value

      Deprecated struct Value documentation string.

      .. macro:: KRPC_FIELD_TESTSERVICE_DEPRECATEDSTRUCT_VALUE

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: int32_t old_value

      .. warning:: Deprecated. Use :member:`krpc_TestService_DeprecatedStruct_t.value` instead.

      Deprecated struct OldValue documentation string.

      .. macro:: KRPC_FIELD_TESTSERVICE_DEPRECATEDSTRUCT_OLDVALUE

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.



.. type:: krpc_TestService_NamesStruct_t

   A structure whose fields are named after names in the generated code.

   .. macro:: KRPC_STRUCT_TESTSERVICE_NAMESSTRUCT

      The service and structure ids of the structure, for :func:`krpc_KRPC_Type_StructTypeById`.


   .. member:: int32_t other

      A value.

      .. macro:: KRPC_FIELD_TESTSERVICE_NAMESSTRUCT_OTHER

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: int32_t result

      A value.

      .. macro:: KRPC_FIELD_TESTSERVICE_NAMESSTRUCT_RESULT

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: int32_t java

      A value.

      .. macro:: KRPC_FIELD_TESTSERVICE_NAMESSTRUCT_JAVA

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: int32_t krpc

      A value.

      .. macro:: KRPC_FIELD_TESTSERVICE_NAMESSTRUCT_KRPC

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: int32_t class

      A value.

      .. macro:: KRPC_FIELD_TESTSERVICE_NAMESSTRUCT_CLASS

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: int32_t bound

      A value named after the class of static methods bound to a connection.

      .. macro:: KRPC_FIELD_TESTSERVICE_NAMESSTRUCT_BOUND

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.



.. type:: krpc_TestService_PartlyDeprecatedStruct_t

   Partly deprecated struct documentation string.

   .. macro:: KRPC_STRUCT_TESTSERVICE_PARTLYDEPRECATEDSTRUCT

      The service and structure ids of the structure, for :func:`krpc_KRPC_Type_StructTypeById`.


   .. member:: int32_t value

      Partly deprecated struct Value documentation string.

      .. macro:: KRPC_FIELD_TESTSERVICE_PARTLYDEPRECATEDSTRUCT_VALUE

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: int32_t old_value

      .. warning:: Deprecated. Use :member:`krpc_TestService_PartlyDeprecatedStruct_t.value` instead.

      Partly deprecated struct OldValue documentation string.

      .. macro:: KRPC_FIELD_TESTSERVICE_PARTLYDEPRECATEDSTRUCT_OLDVALUE

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.



.. type:: krpc_TestService_TestFlagsStruct_t

   Flags struct documentation string.

   .. macro:: KRPC_STRUCT_TESTSERVICE_TESTFLAGSSTRUCT

      The service and structure ids of the structure, for :func:`krpc_KRPC_Type_StructTypeById`.


   .. member:: krpc_TestService_TestFlags_t flags_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTFLAGSSTRUCT_FLAGSFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.



.. type:: krpc_TestService_TestNestedNullableStruct_t

   Nested nullable struct documentation string.

   .. macro:: KRPC_STRUCT_TESTSERVICE_TESTNESTEDNULLABLESTRUCT

      The service and structure ids of the structure, for :func:`krpc_KRPC_Type_StructTypeById`.


   .. member:: krpc_list_nullable_object_t list_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNESTEDNULLABLESTRUCT_LISTFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: char * string_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNESTEDNULLABLESTRUCT_STRINGFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.



.. type:: krpc_TestService_TestNestedStruct_t

   Nested struct documentation string.

   .. macro:: KRPC_STRUCT_TESTSERVICE_TESTNESTEDSTRUCT

      The service and structure ids of the structure, for :func:`krpc_KRPC_Type_StructTypeById`.


   .. member:: krpc_TestService_TestStruct_t struct_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNESTEDSTRUCT_STRUCTFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: krpc_TestService_TestClass_t object_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNESTEDSTRUCT_OBJECTFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: char * string_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNESTEDSTRUCT_STRINGFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.



.. type:: krpc_TestService_TestNullableStruct_t

   Nullable struct documentation string.

   .. macro:: KRPC_STRUCT_TESTSERVICE_TESTNULLABLESTRUCT

      The service and structure ids of the structure, for :func:`krpc_KRPC_Type_StructTypeById`.


   .. member:: int32_t int_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNULLABLESTRUCT_INTFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: krpc_nullable_int32_t nullable_int_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNULLABLESTRUCT_NULLABLEINTFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: krpc_nullable_enum_t nullable_enum_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNULLABLESTRUCT_NULLABLEENUMFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: krpc_nullable_string_t nullable_string_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNULLABLESTRUCT_NULLABLESTRINGFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: krpc_nullable_object_t nullable_object_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTNULLABLESTRUCT_NULLABLEOBJECTFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.



.. type:: krpc_TestService_TestStruct_t

   Struct documentation string.

   .. macro:: KRPC_STRUCT_TESTSERVICE_TESTSTRUCT

      The service and structure ids of the structure, for :func:`krpc_KRPC_Type_StructTypeById`.


   .. member:: int32_t int_field

      Struct IntField documentation string.

      .. macro:: KRPC_FIELD_TESTSERVICE_TESTSTRUCT_INTFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: char * string_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTSTRUCT_STRINGFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: krpc_TestService_TestEnum_t enum_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTSTRUCT_ENUMFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.


   .. member:: krpc_list_int32_t list_field



      .. macro:: KRPC_FIELD_TESTSERVICE_TESTSTRUCT_LISTFIELD

         The position of the field, for :func:`krpc_KRPC_Expression_GetFieldByIndex`.



.. macro:: KRPC_EXCEPTION_TESTSERVICE_CUSTOMEXCEPTION



   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_TESTSERVICE_DEPRECATEDEXCEPTION

   .. warning:: Deprecated. Use :macro:`KRPC_EXCEPTION_TESTSERVICE_CUSTOMEXCEPTION` instead.

   Deprecated exception documentation string.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.



.. macro:: KRPC_EXCEPTION_TESTSERVICE_RUNTIMEERROR

   An exception named RuntimeError.

   The service and exception ids of the exception, for :macro:`KRPC_IS_EXCEPTION`. A call
   that fails with it returns :macro:`KRPC_ERROR_SERVER`.
