package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertTrue;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import krpc.client.services.KRPC;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.KRPC.TypeCode;
import krpc.client.services.TestService;
import krpc.client.services.TestService.TestClass;
import org.junit.Before;
import org.junit.Test;

/** Tests for expression streams. */
public class ExpressionStreamTest {

  private Connection connection;
  private TestService testService;

  /** Set up test. */
  @Before
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void setup() throws IOException {
    connection = TestUtils.connect();
    testService = TestService.newInstance(connection);
  }

  @Test
  public void testExpressionStream()
      throws RPCException, StreamException {
    testService.setStringProperty("foo");
    Expression expr = Expression.call(
        connection, connection.getCall(TestService.class, "getStringProperty"));
    Stream<String> stream = connection.addStream(expr);
    assertEquals("foo", stream.get());
    stream.remove();
  }

  @Test
  public void testComputedValue()
      throws RPCException, StreamException {
    Expression counter = Expression.call(
        connection,
        connection.getCall(TestService.class, "counter", "JavaExpressionStream.Computed", 1));
    // int RPC result * double constant promotes to double
    Expression expr = Expression.multiply(
        connection, counter, Expression.constantDouble(connection, 0.5));
    Stream<Double> stream = connection.addStream(expr);
    assertTrue(stream.get() > 0);
    stream.remove();
  }

  @Test
  public void testPerElementCall()
      throws RPCException, StreamException {
    List<TestClass> objs = new ArrayList<TestClass>();
    List<Expression> constants = new ArrayList<Expression>();
    for (int i = 0; i < 3; i++) {
      TestClass obj = testService.createTestObject("expr" + i);
      obj.setIntProperty(i + 1);
      objs.add(obj);
      constants.add(Expression.constantObject(connection, obj.id));
    }
    Expression objects = Expression.createList(connection, constants);
    Expression param = Expression.parameter(
        connection, "x", Type.classType(connection, "TestService", "TestClass"));
    Expression getProperty = Expression.callWithArguments(
        connection,
        connection.getCall(objs.get(0), "getIntProperty"),
        Collections.singletonMap(0, param));
    Expression selected = Expression.toList(
        connection,
        Expression.select(
            connection, objects,
            Expression.function(connection, Arrays.asList(param), getProperty)));
    Stream<List<Integer>> stream = connection.addStream(selected);
    assertEquals(Arrays.asList(1, 2, 3), stream.get());
    stream.remove();
  }

  @Test
  public void testRunFunction()
      throws RPCException {
    TestClass obj = testService.createTestObject("run");
    obj.setIntProperty(20);
    Expression expr = Expression.multiply(
        connection,
        Expression.call(connection, connection.getCall(obj, "getIntProperty")),
        Expression.constantInt(connection, 2));
    int value = connection.runFunction(expr);
    assertEquals(40, value);
  }

  @Test
  public void testRunFunctionSideEffects()
      throws RPCException {
    TestClass obj = testService.createTestObject("runeffect");
    obj.setIntProperty(1);
    krpc.schema.KRPC.ProcedureCall call = krpc.schema.KRPC.ProcedureCall.newBuilder()
        .setService("TestService")
        .setProcedure("TestClass_set_IntProperty")
        .build();
    Map<Integer, Expression> arguments = new HashMap<Integer, Expression>();
    arguments.put(0, Expression.constantObject(connection, obj.id));
    arguments.put(1, Expression.constantInt(connection, 42));
    Expression expr = Expression.callWithArguments(connection, call, arguments);
    assertNull(connection.runFunction(expr));
    assertEquals(42, obj.getIntProperty());
  }

  @Test
  public void testStruct()
      throws RPCException, StreamException {
    Expression value = Expression.call(
        connection,
        connection.getCall(TestService.class, "counterStruct", "JavaExpressionStream.Struct"));
    Stream<TestService.TestStruct> stream = connection.addStream(value);
    assertEquals("JavaExpressionStream.Struct", stream.get().getStringField());
    stream.remove();
    Stream<String> field = connection.addStream(
        Expression.getField(connection, value, "StringField"));
    assertEquals("JavaExpressionStream.Struct", field.get());
    field.remove();
  }

  @Test
  public void testCreateStruct()
      throws RPCException {
    Expression value = Expression.createStruct(
        connection,
        Type.structType(connection, "TestService", "TestStruct"),
        Arrays.asList(
            Expression.constantInt(connection, 3),
            Expression.constantString(connection, "built"),
            Expression.cast(
                connection,
                Expression.constantInt(connection, 0),
                Type.enumerationType(connection, "TestService", "TestEnum")),
            Expression.createList(
                connection, Arrays.asList(Expression.constantInt(connection, 7)))));
    TestService.TestStruct result = connection.runFunction(value);
    assertEquals(3, result.getIntField());
    assertEquals("built", result.getStringField());
    assertEquals(Arrays.asList(7), result.getListField());
  }

  @Test
  public void testReturnType()
      throws RPCException, StreamException {
    Expression expr = Expression.constantDouble(connection, 1.5);
    assertEquals(TypeCode.DOUBLE, expr.getReturnType().getCode());
    TestClass obj = testService.createTestObject("exprReturnType");
    Type objType = Expression.constantObject(connection, obj.id).getReturnType();
    assertEquals(TypeCode.CLASS, objType.getCode());
    assertEquals("TestService", objType.getService());
    assertEquals("TestClass", objType.getName());
  }
}
