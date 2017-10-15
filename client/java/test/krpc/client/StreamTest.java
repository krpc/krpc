package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

import krpc.client.services.TestService;
import krpc.client.services.TestService.CustomException;
import krpc.client.services.TestService.TestClass;

import org.junit.Before;
import org.junit.Test;

import java.io.IOException;

public class StreamTest {

  private Connection connection;
  private TestService testService;

  @Before
  public void setup() throws IOException {
    connection = TestUtils.connect();
    testService = TestService.newInstance(connection);
  }

  private void pause() {
    try {
      Thread.sleep(50);
    } catch (InterruptedException exn) {
      throw new RuntimeException(exn);
    }
  }

  @Test
  public void testMethod()
      throws RPCException, StreamException, NoSuchMethodException {
    Stream<String> stream = connection.addStream(TestService.class, "floatToString", 3.14159f);
    for (int i = 0; i < 5; i++) {
      assertEquals("3.14159", stream.get());
      pause();
    }
  }

  @Test
  public void testProperty()
      throws RPCException, StreamException, NoSuchMethodException {
    testService.setStringProperty("foo");
    Stream<String> stream = connection.addStream(TestService.class, "getStringProperty");
    for (int i = 0; i < 5; i++) {
      assertEquals("foo", stream.get());
      pause();
    }
  }

  @Test
  public void testClassMethod()
      throws RPCException, StreamException, NoSuchMethodException {
    TestClass obj = testService.createTestObject("bob");
    Stream<String> stream = connection.addStream(obj, "floatToString", 3.14159f);
    for (int i = 0; i < 5; i++) {
      assertEquals("bob3.14159", stream.get());
      pause();
    }
  }

  @Test
  public void testClassStaticMethod()
      throws RPCException, StreamException, NoSuchMethodException {
    // FIXME: Java does not support default parameter values, so have to pass ""
    Stream<String> stream = connection.addStream(
        TestClass.class, "staticMethod", connection, "foo", "");
    for (int i = 0; i < 5; i++) {
      assertEquals("jebfoo", stream.get());
      pause();
    }
  }

  @Test
  public void testClassProperty()
      throws RPCException, StreamException, NoSuchMethodException {
    TestClass obj = testService.createTestObject("jeb");
    obj.setIntProperty(42);
    Stream<Integer> stream = connection.addStream(obj, "getIntProperty");
    for (int i = 0; i < 5; i++) {
      assertEquals(42, (int) stream.get());
      pause();
    }
  }

  @Test
  public void testCounter()
      throws RPCException, StreamException, NoSuchMethodException {
    int count = -1;
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testCounter", 1);
    for (int i = 0; i < 5; i++) {
      assertTrue(count < stream.get());
      count = stream.get();
      pause();
    }
  }

  @Test
  public void testNested()
      throws RPCException, StreamException, NoSuchMethodException {
    Stream<String> x0 = connection.addStream(TestService.class, "floatToString", 0.123f);
    Stream<String> x1 = connection.addStream(TestService.class, "floatToString", 1.234f);
    for (int i = 0; i < 5; i++) {
      assertEquals("0.123", x0.get());
      assertEquals("1.234", x1.get());
      pause();
    }
  }

  @Test
  @SuppressWarnings("checkstyle:emptycatchblock")
  public void testInerleaved()
      throws RPCException, StreamException, NoSuchMethodException {
    Stream<String> stream0 = connection.addStream(TestService.class, "int32ToString", 0);
    assertEquals("0", stream0.get());

    pause();
    assertEquals("0", stream0.get());

    Stream<String> stream1 = connection.addStream(TestService.class, "int32ToString", 1);
    assertEquals("0", stream0.get());
    assertEquals("1", stream1.get());

    pause();
    assertEquals("0", stream0.get());
    assertEquals("1", stream1.get());

    stream1.remove();
    assertEquals("0", stream0.get());
    try {
      stream1.get();
      fail();
    } catch (StreamException exn) {
    }

    pause();
    assertEquals("0", stream0.get());
    try {
      stream1.get();
      fail();
    } catch (StreamException exn) {
    }

    Stream<String> stream2 = connection.addStream(TestService.class, "int32ToString", 2);
    assertEquals("0", stream0.get());
    try {
      stream1.get();
      fail();
    } catch (StreamException exn) {
    }
    assertEquals("2", stream2.get());

    pause();
    assertEquals("0", stream0.get());
    try {
      stream1.get();
      fail();
    } catch (StreamException exn) {
    }
    assertEquals("2", stream2.get());

    stream0.remove();
    try {
      stream0.get();
      fail();
    } catch (StreamException exn) {
    }
    try {
      stream1.get();
      fail();
    } catch (StreamException exn) {
    }
    assertEquals("2", stream2.get());

    pause();
    try {
      stream0.get();
      fail();
    } catch (StreamException exn) {
    }
    try {
      stream1.get();
      fail();
    } catch (StreamException exn) {
    }
    assertEquals("2", stream2.get());

    stream2.remove();
    try {
      stream0.get();
      fail();
    } catch (StreamException exn) {
    }
    try {
      stream1.get();
      fail();
    } catch (StreamException exn) {
    }
    try {
      stream2.get();
      fail();
    } catch (StreamException exn) {
    }

    pause();
    try {
      stream0.get();
      fail();
    } catch (StreamException exn) {
    }
    try {
      stream1.get();
      fail();
    } catch (StreamException exn) {
    }
    try {
      stream2.get();
      fail();
    } catch (StreamException exn) {
    }
  }

  @Test
  @SuppressWarnings("checkstyle:emptycatchblock")
  public void testRemoveStreamTwice()
      throws RPCException, StreamException, NoSuchMethodException {
    Stream<String> stream = connection.addStream(TestService.class, "int32ToString", 0);
    assertEquals("0", stream.get());

    pause();
    assertEquals("0", stream.get());

    stream.remove();
    try {
      stream.get();
      fail();
    } catch (StreamException exn) {
    }
    stream.remove();
    try {
      stream.get();
      fail();
    } catch (StreamException exn) {
    }
  }

  @Test
  public void testAddStreamTwice()
      throws RPCException, StreamException, NoSuchMethodException {
    Stream<String> stream0 = connection.addStream(TestService.class, "int32ToString", 42);
    assertEquals("42", stream0.get());
    pause();
    assertEquals("42", stream0.get());

    Stream<String> stream1 = connection.addStream(TestService.class, "int32ToString", 42);
    assertTrue(stream0.equals(stream1));
    assertEquals("42", stream0.get());
    assertEquals("42", stream1.get());
    pause();
    assertEquals("42", stream0.get());
    assertEquals("42", stream1.get());

    Stream<String> stream2 = connection.addStream(TestService.class, "int32ToString", 43);
    assertFalse(stream0.equals(stream2));
    assertEquals("42", stream0.get());
    assertEquals("42", stream1.get());
    assertEquals("43", stream2.get());
    pause();
    assertEquals("42", stream0.get());
    assertEquals("42", stream1.get());
    assertEquals("43", stream2.get());
  }

  @Test
  @SuppressWarnings("checkstyle:emptycatchblock")
  public void testInvalidOperationExceptionImmediately()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "throwInvalidOperationException");
    try {
      stream.get();
      fail();
    } catch (UnsupportedOperationException exn) {
    }
  }

  @Test
  @SuppressWarnings("checkstyle:emptycatchblock")
  public void testInvalidOperationExceptionLater()
      throws RPCException, StreamException {
    testService.resetInvalidOperationExceptionLater();
    Stream<Integer> stream = connection.addStream(
        TestService.class, "throwInvalidOperationExceptionLater");
    assertEquals((Integer)0, stream.get());
    try {
      while (true) {
        pause();
        stream.get();
      }
    } catch (UnsupportedOperationException exn) {
    }
  }

  @Test
  public void testCustomExceptionImmediately()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(TestService.class, "throwCustomException");
    try {
      stream.get();
      fail();
    } catch (CustomException exn) {
      assertTrue(exn.getMessage().startsWith("A custom kRPC exception"));
    }
  }

  @Test
  public void testCustomExceptionLater()
      throws RPCException, StreamException {
    testService.resetCustomExceptionLater();
    Stream<Integer> stream = connection.addStream(TestService.class, "throwCustomExceptionLater");
    assertEquals(0, (int)stream.get());
    try {
      while (true) {
        pause();
        stream.get();
      }
    } catch (CustomException exn) {
      assertTrue(exn.getMessage().startsWith("A custom kRPC exception"));
    }
  }

  @Test
  public void testYieldException()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(TestService.class, "blockingProcedure", 10, 0);
    for (int i = 0; i < 100; i++) {
      assertEquals(55, (int)stream.get());
      pause();
    }
  }

  @Test
  public void testWait()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testWait", 10);
    synchronized (stream.getCondition()) {
      int count = stream.get();
      assertTrue(count < 10);
      while (count < 10) {
        stream.waitForUpdate();
        count++;
        assertEquals(count, (int)stream.get());
      }
    }
  }

  @Test
  public void testWaitTimeoutShort()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testWaitTimeoutShort", 10);
    synchronized (stream.getCondition()) {
      int count = stream.get();
      stream.waitForUpdateWithTimeout(0);
      assertEquals(count, (int)stream.get());
    }
  }

  @Test
  public void testWaitTimeoutLong()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testWaitTimeoutLong", 10);
    synchronized (stream.getCondition()) {
      int count = stream.get();
      assertTrue(count < 10);
      while (count < 10) {
        stream.waitForUpdateWithTimeout(10);
        count++;
        assertEquals(count, (int)stream.get());
      }
    }
  }

  private volatile boolean testCallbackError = false;
  private volatile boolean testCallbackStop = false;
  private volatile int testCallbackValue = -1;

  @Test
  public void testCallback()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testCallback", 10);
    stream.addCallback(
        (Integer value) -> {
          if (value > 5) {
            testCallbackStop = true;
          } else if (testCallbackValue + 1 != value) {
            testCallbackError = true;
            testCallbackStop = true;
          } else {
            testCallbackValue++;
          }
      });
    stream.start();
    while (!testCallbackStop) {
    }
    stream.remove();
    assertFalse(testCallbackError);
  }

  @Test
  public void testEquality()
      throws RPCException, StreamException {
    Stream<Integer> stream0 = connection.addStream(
        TestService.class, "counter", "StreamTest.testEquality0", 1);
    Stream<Integer> stream1 = connection.addStream(
        TestService.class, "counter", "StreamTest.testEquality0", 1);
    Stream<Integer> stream2 = connection.addStream(
        TestService.class, "counter", "StreamTest.testEquality1", 1);

    assertTrue(stream0.equals(stream0));
    assertTrue(stream0.equals(stream1));
    assertFalse(stream0.equals(stream2));
    assertFalse(stream0.equals(null));
    assertTrue(stream0.hashCode() == stream1.hashCode());
    assertFalse(stream0.hashCode() == stream2.hashCode());
  }
}
