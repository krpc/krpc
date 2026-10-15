.. default-domain:: cs
.. highlight:: csharp

.. namespace:: KRPC.Client.Services.TestService


.. class:: TestService

   Service documentation string.

   .. function:: string AddMultipleValues(float x, int y, long z)



      :parameters:





      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<TestClass> AddToObjectList(System.Collections.Generic.IList<TestClass> l, string value)



      :parameters:




      :Game Scenes: All

   .. function:: int BlockingProcedure(int n, int sum = 0)



      :parameters:




      :Game Scenes: All

   .. function:: string BoolToString(bool value)



      :parameters:



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

   .. function:: int EchoNullableInt(int value)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<int> EchoNullableList(System.Collections.Generic.IList<int> l)



      :parameters:



      :Game Scenes: All

   .. function:: string EchoNullableString(string value)



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

   .. function:: TestEnum EnumDefaultArg(TestEnum x = TestEnum.ValueC)



      :parameters:



      :Game Scenes: All

   .. function:: TestEnum EnumEcho(TestEnum x)



      :parameters:



      :Game Scenes: All

   .. function:: System.Collections.Generic.IList<TestEnum> EnumListDefault(System.Collections.Generic.IList<TestEnum> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Collections.Generic.List<TestEnum> { TestEnum.ValueB, TestEnum.ValueC }``.

      :Game Scenes: All

   .. function:: TestEnum EnumReturn()




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

   .. property:: TestClass NullableObject { get; set; }



      :Game Scenes: All

   .. function:: TestNullableStruct NullableStructEcho(TestNullableStruct x)



      :parameters:



      :Game Scenes: All

   .. property:: TestClass ObjectProperty { get; set; }



      :Game Scenes: All

   .. function:: KRPC.Schema.KRPC.Event OnTimer(uint milliseconds, uint repeats = 1)



      :parameters:




      :Game Scenes: All

   .. function:: KRPC.Schema.KRPC.Event OnTimerUsingLambda(uint milliseconds)



      :parameters:



      :Game Scenes: All

   .. function:: string OptionalArguments(string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:






      :Game Scenes: All

   .. function:: KRPC.Schema.KRPC.Event RemoveEventAfter(uint milliseconds)



      :parameters:



      :Game Scenes: All

   .. function:: void ResetCustomExceptionLater()




      :Game Scenes: All

   .. function:: void ResetInvalidOperationExceptionLater()




      :Game Scenes: All

   .. function:: TestClass ReturnNullWhenNotAllowed()




      :Game Scenes: All

   .. function:: System.Collections.Generic.ISet<int> SetDefault(System.Collections.Generic.ISet<int> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Collections.Generic.HashSet<int> { 1, 2, 3 }``.

      :Game Scenes: All

   .. property:: string StringProperty { get; set; }

      Property documentation string.

      :Game Scenes: All

   .. property:: string StringPropertyPrivateGet { set; }



      :Game Scenes: All

   .. property:: string StringPropertyPrivateSet { get; }



      :Game Scenes: All

   .. function:: int StringToInt32(string value)



      :parameters:



      :Game Scenes: All

   .. function:: TestStruct StructDefault(TestStruct? x = null)



      :parameters:

       * **x** -- If null, defaults to ``new TestStruct(42, "jeb", TestEnum.ValueB, new System.Collections.Generic.List<int> { 1, 2, 3 })``.

      :Game Scenes: All

   .. function:: TestStruct StructEcho(TestStruct x)



      :parameters:



      :Game Scenes: All

   .. function:: TestStruct StructEchoNullable(TestStruct x)



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




      :Game Scenes: All

   .. function:: int ThrowCustomExceptionLater()




      :Game Scenes: All

   .. function:: int ThrowInvalidOperationException()




      :Game Scenes: All

   .. function:: int ThrowInvalidOperationExceptionLater()




      :Game Scenes: All

   .. function:: System.Tuple<int,bool> TupleDefault(System.Tuple<int,bool> x = null)



      :parameters:

       * **x** -- If null, defaults to ``new System.Tuple<int,bool>(1, false)``.

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



      :Game Scenes: All

   .. function:: string ObjectToString(TestClass other)



      :parameters:



      :Game Scenes: All

   .. function:: string OptionalArguments(string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:






      :Game Scenes: All

   .. function:: static string StaticMethod(IConnection connection, string a = "", string b = "")



      :parameters:




      :Game Scenes: All

   .. function:: static TestClass StaticNullableObject(IConnection connection, TestClass value)



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

      Enum ValueC documentation string.



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


   .. property:: int IntField { get; set; }

      Struct IntField documentation string.

   .. property:: string StringField { get; set; }



   .. property:: TestEnum EnumField { get; set; }



   .. property:: System.Collections.Generic.IList<int> ListField { get; set; }




.. struct:: TestNestedStruct

   Nested struct documentation string.


   .. property:: TestStruct StructField { get; set; }



   .. property:: TestClass ObjectField { get; set; }



   .. property:: string StringField { get; set; }




.. struct:: TestNullableStruct

   Nullable struct documentation string.


   .. property:: int IntField { get; set; }



   .. property:: int? NullableIntField { get; set; }



   .. property:: TestEnum? NullableEnumField { get; set; }



   .. property:: string NullableStringField { get; set; }



   .. property:: TestClass NullableObjectField { get; set; }




.. struct:: TestNestedNullableStruct

   Nested nullable struct documentation string.


   .. property:: System.Collections.Generic.IList<TestClass> ListField { get; set; }



   .. property:: string StringField { get; set; }




.. struct:: DeprecatedStruct

   .. warning:: Deprecated. Use :type:`TestStruct` instead.

   Deprecated struct documentation string.


   .. property:: int Value { get; set; }

      Deprecated struct Value documentation string.

   .. property:: int OldValue { get; set; }

      .. warning:: Deprecated. Use :prop:`DeprecatedStruct.Value` instead.

      Deprecated struct OldValue documentation string.


.. class:: CustomException





.. class:: DeprecatedException

   .. warning:: Deprecated. Use CustomException instead.

   Deprecated exception documentation string.
