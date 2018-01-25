.. default-domain:: csharp
.. highlight:: csharp

.. namespace:: KRPC.Client.Services.TestService


.. class:: TestService

   Service documentation string.

   .. method:: string AddMultipleValues(float x, int y, long z)



      :parameters:

   .. method:: System.Collections.Generic.IList<TestClass> AddToObjectList(System.Collections.Generic.IList<TestClass> l, string value)



      :parameters:

   .. method:: int BlockingProcedure(int n, int sum = 0)



      :parameters:

   .. method:: string BoolToString(bool value)



      :parameters:

   .. method:: string BytesToHexString(byte[] value)



      :parameters:

   .. method:: int Counter(string id = "", int divisor = 1)



      :parameters:

   .. method:: TestClass CreateTestObject(string value)



      :parameters:

   .. method:: System.Collections.Generic.IDictionary<int,bool> DictionaryDefault(System.Collections.Generic.IDictionary<int,bool> x = { 1: false, 2: true })



      :parameters:

   .. method:: string DoubleToString(double value)



      :parameters:

   .. method:: TestClass EchoTestObject(TestClass value)



      :parameters:

   .. method:: TestEnum EnumDefaultArg(TestEnum x = 2)



      :parameters:

   .. method:: TestEnum EnumEcho(TestEnum x)



      :parameters:

   .. method:: TestEnum EnumReturn()

   .. method:: string FloatToString(float value)

      Procedure documentation string.

      :parameters:

   .. method:: System.Collections.Generic.IDictionary<string,int> IncrementDictionary(System.Collections.Generic.IDictionary<string,int> d)



      :parameters:

   .. method:: System.Collections.Generic.IList<int> IncrementList(System.Collections.Generic.IList<int> l)



      :parameters:

   .. method:: System.Collections.Generic.IDictionary<string,System.Collections.Generic.IList<int>> IncrementNestedCollection(System.Collections.Generic.IDictionary<string,System.Collections.Generic.IList<int>> d)



      :parameters:

   .. method:: System.Collections.Generic.ISet<int> IncrementSet(System.Collections.Generic.ISet<int> h)



      :parameters:

   .. method:: System.Tuple<int,long> IncrementTuple(System.Tuple<int,long> t)



      :parameters:

   .. method:: string Int32ToString(int value)



      :parameters:

   .. method:: string Int64ToString(long value)



      :parameters:

   .. method:: System.Collections.Generic.IList<int> ListDefault(System.Collections.Generic.IList<int> x = { 1, 2, 3 })



      :parameters:

   .. property:: TestClass ObjectProperty { get; set; }

   .. method:: KRPC.Schema.KRPC.Event OnTimer(uint milliseconds, uint repeats = 1)



      :parameters:

   .. method:: KRPC.Schema.KRPC.Event OnTimerUsingLambda(uint milliseconds)



      :parameters:

   .. method:: string OptionalArguments(string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:

   .. method:: void ResetCustomExceptionLater()

   .. method:: void ResetInvalidOperationExceptionLater()

   .. method:: TestClass ReturnNullWhenNotAllowed()

   .. method:: System.Collections.Generic.ISet<int> SetDefault(System.Collections.Generic.ISet<int> x = { 1, 2, 3 })



      :parameters:

   .. property:: string StringProperty { get; set; }

      Property documentation string.

   .. property:: string StringPropertyPrivateGet { set; }

   .. property:: string StringPropertyPrivateSet { get; }

   .. method:: int StringToInt32(string value)



      :parameters:

   .. method:: int ThrowArgumentException()

   .. method:: int ThrowArgumentNullException(string foo)



      :parameters:

   .. method:: int ThrowArgumentOutOfRangeException(int foo)



      :parameters:

   .. method:: int ThrowCustomException()

   .. method:: int ThrowCustomExceptionLater()

   .. method:: int ThrowInvalidOperationException()

   .. method:: int ThrowInvalidOperationExceptionLater()

   .. method:: System.Tuple<int,bool> TupleDefault(System.Tuple<int,bool> x = { 1, false })



      :parameters:



.. class:: TestClass

   Class documentation string.

   .. method:: string FloatToString(float x)



      :parameters:

   .. method:: string GetValue()

      Method documentation string.

   .. property:: int IntProperty { get; set; }

      Property documentation string.

   .. property:: TestClass ObjectProperty { get; set; }

   .. method:: string ObjectToString(TestClass other)



      :parameters:

   .. method:: string OptionalArguments(string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:

   .. method:: static string StaticMethod(IConnection connection, string a = "", string b = "")



      :parameters:



.. enum:: TestEnum

   Enum documentation string.


   .. value:: ValueA

      Enum ValueA documentation string.


   .. value:: ValueB

      Enum ValueB documentation string.


   .. value:: ValueC

      Enum ValueC documentation string.



.. class:: CustomException
