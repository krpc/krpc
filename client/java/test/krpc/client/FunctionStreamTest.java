package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertThrows;
import static org.junit.Assert.assertTrue;

import java.io.IOException;
import java.math.BigDecimal;
import java.math.BigInteger;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.atomic.AtomicLong;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.KRPC.Type;
import krpc.client.services.TestService;
import krpc.client.services.TestService.TestClass;
import org.junit.Before;
import org.junit.Test;

/**
 * How the Java type of an argument converts to the type of a function's parameter. The
 * conformance suite covers the rest of server side functions, except a function with no value,
 * as the testees build calls only of procedures with a value.
 */
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

  /** A function that returns its one parameter, of the given type. */
  private Expression identity(Type type) throws RPCException {
    Expression x = Expression.parameter(connection, "x", type);
    return Expression.lambda(connection, Arrays.asList(x), x);
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
  public void testNumberArgumentWidensAsJavaWidensIt() throws RPCException {
    Expression toDouble = identity(Type.double_(connection));
    assertEquals(2.0, (double) connection.run(toDouble, 2), 0);
    assertEquals(2.0, (double) connection.run(toDouble, (short) 2), 0);
    assertEquals(3, (int) connection.run(identity(Type.int_(connection)), (byte) 3));
  }

  @Test
  public void testNumberArgumentOfAnotherClassDoesNotConvert() throws RPCException {
    Expression toDouble = identity(Type.double_(connection));
    IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(toDouble, new BigDecimal("1e400")));
    assertTrue(e.getMessage().contains("Argument 1 is a java.math.BigDecimal"));
    assertThrows(IllegalArgumentException.class,
        () -> connection.run(toDouble, BigInteger.ONE));
    assertThrows(IllegalArgumentException.class,
        () -> connection.run(toDouble, new AtomicLong(1)));
    assertThrows(IllegalArgumentException.class,
        () -> connection.run(identity(Type.uLong(connection)), -1.5));
  }

  @Test
  public void testNarrowerSignedArgumentConvertsToUnsignedParameterIfNotNegative()
      throws RPCException {
    Expression toUnsignedLong = identity(Type.uLong(connection));
    assertEquals(5L, (long) connection.run(toUnsignedLong, 5));
    IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(toUnsignedLong, -5));
    assertTrue(e.getMessage().contains("Argument 1 is negative"));
    Expression toUnsignedInt = identity(Type.uInt(connection));
    assertEquals(5, (int) connection.run(toUnsignedInt, (short) 5));
    assertThrows(IllegalArgumentException.class,
        () -> connection.run(toUnsignedInt, (short) -5));
    // A long is wider than an unsigned 32-bit parameter
    assertThrows(IllegalArgumentException.class, () -> connection.run(toUnsignedInt, 5L));
  }

  @Test
  public void testUnsignedParameterTakesItsOwnJavaTypeBitForBit() throws RPCException {
    // The maximum of each unsigned type reads back as -1
    Expression toUnsignedLong = identity(Type.uLong(connection));
    long maxLong = connection.run(toUnsignedLong, -1L);
    assertEquals(-1L, maxLong);
    assertEquals(-1L, (long) connection.run(toUnsignedLong, maxLong));
    Expression toUnsignedInt = identity(Type.uInt(connection));
    int maxInt = connection.run(toUnsignedInt, -1);
    assertEquals(-1, maxInt);
    assertEquals(-1, (int) connection.run(toUnsignedInt, maxInt));
  }

  @Test
  public void testCollectionItemDoesNotWiden() throws RPCException {
    Expression doubles = Expression.parameter(
        connection, "l", Type.listType(connection, Type.double_(connection)));
    Expression count = Expression.lambda(
        connection, Arrays.asList(doubles), Expression.count(connection, doubles));
    assertEquals(2, (int) connection.run(count, Arrays.asList(1.0, 2.0)));
    IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
        () -> connection.run(count, Arrays.asList(1.0, 2)));
    assertTrue(e.getMessage().contains(
        "The item at index 1 of argument 1 is a java.lang.Integer"));
  }
}
