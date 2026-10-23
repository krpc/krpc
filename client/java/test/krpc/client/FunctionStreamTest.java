package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertNotEquals;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertThrows;
import static org.junit.Assert.assertTrue;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.atomic.AtomicLong;
import krpc.client.services.KRPC;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.KRPC.TypeCode;
import krpc.client.services.TestService;
import krpc.client.services.TestService.TestClass;
import org.javatuples.Pair;
import org.javatuples.Triplet;
import org.junit.Before;
import org.junit.Test;

/** Tests for function streams. */
public class FunctionStreamTest {

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
  public void testFunctionStream()
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
        connection.getCall(TestService.class, "counter", "JavaFunctionStream.Computed", 1));
    // int RPC result * double constant promotes to double
    Expression expr = Expression.multiply(
        connection, counter, Expression.constantDouble(connection, 0.5));
    Stream<Double> stream = connection.addStream(expr);
    double value = stream.get();
    assertTrue(value >= 0.5);
    assertEquals(0, value * 2 % 1, 0);
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
      constants.add(Expression.constantObject(connection, RemoteObject.getObjectId(obj)));
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
            Expression.lambda(connection, Arrays.asList(param), getProperty)));
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
    int value = connection.run(expr);
    assertEquals(40, value);
  }

  @Test
  public void testNullableElements() throws RPCException, StreamException {
    List<Integer> ints = Arrays.asList(5, null);
    Expression expr = Expression.call(
        connection, connection.getCall(TestService.class, "echoListOfNullableInts", ints));
    List<Integer> value = connection.run(expr);
    assertEquals(ints, value);
    Stream<List<Integer>> stream = connection.addStream(expr);
    assertEquals(ints, stream.get());
    stream.remove();
  }

  @Test
  public void testNullableObjectElements() throws RPCException, StreamException {
    TestClass obj = testService.createTestObject("nullableelements");
    List<TestClass> objects = Arrays.asList(obj, null);
    Expression expr = Expression.call(
        connection, connection.getCall(TestService.class, "echoListOfNullableObjects", objects));
    List<TestClass> value = connection.run(expr);
    assertEquals(objects, value);
    Stream<List<TestClass>> stream = connection.addStream(expr);
    assertEquals(objects, stream.get());
    stream.remove();
  }

  @Test
  public void testRunFunctionNullResult()
      throws RPCException, StreamException {
    TestClass obj = testService.createTestObject("runnull");
    Expression expr =
        Expression.call(connection, connection.getCall(obj, "getObjectProperty"));
    // The property is nullable and unset, so the function's value is null
    TestClass value = connection.run(expr);
    assertNull(value);
    Stream<TestClass> stream = connection.addStream(expr);
    assertNull(stream.get());
    stream.remove();
    TestClass other = testService.createTestObject("runnull2");
    obj.setObjectProperty(other);
    assertEquals(other, connection.<TestClass>run(expr));
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
    arguments.put(0, Expression.constantObject(connection, RemoteObject.getObjectId(obj)));
    arguments.put(1, Expression.constantInt(connection, 42));
    Expression expr = Expression.callWithArguments(connection, call, arguments);
    assertNull(connection.run(expr));
    assertEquals(42, obj.getIntProperty());
    assertThrows(StreamException.class, () -> connection.addStream(expr));
  }

  @Test
  public void testStruct()
      throws RPCException, StreamException {
    Expression value = Expression.call(
        connection,
        connection.getCall(TestService.class, "counterStruct", "JavaFunctionStream.Struct"));
    Stream<TestService.TestStruct> stream = connection.addStream(value);
    assertEquals("JavaFunctionStream.Struct", stream.get().getStringField());
    stream.remove();
    Stream<String> field = connection.addStream(
        Expression.getField(connection, value, "StringField"));
    assertEquals("JavaFunctionStream.Struct", field.get());
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
    TestService.TestStruct result = connection.run(value);
    assertEquals(3, result.getIntField());
    assertEquals("built", result.getStringField());
    assertEquals(Arrays.asList(7), result.getListField());
  }

  @Test
  public void testTupleAndDictionary()
      throws RPCException, StreamException {
    Expression tuple = Expression.createTuple(
        connection,
        Arrays.asList(
            Expression.constantInt(connection, 1),
            Expression.constantString(connection, "foo"),
            Expression.constantDouble(connection, 2.5)));
    Triplet<Integer, String, Double> expected =
        new Triplet<Integer, String, Double>(1, "foo", 2.5);
    assertEquals(expected, connection.run(tuple));
    Stream<Triplet<Integer, String, Double>> stream = connection.addStream(tuple);
    assertEquals(expected, stream.get());
    stream.remove();
    Expression dictionary = Expression.createDictionary(
        connection,
        Arrays.asList(
            Expression.constantString(connection, "a"),
            Expression.constantString(connection, "b")),
        Arrays.asList(
            Expression.createList(
                connection, Arrays.asList(Expression.constantInt(connection, 1))),
            Expression.createList(connection, Arrays.asList(
                Expression.constantInt(connection, 2), Expression.constantInt(connection, 3)))));
    Map<String, List<Integer>> expectedDictionary = new HashMap<String, List<Integer>>();
    expectedDictionary.put("a", Arrays.asList(1));
    expectedDictionary.put("b", Arrays.asList(2, 3));
    assertEquals(expectedDictionary, connection.run(dictionary));
  }

  @Test
  public void testTypeParameterThatCannotHoldTheValue()
      throws RPCException, StreamException {
    Expression expr = Expression.constantDouble(connection, 1.5);
    Stream<Integer> stream = connection.addStream(expr);
    assertThrows(ClassCastException.class, () -> stream.get().intValue());
    stream.remove();
  }

  @Test
  public void testEventFromFunction()
      throws RPCException, StreamException {
    Event event = connection.addEvent(Expression.constantBool(connection, true));
    synchronized (event.getCondition()) {
      event.waitFor();
    }
    event.remove();
  }

  @Test(timeout = 30000)
  public void testEventsAndStreamsOverOneFunction()
      throws RPCException, StreamException {
    // Each add creates its own stream, and removing one leaves the other live
    Expression never = Expression.constantBool(connection, false);
    Event first = connection.addEvent(never);
    Event second = connection.addEvent(never);
    assertNotEquals(first, second);
    first.remove();
    synchronized (first.getCondition()) {
      assertThrows(StreamException.class, () -> first.waitFor());
    }
    synchronized (second.getCondition()) {
      assertFalse(second.waitForWithTimeout(0.1));
    }
    second.remove();
    TestClass obj = testService.createTestObject("ownstream");
    obj.setIntProperty(3);
    Expression value = Expression.call(connection, connection.getCall(obj, "getIntProperty"));
    Stream<Integer> stream1 = connection.addStream(value);
    Stream<Integer> stream2 = connection.addStream(value);
    assertNotEquals(stream1, stream2);
    stream1.remove();
    assertThrows(StreamException.class, () -> stream1.get());
    assertEquals(Integer.valueOf(3), stream2.get());
    stream2.remove();
  }

  @Test(timeout = 30000)
  public void testRemovingStreamAgainThroughAnotherHandle()
      throws RPCException, StreamException {
    testService.setStringProperty("live");
    Expression value = Expression.call(
        connection, connection.getCall(TestService.class, "getStringProperty"));
    Stream<String> first = connection.addStream(value);
    Stream<String> second = new Stream<String>(
        connection, first.stream.getReturnType(), first.stream.getId());
    first.remove();
    // The server can give the new stream the removed one's id
    Stream<String> third = connection.addStream(value);
    second.remove();
    assertEquals("live", third.get());
    third.remove();
  }

  @Test(timeout = 30000)
  public void testCallbacksSkipAnError()
      throws RPCException, StreamException, InterruptedException {
    List<Throwable> uncaught = Collections.synchronizedList(new ArrayList<Throwable>());
    Thread.UncaughtExceptionHandler handler = Thread.getDefaultUncaughtExceptionHandler();
    Thread.setDefaultUncaughtExceptionHandler((thread, exn) -> uncaught.add(exn));
    try {
      Expression fails = Expression.call(
          connection, connection.getCall(TestService.class, "throwInvalidOperationException"));
      Stream<Integer> stream = connection.addStream(fails);
      stream.addCallback((Integer value) -> {});
      Event event = connection.addEvent(Expression.equal(
          connection, fails, Expression.constantInt(connection, 0)));
      event.addCallback(() -> {});
      synchronized (stream.getCondition()) {
        assertThrows(IllegalStateException.class, () -> stream.waitForUpdate());
      }
      synchronized (event.getCondition()) {
        assertThrows(IllegalStateException.class, () -> event.waitFor());
      }
      assertThrows(IllegalStateException.class, () -> stream.get());
      // The callbacks run on the update thread after it notifies the condition
      Thread.sleep(200);
      assertEquals(Collections.emptyList(), uncaught);
      stream.remove();
      event.remove();
    } finally {
      Thread.setDefaultUncaughtExceptionHandler(handler);
    }
  }

  @Test(timeout = 30000)
  public void testRemovalWakesBlockedWait()
      throws RPCException, StreamException, InterruptedException {
    Expression never = Expression.constantBool(connection, false);
    Event event = connection.addEvent(never);
    Stream<Integer> stream = connection.addStream(Expression.constantInt(connection, 1));
    List<Throwable> errors = Collections.synchronizedList(new ArrayList<Throwable>());
    Thread eventWaiter = new Thread(() -> {
      synchronized (event.getCondition()) {
        try {
          event.waitFor();
        } catch (RPCException | StreamException exn) {
          errors.add(exn);
        }
      }
    });
    Thread streamWaiter = new Thread(() -> {
      synchronized (stream.getCondition()) {
        try {
          stream.get();
          stream.waitForUpdate();
        } catch (RPCException | StreamException exn) {
          errors.add(exn);
        }
      }
    });
    eventWaiter.start();
    streamWaiter.start();
    while (eventWaiter.getState() != Thread.State.WAITING
        || streamWaiter.getState() != Thread.State.WAITING) {
      Thread.sleep(10);
    }
    event.remove();
    eventWaiter.join();
    stream.remove();
    streamWaiter.join();
    assertEquals(2, errors.size());
    assertTrue(errors.get(0) instanceof StreamException);
    assertTrue(errors.get(1) instanceof StreamException);
  }

  @Test
  public void testReturnType()
      throws RPCException {
    Expression expr = Expression.constantDouble(connection, 1.5);
    assertEquals(TypeCode.DOUBLE, expr.getReturnType().getCode());
    TestClass obj = testService.createTestObject("exprReturnType");
    Type objType =
        Expression.constantObject(connection, RemoteObject.getObjectId(obj)).getReturnType();
    assertEquals(TypeCode.CLASS, objType.getCode());
    assertEquals("TestService", objType.getService());
    assertEquals("TestClass", objType.getName());
  }

  @Test
  public void testRunFunctionWithArguments()
      throws RPCException {
    Expression value = Expression.parameter(connection, "x", Type.double_(connection));
    Expression factor = Expression.parameter(connection, "n", Type.int_(connection));
    Expression function = Expression.lambda(
        connection, Arrays.asList(value, factor), Expression.multiply(connection, value, factor));
    assertEquals(7.5, (double) connection.run(function, 2.5, 3), 0);
    // An int widens to a double parameter
    assertEquals(-8.0, (double) connection.run(function, 2, -4), 0);
    IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(function, 1.0));
    assertTrue(e.getMessage().contains("takes 2 arguments, got 1"));
    // A short widens as Java widens it, and a wrong type names its position
    assertEquals(6.0, (double) connection.run(function, (short) 2, (byte) 3), 0);
    e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(function, "x", 3));
    assertTrue(e.getMessage().contains("Argument 1 is a java.lang.String"));
    // A narrower whole number that is not negative converts to an unsigned parameter
    Expression unsigned = Expression.parameter(connection, "u", Type.uLong(connection));
    Expression identity = Expression.lambda(connection, Arrays.asList(unsigned), unsigned);
    assertEquals(5L, (long) connection.run(identity, 5));
    assertEquals(5L, (long) connection.run(identity, 5L));
    e = assertThrows(IllegalArgumentException.class, () -> connection.run(identity, -5));
    assertTrue(e.getMessage().contains("Argument 1 is negative"));
    // A long holds an unsigned 64-bit value bit for bit, so the maximum reads back as -1
    long max = connection.run(identity, -1L);
    assertEquals(-1L, max);
    assertEquals(-1L, (long) connection.run(identity, max));
    // A number that is not whole is the wrong type, negative or not
    e = assertThrows(IllegalArgumentException.class, () -> connection.run(identity, -1.5));
    assertTrue(e.getMessage().contains("Argument 1 is a java.lang.Double"));
    // A long converts to an unsigned parameter only as wide as itself
    Expression unsignedInt = Expression.parameter(connection, "u", Type.uInt(connection));
    Expression identityInt = Expression.lambda(
        connection, Arrays.asList(unsignedInt), unsignedInt);
    assertEquals(5, (int) connection.run(identityInt, (short) 5));
    assertThrows(IllegalArgumentException.class, () -> connection.run(identityInt, 5L));
    assertThrows(IllegalArgumentException.class, () -> connection.run(identityInt, (short) -5));
    // An int holds an unsigned 32-bit value bit for bit
    int maxInt = connection.run(identityInt, -1);
    assertEquals(-1, maxInt);
    assertEquals(-1, (int) connection.run(identityInt, maxInt));
  }

  @Test
  public void testRunFunctionWithNumbersJavaDoesNotWiden()
      throws RPCException {
    Expression value = Expression.parameter(connection, "x", Type.double_(connection));
    Expression identity = Expression.lambda(connection, Arrays.asList(value), value);
    IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(identity, new BigDecimal("1e400")));
    assertTrue(e.getMessage().contains("Argument 1 is a java.math.BigDecimal"));
    e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(identity, BigInteger.ONE));
    assertTrue(e.getMessage().contains("Argument 1 is a java.math.BigInteger"));
    e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(identity, new AtomicLong(1)));
    assertTrue(e.getMessage().contains("Argument 1 is a java.util.concurrent.atomic.AtomicLong"));
  }

  @Test
  public void testRunFunctionWithArgumentsThatFailToEncode()
      throws RPCException {
    Expression value = Expression.parameter(connection, "x", Type.double_(connection));
    Expression function = Expression.lambda(connection, Arrays.asList(value), value);
    IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(function));
    assertTrue(e.getMessage().contains("takes 1 argument, got 0"));
    // A null in a list that cannot hold one
    Expression list = Expression.parameter(
        connection, "l", Type.listType(connection, Type.int_(connection)));
    Expression count = Expression.lambda(
        connection, Arrays.asList(list), Expression.count(connection, list));
    assertEquals(2, (int) connection.run(count, Arrays.asList(1, 2)));
    e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(count, Arrays.asList(1, null)));
    assertTrue(e.getMessage().contains("The item at index 1 of argument 1 is null"));
    // A wrong number in a list names the item, as a number does not widen there
    e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(count, Arrays.asList(1, 2.5)));
    assertTrue(e.getMessage().contains(
        "The item at index 1 of argument 1 is a java.lang.Double, where the function takes a "
            + "SINT32"));
    // A pair for a triple
    Expression tuple = Expression.parameter(connection, "t", Type.tupleType(connection,
        Arrays.asList(Type.int_(connection), Type.int_(connection), Type.int_(connection))));
    Expression identity = Expression.lambda(connection, Arrays.asList(tuple), tuple);
    e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(identity, Pair.with(1, 2)));
    assertTrue(e.getMessage().contains("Argument 1 has 2 items"));
    e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(identity, Triplet.with(1, 2, "3")));
    assertTrue(e.getMessage().contains(
        "The item at index 2 of argument 1 is a java.lang.String"));
  }

  @Test
  public void testRunFunctionWithObjectAndNullArguments()
      throws RPCException {
    TestClass obj = testService.createTestObject("argument");
    obj.setIntProperty(4);
    Expression param = Expression.parameter(
        connection, "obj", Type.classType(connection, "TestService", "TestClass"));
    Expression label = Expression.parameter(
        connection, "label", Type.nullableType(connection, Type.string(connection)));
    Map<Integer, Expression> arguments = new HashMap<Integer, Expression>();
    arguments.put(0, param);
    Expression getProperty = Expression.callWithArguments(
        connection, connection.getCall(obj, "getIntProperty"), arguments);
    Expression function = Expression.lambda(connection, Arrays.asList(param, label),
        Expression.conditional(connection, Expression.isNull(connection, label),
            getProperty, Expression.constantInt(connection, 0)));
    assertEquals(4, (int) connection.run(function, obj, null));
    assertEquals(0, (int) connection.run(function, obj, "label"));
    // The server checks a null for a parameter that is not nullable, and an object's class
    assertThrows(IllegalArgumentException.class, () -> connection.run(function, null, "label"));
    Expression otherClass = Expression.constantInt(connection, 0);
    assertThrows(IllegalArgumentException.class,
        () -> connection.run(function, otherClass, "label"));
  }
}
