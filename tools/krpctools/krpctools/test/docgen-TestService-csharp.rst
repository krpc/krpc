.. default-domain:: cs
.. highlight:: csharp

.. namespace:: KRPC.Client.Services.TestService


.. class:: TestService

   Service documentation string.

   .. function:: string AddMultipleValues(float x, int y, long z)



      :parameters:





      :Game Scenes: All

   .. function:: System.Collections.Generic.IDictionary<string,TestClass> AddToObjectDictionary(System.Collections.Generic.IDictionary<string,TestClass> d, string key)



      :parameters:




      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<TestClass> AddToObjectList(System.Collections.Generic.IList<TestClass> l, string value)



      :parameters:




      :Game Scenes: All

   .. function:: System.Collections.Generic.ISet<TestClass> AddToObjectSet(System.Collections.Generic.ISet<TestClass> h, string value)



      :parameters:




      :Game Scenes: All

   .. function:: int BlockingProcedure(int n, int sum = 0)



      :parameters:




      :Game Scenes: All

   .. function:: string BoolToString(bool value)



      :parameters:



      :Game Scenes: All

   .. function:: byte[] BytesDefault(byte[] x = null)



      :parameters:

       * **x** -- If null, defaults to ``new byte[] { 0xca, 0xfe }``.

      :Game Scenes: All

   .. function:: string BytesToHexString(byte[] value)



      :parameters:



      :Game Scenes: All

   .. function:: int Counter(string id = "", int divisor = 1)



      :parameters:




      :Game Scenes: All

   .. function:: TestStruct CounterStruct(string id = "")

      Returns a struct whose IntField counts the number of times it has been called,
      so that a stream on it changes on every update.

      :parameters:



      :Game Scenes: All

   .. function:: TestClass CreateTestObject(string value)



      :parameters:



      :Game Scenes: All

   .. function:: string DeprecatedProcedure(float value)

      .. warning:: Deprecated. Use :meth:`TestService.FloatToString <TestService.TestService.FloatToString>` instead.

      Deprecated procedure documentation string.

      :parameters:



      :Game Scenes: All

   .. function:: string DeprecatedProcedureNoMessage(float value)

      .. warning:: Deprecated.

      Deprecated procedure with no reason documentation string.

      :parameters:



      :Game Scenes: All

   .. property:: string DeprecatedProperty { get; set; }

      .. warning:: Deprecated. Use :prop:`TestService.StringProperty <TestService.TestService.StringProperty>` instead.

      Deprecated property documentation string.

      :Game Scenes: All

   .. function:: System.Collections.Generic.IDictionary<int,bool> DictionaryDefault(System.Collections.Generic.IDictionary<int,bool> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Collections.Generic.Dictionary<int,bool> { { 1, false }, { 2, true } }``.

      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<double> DoubleSpecialDefaults(double nan = double.NaN, double infinity = double.PositiveInfinity, double negativeInfinity = double.NegativeInfinity, double maximum = double.MaxValue, double lowest = double.MinValue)

      Procedure whose defaults are the special values of double.

      :parameters:







      :Game Scenes: All

   .. function:: string DoubleToString(double value)



      :parameters:



      :Game Scenes: All

   .. function:: string DumpExpressionTree(KRPC.Expression expression)

      The expression tree of a function, as text.

      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IDictionary<string,TestClass> EchoDictionaryOfNullableObjects(System.Collections.Generic.IDictionary<string,TestClass> d)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<int?> EchoListOfNullableInts(System.Collections.Generic.IList<int?> l)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<TestClass> EchoListOfNullableObjects(System.Collections.Generic.IList<TestClass> l)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<System.Collections.Generic.IList<TestClass>> EchoNestedListOfNullableObjects(System.Collections.Generic.IList<System.Collections.Generic.IList<TestClass>> l)



      :parameters:



      :Game Scenes: All

   .. function:: TestNestedNullableStruct EchoNestedNullableStruct(TestNestedNullableStruct value)



      :parameters:



      :Game Scenes: All

   .. function:: bool? EchoNullableBool(bool? value)



      :parameters:



      :Game Scenes: All

   .. function:: byte[] EchoNullableBytes(byte[] value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IDictionary<string,int> EchoNullableDictionary(System.Collections.Generic.IDictionary<string,int> d)



      :parameters:



      :Game Scenes: All

   .. function:: double? EchoNullableDouble(double? value)



      :parameters:



      :Game Scenes: All

   .. function:: TestEnum? EchoNullableEnum(TestEnum? x)



      :parameters:



      :Game Scenes: All

   .. function:: float? EchoNullableFloat(float? value)



      :parameters:



      :Game Scenes: All

   .. function:: int? EchoNullableInt(int? value)

      Returns the given value.

      :parameters:

       * **value** -- The value, or ``null``.

      :returns: The value.
      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<int> EchoNullableList(System.Collections.Generic.IList<int> l)



      :parameters:



      :Game Scenes: All

   .. function:: long? EchoNullableLong(long? value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.ISet<int> EchoNullableSet(System.Collections.Generic.ISet<int> h)



      :parameters:



      :Game Scenes: All

   .. function:: string EchoNullableString(string value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Tuple<int,long> EchoNullableTuple(System.Tuple<int,long> t)



      :parameters:



      :Game Scenes: All

   .. function:: uint? EchoNullableUInt(uint? value)



      :parameters:



      :Game Scenes: All

   .. function:: ulong? EchoNullableULong(ulong? value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.ISet<string> EchoStringSet(System.Collections.Generic.ISet<string> h)



      :parameters:



      :Game Scenes: All

   .. function:: TestClass EchoTestObject(TestClass value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Tuple<int,TestClass> EchoTupleWithANullableObject(System.Tuple<int,TestClass> t)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<string> EmptyListDefault(System.Collections.Generic.IList<string> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Collections.Generic.List<string> { }``.

      :Game Scenes: All

   .. function:: TestEnum EnumDefault(TestEnum x = TestEnum.ValueC)



      :parameters:



      :Game Scenes: All

   .. function:: TestEnum EnumEcho(TestEnum x)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<TestEnum> EnumListDefault(System.Collections.Generic.IList<TestEnum> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Collections.Generic.List<TestEnum> { TestEnum.ValueB, TestEnum.ValueC }``.

      :Game Scenes: All

   .. function:: string EnumToString(TestEnum x)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<float> FloatSpecialDefaults(float nan = float.NaN, float infinity = float.PositiveInfinity, float negativeInfinity = float.NegativeInfinity, float maximum = float.MaxValue, float lowest = float.MinValue, float fraction = 0.1f)

      Procedure whose defaults are the special values of float, plus a finite fraction
      that no float can hold exactly.

      :parameters:








      :Game Scenes: All

   .. function:: string FloatToString(float value)

      Procedure documentation string.

      :parameters:



      :Game Scenes: All

   .. function:: byte[] HexStringToBytes(string value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IDictionary<string,int> IncrementDictionary(System.Collections.Generic.IDictionary<string,int> d)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<int> IncrementList(System.Collections.Generic.IList<int> l)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<TestStruct> IncrementListOfStructs(System.Collections.Generic.IList<TestStruct> l)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IDictionary<string,System.Collections.Generic.IList<int>> IncrementNestedCollection(System.Collections.Generic.IDictionary<string,System.Collections.Generic.IList<int>> d)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.ISet<int> IncrementSet(System.Collections.Generic.ISet<int> h)



      :parameters:



      :Game Scenes: All

   .. function:: System.Tuple<int,long> IncrementTuple(System.Tuple<int,long> t)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<int> Int32SpecialDefaults(int maximum = int.MaxValue, int minimum = int.MinValue)

      Procedure whose defaults are the extremes of int.

      :parameters:




      :Game Scenes: All

   .. function:: string Int32ToString(int value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<long> Int64SpecialDefaults(long maximum = long.MaxValue, long minimum = long.MinValue)

      Procedure whose defaults are the extremes of long.

      :parameters:




      :Game Scenes: All

   .. function:: string Int64ToString(long value)



      :parameters:



      :Game Scenes: All

   .. function:: string JoinWithAtSign(string firstValue, string from)

      Returns *firstValue* and *from* joined by an at
      sign, e.g. ``name@host``. The parameter names are:

      * a name in camel case, and
      * a name that is a keyword in some languages.

      :parameters:

       * **firstValue** -- The string before the at sign.
       * **from** -- The string after the at sign.

      :returns: The strings joined.
      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<int> ListDefault(System.Collections.Generic.IList<int> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Collections.Generic.List<int> { 1, 2, 3 }``.

      :Game Scenes: All

   .. function:: TestNestedStruct NestedStructEcho(TestNestedStruct x)



      :parameters:



      :Game Scenes: All

   .. function:: TestClass NotNullableObject(TestClass value)



      :parameters:



      :Game Scenes: All

   .. function:: string NullDefault(TestClass obj = null)



      :parameters:



      :Game Scenes: All

   .. property:: TestClass NullableObject { get; set; }



      :Game Scenes: All

   .. function:: TestNullableStruct NullableStructEcho(TestNullableStruct x)



      :parameters:



      :Game Scenes: All

   .. property:: TestClass ObjectProperty { get; set; }

      A property holding an object.

      :returns: The object, or ``null``.
      :Game Scenes: All

   .. function:: KRPC.Client.Event OnTimer(uint milliseconds, uint repeats = 1)



      :parameters:




      :Game Scenes: All

   .. function:: string OptionalArguments(string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:






      :Game Scenes: All

   .. function:: KRPC.Client.Event RemoveEventAfter(uint milliseconds)



      :parameters:



      :Game Scenes: All

   .. function:: void RemoveTestObject(TestClass value)



      :parameters:



      :Game Scenes: All

   .. function:: void ResetCustomExceptionLater()




      :Game Scenes: All

   .. function:: void ResetInvalidOperationExceptionLater()




      :Game Scenes: All

   .. function:: TestClass ReturnNullWhenNotAllowed()




      :Game Scenes: All

   .. function:: int ScalarDefault(int x = 42)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.ISet<int> SetDefault(System.Collections.Generic.ISet<int> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Collections.Generic.HashSet<int> { 1, 2, 3 }``.

      :Game Scenes: All

   .. function:: string StringDefault(string x = "foo")



      :parameters:



      :Game Scenes: All

   .. property:: string StringProperty { get; set; }

      Property documentation string.

      :Game Scenes: All

   .. property:: string StringPropertyPrivateGet { set; }



      :Game Scenes: All

   .. property:: string StringPropertyPrivateSet { get; }



      :Game Scenes: All

   .. function:: bool StringToBool(string value)



      :parameters:



      :Game Scenes: All

   .. function:: double StringToDouble(string value)



      :parameters:



      :Game Scenes: All

   .. function:: TestEnum StringToEnum(string value)



      :parameters:



      :Game Scenes: All

   .. function:: float StringToFloat(string value)



      :parameters:



      :Game Scenes: All

   .. function:: int StringToInt32(string value)



      :parameters:



      :Game Scenes: All

   .. function:: long StringToInt64(string value)



      :parameters:



      :Game Scenes: All

   .. function:: uint StringToUInt32(string value)



      :parameters:



      :Game Scenes: All

   .. function:: ulong StringToUInt64(string value)



      :parameters:



      :Game Scenes: All

   .. function:: TestStruct StructDefault(TestStruct? x = null)



      :parameters:

       * **x** -- If null, defaults to ``new TestStruct(42, "alpha", TestEnum.ValueB, new System.Collections.Generic.List<int> { 1, 2, 3 })``.

      :Game Scenes: All

   .. function:: TestStruct StructEcho(TestStruct x)



      :parameters:



      :Game Scenes: All

   .. function:: TestStruct? StructEchoNullable(TestStruct? x)



      :parameters:



      :Game Scenes: All

   .. function:: System.Tuple<TestClass,TestClass> SwapObjectTuple(System.Tuple<TestClass,TestClass> t)



      :parameters:



      :Game Scenes: All

   .. function:: int ThrowArgumentException()




      :Game Scenes: All

   .. function:: int ThrowArgumentNullException(string foo)



      :parameters:



      :Game Scenes: All

   .. function:: int ThrowArgumentOutOfRangeException(int foo)



      :parameters:



      :Game Scenes: All

   .. function:: int ThrowCustomException()

      Throws a :type:`CustomException`.


      :Game Scenes: All


      .. note::

         Links to each kind of member: :type:`TestService <TestService.TestService>`, :type:`TestClass`,
         :type:`TestEnum`, :type:`TestStruct`, :type:`KRPC.InvalidOperationException`,
         :meth:`TestService.FloatToString <TestService.TestService.FloatToString>`, :prop:`TestService.StringProperty <TestService.TestService.StringProperty>`,
         :prop:`TestService.StringPropertyPrivateGet <TestService.TestService.StringPropertyPrivateGet>`, :meth:`TestClass.FloatToString`,
         :meth:`TestClass.StaticMethod`, :prop:`TestClass.IntProperty`,
         :prop:`TestClass.StringPropertyPrivateGet`, :value:`TestEnum.ValueA` and
         :prop:`TestStruct.IntField`.

   .. function:: int ThrowCustomExceptionLater()




      :Game Scenes: All

   .. function:: int ThrowInvalidOperationException()




      :Game Scenes: All

   .. function:: int ThrowInvalidOperationExceptionLater()




      :Game Scenes: All

   .. function:: int ThrowMappedException(string name)

      Throws the system exception of the given name, which the server maps to the KRPC
      exception of that name.

      :parameters:



      :Game Scenes: All

   .. function:: System.Tuple<int,bool> TupleDefault(System.Tuple<int,bool> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Tuple<int,bool>(1, false)``.

      :Game Scenes: All

   .. function:: string UInt32ToString(uint value)



      :parameters:



      :Game Scenes: All

   .. function:: string UInt64ToString(ulong value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<uint> Uint32SpecialDefaults(uint maximum = uint.MaxValue)

      Procedure whose default is the largest uint.

      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<ulong> Uint64SpecialDefaults(ulong maximum = ulong.MaxValue)

      Procedure whose default is the largest ulong.

      :parameters:



      :Game Scenes: All



.. class:: TestClass

   Class documentation string.

   .. function:: TestClass EchoNullableObject(TestClass value)



      :parameters:



      :Game Scenes: All

   .. function:: string FloatToString(float x)



      :parameters:



      :Game Scenes: All

   .. function:: string GetValue()

      Method documentation string.


      :Game Scenes: All

   .. property:: int IntProperty { get; set; }

      Property documentation string.

      :Game Scenes: All

   .. property:: TestClass ObjectProperty { get; set; }

      A property of the object, holding another object.

      :returns: The other object, or ``null``.
      :Game Scenes: All

   .. function:: string ObjectToString(TestClass other)



      :parameters:



      :Game Scenes: All

   .. function:: string OptionalArguments(string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:






      :Game Scenes: All

   .. function:: static string StaticMethod(IConnection connection, string a = "", string b = "")

      Returns static followed by *a* and *b*.

      :parameters:

       * **a** -- The first string.
       * **b** -- The second string.

      :returns: The strings joined.
      :Game Scenes: All

   .. function:: static TestClass StaticNullableObject(IConnection connection, TestClass value)



      :parameters:



      :Game Scenes: All

   .. function:: static string StaticOptionalArguments(IConnection connection, string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:






      :Game Scenes: All

   .. property:: string StringPropertyPrivateGet { set; }



      :Game Scenes: All

   .. property:: string StringPropertyPrivateSet { get; }



      :Game Scenes: All



.. class:: DeprecatedClass

   .. warning:: Deprecated. Use :type:`TestClass` instead.

   Deprecated class documentation string.

   .. function:: string DeprecatedMethod()

      .. warning:: Deprecated. Use :meth:`TestClass.GetValue` instead.

      Deprecated method documentation string.


      :Game Scenes: All



.. enum:: TestEnum

   Enum documentation string.


   .. enumerator:: ValueA

      Enum ValueA documentation string.


   .. enumerator:: ValueB

      Enum ValueB documentation string.


   .. enumerator:: ValueC

      Enum ValueC documentation string, in :math:`\mathrm{m/s^{2}}`, equal to
      :math:`\frac{1}{2} \rho v^{2}`.



.. enum:: DeprecatedEnum

   .. warning:: Deprecated. Use :type:`TestEnum` instead.

   Deprecated enum documentation string.


   .. enumerator:: ValueA

      Deprecated enum ValueA documentation string.


   .. enumerator:: ValueB

      .. warning:: Deprecated. Use :value:`DeprecatedEnum.ValueA` instead.

      Deprecated enum ValueB documentation string.



.. struct:: TestStruct

   Struct documentation string.

   .. function:: TestStruct(int intField, string stringField, TestEnum enumField, System.Collections.Generic.IList<int> listField)

      Construct a TestStruct from the values of its fields.


   .. property:: int IntField { get; }

      Struct IntField documentation string.

   .. property:: string StringField { get; }



   .. property:: TestEnum EnumField { get; }



   .. property:: System.Collections.Generic.IList<int> ListField { get; }




.. struct:: TestNestedStruct

   Nested struct documentation string.

   .. function:: TestNestedStruct(TestStruct structField, TestClass objectField, string stringField)

      Construct a TestNestedStruct from the values of its fields.


   .. property:: TestStruct StructField { get; }



   .. property:: TestClass ObjectField { get; }



   .. property:: string StringField { get; }




.. struct:: TestNullableStruct

   Nullable struct documentation string.

   .. function:: TestNullableStruct(int intField, int? nullableIntField, TestEnum? nullableEnumField, string nullableStringField, TestClass nullableObjectField)

      Construct a TestNullableStruct from the values of its fields.


   .. property:: int IntField { get; }



   .. property:: int? NullableIntField { get; }



   .. property:: TestEnum? NullableEnumField { get; }



   .. property:: string NullableStringField { get; }



   .. property:: TestClass NullableObjectField { get; }




.. struct:: TestNestedNullableStruct

   Nested nullable struct documentation string.

   .. function:: TestNestedNullableStruct(System.Collections.Generic.IList<TestClass> listField, string stringField)

      Construct a TestNestedNullableStruct from the values of its fields.


   .. property:: System.Collections.Generic.IList<TestClass> ListField { get; }



   .. property:: string StringField { get; }




.. struct:: DeprecatedStruct

   .. warning:: Deprecated. Use :type:`TestStruct` instead.

   Deprecated struct documentation string.

   .. function:: DeprecatedStruct(int value, int oldValue)

      Construct a DeprecatedStruct from the values of its fields.


   .. property:: int Value { get; }

      Deprecated struct Value documentation string.

   .. property:: int OldValue { get; }

      .. warning:: Deprecated. Use :prop:`DeprecatedStruct.Value` instead.

      Deprecated struct OldValue documentation string.


.. class:: CustomException





.. class:: DeprecatedException

   .. warning:: Deprecated. Use :type:`CustomException` instead.

   Deprecated exception documentation string.
