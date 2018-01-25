.. default-domain:: java
.. highlight:: java

.. package:: krpc.client.services.TestService


.. type:: public class TestService

   Service documentation string.

   .. method:: String addMultipleValues(float x, int y, long z)



      :param float x:
      :param int y:
      :param long z:

   .. method:: java.util.List<TestClass> addToObjectList(java.util.List<TestClass> l, String value)



      :param java.util.List<TestClass> l:
      :param String value:

   .. method:: int blockingProcedure(int n, int sum)



      :param int n:
      :param int sum:

   .. method:: String boolToString(boolean value)



      :param boolean value:

   .. method:: String bytesToHexString(byte[] value)



      :param byte[] value:

   .. method:: int counter(String id, int divisor)



      :param String id:
      :param int divisor:

   .. method:: TestClass createTestObject(String value)



      :param String value:

   .. method:: java.util.Map<Integer,Boolean> dictionaryDefault(java.util.Map<Integer,Boolean> x)



      :param java.util.Map<Integer,Boolean> x:

   .. method:: String doubleToString(double value)



      :param double value:

   .. method:: TestClass echoTestObject(TestClass value)



      :param TestClass value:

   .. method:: TestEnum enumDefaultArg(TestEnum x)



      :param TestEnum x:

   .. method:: TestEnum enumEcho(TestEnum x)



      :param TestEnum x:

   .. method:: TestEnum enumReturn()

   .. method:: String floatToString(float value)

      Procedure documentation string.

      :param float value:

   .. method:: java.util.Map<String,Integer> incrementDictionary(java.util.Map<String,Integer> d)



      :param java.util.Map<String,Integer> d:

   .. method:: java.util.List<Integer> incrementList(java.util.List<Integer> l)



      :param java.util.List<Integer> l:

   .. method:: java.util.Map<String,java.util.List<Integer>> incrementNestedCollection(java.util.Map<String,java.util.List<Integer>> d)



      :param java.util.Map<String,java.util.List<Integer>> d:

   .. method:: java.util.Set<Integer> incrementSet(java.util.Set<Integer> h)



      :param java.util.Set<Integer> h:

   .. method:: org.javatuples.Pair<Integer,Long> incrementTuple(org.javatuples.Pair<Integer,Long> t)



      :param org.javatuples.Pair<Integer,Long> t:

   .. method:: String int32ToString(int value)



      :param int value:

   .. method:: String int64ToString(long value)



      :param long value:

   .. method:: java.util.List<Integer> listDefault(java.util.List<Integer> x)



      :param java.util.List<Integer> x:

   .. method:: TestClass getObjectProperty()

   .. method:: void setObjectProperty(TestClass value)

   .. method:: krpc.schema.KRPC.Event onTimer(int milliseconds, int repeats)



      :param int milliseconds:
      :param int repeats:

   .. method:: krpc.schema.KRPC.Event onTimerUsingLambda(int milliseconds)



      :param int milliseconds:

   .. method:: String optionalArguments(String x, String y, String z, TestClass obj)



      :param String x:
      :param String y:
      :param String z:
      :param TestClass obj:

   .. method:: void resetCustomExceptionLater()

   .. method:: void resetInvalidOperationExceptionLater()

   .. method:: TestClass returnNullWhenNotAllowed()

   .. method:: java.util.Set<Integer> setDefault(java.util.Set<Integer> x)



      :param java.util.Set<Integer> x:

   .. method:: String getStringProperty()

   .. method:: void setStringProperty(String value)

      Property documentation string.

   .. method:: void setStringPropertyPrivateGet(String value)

   .. method:: String getStringPropertyPrivateSet()

   .. method:: int stringToInt32(String value)



      :param String value:

   .. method:: int throwArgumentException()

   .. method:: int throwArgumentNullException(String foo)



      :param String foo:

   .. method:: int throwArgumentOutOfRangeException(int foo)



      :param int foo:

   .. method:: int throwCustomException()

   .. method:: int throwCustomExceptionLater()

   .. method:: int throwInvalidOperationException()

   .. method:: int throwInvalidOperationExceptionLater()

   .. method:: org.javatuples.Pair<Integer,Boolean> tupleDefault(org.javatuples.Pair<Integer,Boolean> x)



      :param org.javatuples.Pair<Integer,Boolean> x:



.. type:: public class TestClass

   Class documentation string.

   .. method:: String floatToString(float x)



      :param float x:

   .. method:: String getValue()

      Method documentation string.

   .. method:: int getIntProperty()

   .. method:: void setIntProperty(int value)

      Property documentation string.

   .. method:: TestClass getObjectProperty()

   .. method:: void setObjectProperty(TestClass value)

   .. method:: String objectToString(TestClass other)



      :param TestClass other:

   .. method:: String optionalArguments(String x, String y, String z, TestClass obj)



      :param String x:
      :param String y:
      :param String z:
      :param TestClass obj:

   .. method:: static String staticMethod(Connection connection, String a, String b)



      :param String a:
      :param String b:



.. type:: public enum TestEnum

   Enum documentation string.


   .. field:: public TestEnum VALUE_A

      Enum ValueA documentation string.


   .. field:: public TestEnum VALUE_B

      Enum ValueB documentation string.


   .. field:: public TestEnum VALUE_C

      Enum ValueC documentation string.



.. type:: public class CustomException
