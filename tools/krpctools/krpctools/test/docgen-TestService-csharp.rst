.. default-domain:: csharp
.. highlight:: csharp

.. namespace:: KRPC.Client.Services.TestService


.. class:: TestService

   Service documentation string.

   .. method:: string AddMultipleValues(float x, int y, long z)



      :parameters:





      :Game Scenes: All

   .. method:: System.Collections.Generic.IList<TestClass> AddToObjectList(System.Collections.Generic.IList<TestClass> l, string value)



      :parameters:




      :Game Scenes: All

   .. method:: int BlockingProcedure(int n, int sum = 0)



      :parameters:




      :Game Scenes: All

   .. method:: string BoolToString(bool value)



      :parameters:



      :Game Scenes: All

   .. method:: string BytesToHexString(byte[] value)



      :parameters:



      :Game Scenes: All

   .. method:: int Counter(string id = "", int divisor = 1)



      :parameters:




      :Game Scenes: All

   .. method:: TestClass CreateTestObject(string value)



      :parameters:



      :Game Scenes: All

   .. method:: System.Collections.Generic.IDictionary<int,bool> DictionaryDefault(System.Collections.Generic.IDictionary<int,bool> x = { 1: false, 2: true })



      :parameters:



      :Game Scenes: All

   .. method:: string DoubleToString(double value)



      :parameters:



      :Game Scenes: All

   .. method:: TestClass EchoTestObject(TestClass value)



      :parameters:



      :Game Scenes: All

   .. method:: TestEnum EnumDefaultArg(TestEnum x = 2)



      :parameters:



      :Game Scenes: All

   .. method:: TestEnum EnumEcho(TestEnum x)



      :parameters:



      :Game Scenes: All

   .. method:: TestEnum EnumReturn()




      :Game Scenes: All

   .. method:: string FloatToString(float value)

      Procedure documentation string.

      :parameters:



      :Game Scenes: All

   .. method:: System.Collections.Generic.IDictionary<string,int> IncrementDictionary(System.Collections.Generic.IDictionary<string,int> d)



      :parameters:



      :Game Scenes: All

   .. method:: System.Collections.Generic.IList<int> IncrementList(System.Collections.Generic.IList<int> l)



      :parameters:



      :Game Scenes: All

   .. method:: System.Collections.Generic.IDictionary<string,System.Collections.Generic.IList<int>> IncrementNestedCollection(System.Collections.Generic.IDictionary<string,System.Collections.Generic.IList<int>> d)



      :parameters:



      :Game Scenes: All

   .. method:: System.Collections.Generic.ISet<int> IncrementSet(System.Collections.Generic.ISet<int> h)



      :parameters:



      :Game Scenes: All

   .. method:: System.Tuple<int,long> IncrementTuple(System.Tuple<int,long> t)



      :parameters:



      :Game Scenes: All

   .. method:: string Int32ToString(int value)



      :parameters:



      :Game Scenes: All

   .. method:: string Int64ToString(long value)



      :parameters:



      :Game Scenes: All

   .. method:: System.Collections.Generic.IList<int> ListDefault(System.Collections.Generic.IList<int> x = { 1, 2, 3 })



      :parameters:



      :Game Scenes: All

   .. property:: TestClass ObjectProperty { get; set; }



      :Game Scenes: All

   .. method:: KRPC.Schema.KRPC.Event OnTimer(uint milliseconds, uint repeats = 1)



      :parameters:




      :Game Scenes: All

   .. method:: KRPC.Schema.KRPC.Event OnTimerUsingLambda(uint milliseconds)



      :parameters:



      :Game Scenes: All

   .. method:: string OptionalArguments(string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:






      :Game Scenes: All

   .. method:: void ResetCustomExceptionLater()




      :Game Scenes: All

   .. method:: void ResetInvalidOperationExceptionLater()




      :Game Scenes: All

   .. method:: TestClass ReturnNullWhenNotAllowed()




      :Game Scenes: All

   .. method:: System.Collections.Generic.ISet<int> SetDefault(System.Collections.Generic.ISet<int> x = { 1, 2, 3 })



      :parameters:



      :Game Scenes: All

   .. property:: string StringProperty { get; set; }

      Property documentation string.

      :Game Scenes: All

   .. property:: string StringPropertyPrivateGet { set; }



      :Game Scenes: All

   .. property:: string StringPropertyPrivateSet { get; }



      :Game Scenes: All

   .. method:: int StringToInt32(string value)



      :parameters:



      :Game Scenes: All

   .. method:: int ThrowArgumentException()




      :Game Scenes: All

   .. method:: int ThrowArgumentNullException(string foo)



      :parameters:



      :Game Scenes: All

   .. method:: int ThrowArgumentOutOfRangeException(int foo)



      :parameters:



      :Game Scenes: All

   .. method:: int ThrowCustomException()




      :Game Scenes: All

   .. method:: int ThrowCustomExceptionLater()




      :Game Scenes: All

   .. method:: int ThrowInvalidOperationException()




      :Game Scenes: All

   .. method:: int ThrowInvalidOperationExceptionLater()




      :Game Scenes: All

   .. method:: System.Tuple<int,bool> TupleDefault(System.Tuple<int,bool> x = { 1, false })



      :parameters:



      :Game Scenes: All



.. class:: TestClass

   Class documentation string.

   .. method:: string FloatToString(float x)



      :parameters:



      :Game Scenes: All

   .. method:: string GetValue()

      Method documentation string.


      :Game Scenes: All

   .. property:: int IntProperty { get; set; }

      Property documentation string.

      :Game Scenes: All

   .. property:: TestClass ObjectProperty { get; set; }



      :Game Scenes: All

   .. method:: string ObjectToString(TestClass other)



      :parameters:



      :Game Scenes: All

   .. method:: string OptionalArguments(string x, string y = "foo", string z = "bar", TestClass obj = null)



      :parameters:






      :Game Scenes: All

   .. method:: static string StaticMethod(IConnection connection, string a = "", string b = "")



      :parameters:




      :Game Scenes: All

   .. property:: string StringPropertyPrivateGet { set; }



      :Game Scenes: All

   .. property:: string StringPropertyPrivateSet { get; }



      :Game Scenes: All



.. enum:: TestEnum

   Enum documentation string.


   .. value:: ValueA

      Enum ValueA documentation string.


   .. value:: ValueB

      Enum ValueB documentation string.


   .. value:: ValueC

      Enum ValueC documentation string.



.. class:: CustomException
