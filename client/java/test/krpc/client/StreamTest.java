package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

import krpc.client.services.TestService;
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
    Stream<Integer> stream = connection.addStream(TestService.class, "counter");
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
    // var streamId = stream0.Id;
    assertEquals("42", stream0.get());

    pause();
    assertEquals("42", stream0.get());

    Stream<String> stream1 = connection.addStream(TestService.class, "int32ToString", 42);
    // assertEquals(streamId, stream1.Id);
    assertEquals("42", stream0.get());
    assertEquals("42", stream1.get());

    pause();
    assertEquals("42", stream0.get());
    assertEquals("42", stream1.get());
  }
}
