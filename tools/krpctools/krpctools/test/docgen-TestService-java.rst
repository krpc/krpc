.. default-domain:: java
.. highlight:: java

.. package:: krpc.client.services.TestService


.. type:: public class TestService

   Service documentation string.

   .. method:: String addMultipleValues(float x, int y, long z)



      :param float x:
      :param int y:
      :param long z:
      :Game Scenes: All

   .. method:: java.util.List<TestClass> addToObjectList(java.util.List<TestClass> l, String value)



      :param java.util.List<TestClass> l:
      :param String value:
      :Game Scenes: All

   .. method:: int blockingProcedure(int n, int sum)



      :param int n:
      :param int sum:
      :Game Scenes: All

   .. method:: String boolToString(boolean value)



      :param boolean value:
      :Game Scenes: All

   .. method:: String bytesToHexString(byte[] value)



      :param byte[] value:
      :Game Scenes: All

   .. method:: int counter(String id, int divisor)



      :param String id:
      :param int divisor:
      :Game Scenes: All

   .. method:: TestClass createTestObject(String value)



      :param String value:
      :Game Scenes: All

   .. method:: java.util.Map<Integer,Boolean> dictionaryDefault(java.util.Map<Integer,Boolean> x)



      :param java.util.Map<Integer,Boolean> x:
      :Game Scenes: All

   .. method:: String doubleToString(double value)



      :param double value:
      :Game Scenes: All

   .. method:: TestClass echoTestObject(TestClass value)



      :param TestClass value:
      :Game Scenes: All

   .. method:: TestEnum enumDefaultArg(TestEnum x)



      :param TestEnum x:
      :Game Scenes: All

   .. method:: TestEnum enumEcho(TestEnum x)



      :param TestEnum x:
      :Game Scenes: All

   .. method:: TestEnum enumReturn()



      :Game Scenes: All

   .. method:: String floatToString(float value)

      Procedure documentation string.

      :param float value:
      :Game Scenes: All

   .. method:: java.util.Map<String,Integer> incrementDictionary(java.util.Map<String,Integer> d)



      :param java.util.Map<String,Integer> d:
      :Game Scenes: All

   .. method:: java.util.List<Integer> incrementList(java.util.List<Integer> l)



      :param java.util.List<Integer> l:
      :Game Scenes: All

   .. method:: java.util.Map<String,java.util.List<Integer>> incrementNestedCollection(java.util.Map<String,java.util.List<Integer>> d)



      :param java.util.Map<String,java.util.List<Integer>> d:
      :Game Scenes: All

   .. method:: java.util.Set<Integer> incrementSet(java.util.Set<Integer> h)



      :param java.util.Set<Integer> h:
      :Game Scenes: All

   .. method:: org.javatuples.Pair<Integer,Long> incrementTuple(org.javatuples.Pair<Integer,Long> t)



      :param org.javatuples.Pair<Integer,Long> t:
      :Game Scenes: All

   .. method:: String int32ToString(int value)



      :param int value:
      :Game Scenes: All

   .. method:: String int64ToString(long value)



      :param long value:
      :Game Scenes: All

   .. method:: java.util.List<Integer> listDefault(java.util.List<Integer> x)



      :param java.util.List<Integer> x:
      :Game Scenes: All

   .. method:: TestClass getObjectProperty()

   .. method:: void setObjectProperty(TestClass value)



      :Game Scenes: All

   .. method:: krpc.schema.KRPC.Event onTimer(int milliseconds, int repeats)



      :param int milliseconds:
      :param int repeats:
      :Game Scenes: All

   .. method:: krpc.schema.KRPC.Event onTimerUsingLambda(int milliseconds)



      :param int milliseconds:
      :Game Scenes: All

   .. method:: String optionalArguments(String x, String y, String z, TestClass obj)



      :param String x:
      :param String y:
      :param String z:
      :param TestClass obj:
      :Game Scenes: All

   .. method:: void resetCustomExceptionLater()



      :Game Scenes: All

   .. method:: void resetInvalidOperationExceptionLater()



      :Game Scenes: All

   .. method:: TestClass returnNullWhenNotAllowed()



      :Game Scenes: All

   .. method:: java.util.Set<Integer> setDefault(java.util.Set<Integer> x)



      :param java.util.Set<Integer> x:
      :Game Scenes: All

   .. method:: String getStringProperty()

   .. method:: void setStringProperty(String value)

      Property documentation string.

      :Game Scenes: All

   .. method:: void setStringPropertyPrivateGet(String value)



      :Game Scenes: All

   .. method:: String getStringPropertyPrivateSet()




      :Game Scenes: All

   .. method:: int stringToInt32(String value)



      :param String value:
      :Game Scenes: All

   .. method:: int throwArgumentException()



      :Game Scenes: All

   .. method:: int throwArgumentNullException(String foo)



      :param String foo:
      :Game Scenes: All

   .. method:: int throwArgumentOutOfRangeException(int foo)



      :param int foo:
      :Game Scenes: All

   .. method:: int throwCustomException()



      :Game Scenes: All

   .. method:: int throwCustomExceptionLater()



      :Game Scenes: All

   .. method:: int throwInvalidOperationException()



      :Game Scenes: All

   .. method:: int throwInvalidOperationExceptionLater()



      :Game Scenes: All

   .. method:: org.javatuples.Pair<Integer,Boolean> tupleDefault(org.javatuples.Pair<Integer,Boolean> x)



      :param org.javatuples.Pair<Integer,Boolean> x:
      :Game Scenes: All



.. type:: public class TestClass

   Class documentation string.

   .. method:: String floatToString(float x)



      :param float x:
      :Game Scenes: All

   .. method:: String getValue()

      Method documentation string.

      :Game Scenes: All

   .. method:: int getIntProperty()

   .. method:: void setIntProperty(int value)

      Property documentation string.

      :Game Scenes: All

   .. method:: TestClass getObjectProperty()

   .. method:: void setObjectProperty(TestClass value)



      :Game Scenes: All

   .. method:: String objectToString(TestClass other)



      :param TestClass other:
      :Game Scenes: All

   .. method:: String optionalArguments(String x, String y, String z, TestClass obj)



      :param String x:
      :param String y:
      :param String z:
      :param TestClass obj:
      :Game Scenes: All

   .. method:: static String staticMethod(Connection connection, String a, String b)



      :param String a:
      :param String b:
      :Game Scenes: All



.. type:: public enum TestEnum

   Enum documentation string.


   .. field:: public TestEnum VALUE_A

      Enum ValueA documentation string.


   .. field:: public TestEnum VALUE_B

      Enum ValueB documentation string.


   .. field:: public TestEnum VALUE_C

      Enum ValueC documentation string.



.. type:: public class CustomException
