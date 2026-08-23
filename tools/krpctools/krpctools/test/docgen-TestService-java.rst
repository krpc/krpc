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
   

   .. method:: TestStruct counterStruct(String id)

      Returns a struct whose IntField counts the number of times it has been called,
      so that a stream on it changes on every update.

      :param String id:
   

   .. method:: TestClass createTestObject(String value)



      :param String value:
   

   .. method:: String deprecatedProcedure(float value)

      .. warning:: Deprecated. Use :meth:`floatToString(float)` instead.

      Deprecated procedure documentation string.

      :param float value:
   

   .. method:: String deprecatedProcedureNoMessage(float value)

      .. warning:: Deprecated.

      Deprecated procedure with no reason documentation string.

      :param float value:
   

   .. method:: String getDeprecatedProperty()

   .. method:: void setDeprecatedProperty(String value)

      .. warning:: Deprecated. Use :meth:`getStringProperty()` instead.

      Deprecated property documentation string.

   

   .. method:: java.util.Map<Integer,Boolean> dictionaryDefault(java.util.Map<Integer,Boolean> x)



      :param java.util.Map<Integer,Boolean> x:
   

   .. method:: java.util.List<Double> doubleSpecialDefaults(double nan, double infinity, double negativeInfinity, double maximum, double lowest)

      Procedure whose defaults are the special values of double.

      :param double nan:
      :param double infinity:
      :param double negativeInfinity:
      :param double maximum:
      :param double lowest:
   

   .. method:: String doubleToString(double value)



      :param double value:
   

   .. method:: String dumpExpressionTree(KRPC.Expression expression)

      Returns a dump of the expression tree of the given server side expression,
      as rendered by KRPC.Service.KRPC.ExpressionTreePrinter. Used by tests to
      verify the trees that the expression API generates.

      :param KRPC.Expression expression:
   

   .. method:: int echoNullableInt(int value)



      :param int value:
   

   .. method:: java.util.List<Integer> echoNullableList(java.util.List<Integer> l)



      :param java.util.List<Integer> l:
   

   .. method:: String echoNullableString(String value)



      :param String value:
   

   .. method:: TestClass echoTestObject(TestClass value)



      :param TestClass value:
   

   .. method:: java.util.List<String> emptyListDefault(java.util.List<String> x)



      :param java.util.List<String> x:
   

   .. method:: TestEnum enumDefaultArg(TestEnum x)



      :param TestEnum x:
   

   .. method:: TestEnum enumEcho(TestEnum x)



      :param TestEnum x:
   

   .. method:: java.util.List<TestEnum> enumListDefault(java.util.List<TestEnum> x)



      :param java.util.List<TestEnum> x:
   

   .. method:: TestEnum enumReturn()



   

   .. method:: java.util.List<Float> floatSpecialDefaults(float nan, float infinity, float negativeInfinity, float maximum, float lowest, float fraction)

      Procedure whose defaults are the special values of float, plus a finite fraction
      that no float can hold exactly.

      :param float nan:
      :param float infinity:
      :param float negativeInfinity:
      :param float maximum:
      :param float lowest:
      :param float fraction:
   

   .. method:: String floatToString(float value)

      Procedure documentation string.

      :param float value:
   

   .. method:: java.util.Map<String,Integer> incrementDictionary(java.util.Map<String,Integer> d)



      :param java.util.Map<String,Integer> d:
   

   .. method:: java.util.List<Integer> incrementList(java.util.List<Integer> l)



      :param java.util.List<Integer> l:
   

   .. method:: java.util.List<TestStruct> incrementListOfStructs(java.util.List<TestStruct> l)



      :param java.util.List<TestStruct> l:
   

   .. method:: java.util.Map<String,java.util.List<Integer>> incrementNestedCollection(java.util.Map<String,java.util.List<Integer>> d)



      :param java.util.Map<String,java.util.List<Integer>> d:
   

   .. method:: java.util.Set<Integer> incrementSet(java.util.Set<Integer> h)



      :param java.util.Set<Integer> h:
   

   .. method:: org.javatuples.Pair<Integer,Long> incrementTuple(org.javatuples.Pair<Integer,Long> t)



      :param org.javatuples.Pair<Integer,Long> t:
   

   .. method:: java.util.List<Integer> int32SpecialDefaults(int maximum, int minimum)

      Procedure whose defaults are the extremes of int.

      :param int maximum:
      :param int minimum:
   

   .. method:: String int32ToString(int value)



      :param int value:
   

   .. method:: java.util.List<Long> int64SpecialDefaults(long maximum, long minimum)

      Procedure whose defaults are the extremes of long.

      :param long maximum:
      :param long minimum:
   

   .. method:: String int64ToString(long value)



      :param long value:
   

   .. method:: java.util.List<Integer> listDefault(java.util.List<Integer> x)



      :param java.util.List<Integer> x:
   

   .. method:: TestNestedStruct nestedStructEcho(TestNestedStruct x)



      :param TestNestedStruct x:
   

   .. method:: TestClass notNullableObject(TestClass value)



      :param TestClass value:
   

   .. method:: TestClass getNullableObject()

   .. method:: void setNullableObject(TestClass value)



   

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
   

   .. method:: TestStruct structDefault(TestStruct x)



      :param TestStruct x:
   

   .. method:: TestStruct structEcho(TestStruct x)



      :param TestStruct x:
   

   .. method:: TestStruct structEchoNullable(TestStruct x)



      :param TestStruct x:
   

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
   

   .. method:: java.util.List<Integer> uint32SpecialDefaults(int maximum)

      Procedure whose default is the largest uint.

      :param int maximum:
   

   .. method:: java.util.List<Long> uint64SpecialDefaults(long maximum)

      Procedure whose default is the largest ulong.

      :param long maximum:
   



.. type:: public class TestClass

   Class documentation string.

   .. method:: TestClass echoNullableObject(TestClass value)



      :param TestClass value:
   

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
   

   .. method:: static TestClass staticNullableObject(Connection connection, TestClass value)



      :param TestClass value:
   

   .. method:: void setStringPropertyPrivateGet(String value)



   

   .. method:: String getStringPropertyPrivateSet()




   



.. type:: public class DeprecatedClass

   .. warning:: Deprecated. Use :type:`TestClass` instead.

   Deprecated class documentation string.

   .. method:: String deprecatedMethod()

      .. warning:: Deprecated. Use :meth:`TestClass.getValue()` instead.

      Deprecated method documentation string.

   



.. type:: public enum TestEnum

   Enum documentation string.


   .. field:: public TestEnum VALUE_A

      Enum ValueA documentation string.


   .. field:: public TestEnum VALUE_B

      Enum ValueB documentation string.


   .. field:: public TestEnum VALUE_C

      Enum ValueC documentation string.



.. type:: public enum DeprecatedEnum

   .. warning:: Deprecated. Use :type:`TestEnum` instead.

   Deprecated enum documentation string.


   .. field:: public DeprecatedEnum VALUE_A

      Deprecated enum ValueA documentation string.


   .. field:: public DeprecatedEnum VALUE_B

      .. warning:: Deprecated. Use :meth:`DeprecatedEnum.VALUE_A` instead.

      Deprecated enum ValueB documentation string.



.. type:: public class TestStruct

   Struct documentation string.


   .. method:: int getIntField()

      Struct IntField documentation string.

   .. method:: String getStringField()



   .. method:: TestEnum getEnumField()



   .. method:: java.util.List<Integer> getListField()




.. type:: public class TestNestedStruct

   Nested struct documentation string.


   .. method:: TestStruct getStructField()



   .. method:: TestClass getObjectField()



   .. method:: String getStringField()




.. type:: public class DeprecatedStruct

   .. warning:: Deprecated. Use :type:`TestStruct` instead.

   Deprecated struct documentation string.


   .. method:: int getValue()

      Deprecated struct Value documentation string.

   .. method:: int getOldValue()

      .. warning:: Deprecated. Use :meth:`DeprecatedStruct.getValue()` instead.

      Deprecated struct OldValue documentation string.


.. type:: public class CustomException





.. type:: public class DeprecatedException

   .. warning:: Deprecated. Use CustomException instead.

   Deprecated exception documentation string.
