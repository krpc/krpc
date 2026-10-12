package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertThrows;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

import com.google.protobuf.ByteString;
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collections;
import java.util.List;
import krpc.client.services.TestService;
import krpc.client.services.TestService.CustomException;
import krpc.client.services.TestService.TestClass;
import krpc.schema.KRPC.ProcedureResult;
import krpc.schema.KRPC.Type;
import org.junit.Before;
import org.junit.Test;

/** Tests for Stream. */
public class StreamTest {

  private Connection connection;
  private TestService testService;

  @Before
  @SuppressWarnings("checkstyle:missingjavadocmethod")
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
  public void testNullable()
      throws RPCException, StreamException, NoSuchMethodException {
    Stream<Integer> stream =
        connection.addStream(TestService.class, "echoNullableInt", (Object) null);
    for (int i = 0; i < 5; i++) {
      assertNull(stream.get());
      pause();
    }
  }

  @Test
  public void testNullableListElements()
      throws RPCException, StreamException, NoSuchMethodException {
    // A stream carries the value itself, so a null inside a collection reaches the decoder
    // as a presence bool where a null result does not
    List<Integer> value = Arrays.asList(1, null, 3);
    Stream<List<Integer>> stream =
        connection.addStream(TestService.class, "echoListOfNullableInts", value);
    for (int i = 0; i < 5; i++) {
      assertEquals(value, stream.get());
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
    // Java has no default parameter values, so the optional parameter is passed explicitly
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

  @Test(timeout = 30000)
  public void testRemovingStreamAgainThroughAnotherHandle()
      throws RPCException, StreamException {
    Stream<String> first = connection.addStream(TestService.class, "int32ToString", 46);
    Stream<String> second = connection.addStream(TestService.class, "int32ToString", 46);
    first.remove();
    // The server can give the new stream the removed one's id
    Stream<String> third = connection.addStream(TestService.class, "int32ToString", 46);
    second.remove();
    assertEquals("46", third.get());
    third.remove();
  }

  @Test(timeout = 30000)
  public void testCallbacksSkipAnError()
      throws RPCException, StreamException, InterruptedException {
    List<Throwable> uncaught = Collections.synchronizedList(new ArrayList<Throwable>());
    Thread.UncaughtExceptionHandler handler = Thread.getDefaultUncaughtExceptionHandler();
    Thread.setDefaultUncaughtExceptionHandler((thread, exn) -> uncaught.add(exn));
    try {
      Stream<Integer> stream = connection.addStream(
          TestService.class, "throwInvalidOperationException");
      List<Integer> values = Collections.synchronizedList(new ArrayList<Integer>());
      stream.addCallback((Integer value) -> values.add(value));
      synchronized (stream.getCondition()) {
        assertThrows(UnsupportedOperationException.class, () -> stream.waitForUpdate());
      }
      assertThrows(UnsupportedOperationException.class, () -> stream.get());
      // The callbacks run on the update thread after it notifies the condition
      Thread.sleep(200);
      assertEquals(Collections.emptyList(), values);
      assertEquals(Collections.emptyList(), uncaught);
      stream.remove();
    } finally {
      Thread.setDefaultUncaughtExceptionHandler(handler);
    }
  }

  @Test(timeout = 30000)
  public void testRemovalWakesBlockedWait()
      throws RPCException, StreamException, InterruptedException {
    Event event = testService.onTimer(60000, 1);
    Stream<String> stream = connection.addStream(TestService.class, "int32ToString", 47);
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
    assertEquals((Integer) 0, stream.get());
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
    assertEquals(0, (int) stream.get());
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
      assertEquals(55, (int) stream.get());
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
        assertEquals(count, (int) stream.get());
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
      assertFalse(stream.waitForUpdateWithTimeout(0));
      assertEquals(count, (int) stream.get());
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
        assertTrue(stream.waitForUpdateWithTimeout(10));
        count++;
        assertEquals(count, (int) stream.get());
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
    assertEquals(testCallbackValue, 5);
  }

  // The update thread must not hold the update lock while waiting for a stream's condition. A
  // caller holding that condition, which is how waiting for an update is documented to work,
  // otherwise blocks forever on anything needing the update lock, and the update thread blocks
  // on the condition, deadlocking both. The timeout catches it, as it hangs the test outright.
  @Test(timeout = 30000)
  public void testRemoveWhileHoldingCondition()
      throws RPCException, StreamException, InterruptedException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testRemoveWhileHoldingCondition", 1);
    stream.start();
    synchronized (stream.getCondition()) {
      stream.waitForUpdate();
      // Let the update thread reach the next update and block on this condition
      Thread.sleep(200);
      stream.remove();
    }
  }

  private volatile int testCallbackThrowsCount = 0;
  private volatile boolean testCallbackThrowsStop = false;

  // A callback that throws must not take the update thread down with it, which would stop
  // every stream on the connection. The timeout catches that, as the loop below would
  // otherwise never see another update.
  @Test(timeout = 30000)
  public void testCallbackThrows()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testCallbackThrows", 10);
    stream.addCallback(
        (Integer value) -> {
          throw new IllegalStateException("callback failed");
      });
    stream.addCallback(
        (Integer value) -> {
          if (value > 5) {
            testCallbackThrowsStop = true;
          } else {
            testCallbackThrowsCount++;
          }
      });
    stream.start();
    while (!testCallbackThrowsStop) {
    }
    stream.remove();
    // The callback registered after the one that threw still ran
    assertTrue(testCallbackThrowsCount > 0);
  }

  private volatile boolean testRemoveCallbackCalled1 = false;
  private volatile boolean testRemoveCallbackCalled2 = false;

  @Test
  public void testRemoveCallback()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testRemoveCallback", 10);
    stream.addCallback(
        (Integer value) -> {
          testRemoveCallbackCalled1 = true;
        }
    );
    int callback2Tag = stream.addCallback(
        (Integer value) -> {
          testRemoveCallbackCalled2 = true;
        }
    );
    stream.removeCallback(callback2Tag);
    stream.start();
    while (!testRemoveCallbackCalled1) {
    }
    stream.remove();
    assertFalse(testRemoveCallbackCalled2);
  }

  private volatile boolean testRateError = false;
  private volatile boolean testRateStop = false;
  private volatile int testRateValue = 0;

  @Test
  public void testRate()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testRate", 1);
    stream.addCallback(
        (Integer value) -> {
          if (value > 5) {
            testRateStop = true;
          } else if (testRateValue + 1 != value) {
            testRateError = true;
            testRateStop = true;
          } else {
            testRateValue++;
          }
      });
    stream.setRate(5);
    stream.start();
    long startTime = System.nanoTime();
    while (!testRateStop) {
    }
    long elapsed = System.nanoTime() - startTime;
    assertTrue(elapsed > 1000000000);
    assertTrue(elapsed < 1500000000);
    stream.remove();
    assertFalse(testRateError);
    assertEquals(testRateValue, 5);
  }

  @Test
  public void testGetRate()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testGetRate", 1);
    assertEquals(0, stream.getRate(), 0.0001);
    stream.setRate(5);
    assertEquals(5, stream.getRate(), 0.0001);
    stream.setRate(0);
    assertEquals(0, stream.getRate(), 0.0001);
    stream.remove();
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

  /**
   * Waits for a list to reach a size, and checks it stays there. End callbacks run on the update
   * thread after it wakes the waiters.
   */
  private static void awaitSize(List<?> list, int size) throws InterruptedException {
    long deadline = System.currentTimeMillis() + 10000;
    while (list.size() < size && System.currentTimeMillis() < deadline) {
      Thread.sleep(10);
    }
    Thread.sleep(100);
    assertEquals(size, list.size());
  }

  @Test(timeout = 30000)
  public void testErrorEndsTheStream()
      throws RPCException, StreamException, InterruptedException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "throwInvalidOperationException");
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    stream.addEndCallback(ended::add);
    synchronized (stream.getCondition()) {
      assertThrows(UnsupportedOperationException.class, () -> stream.waitForUpdate());
      assertThrows(
          UnsupportedOperationException.class, () -> stream.waitForUpdateWithTimeout(1));
    }
    assertThrows(UnsupportedOperationException.class, () -> stream.get());
    awaitSize(ended, 1);
    assertTrue(ended.get(0) instanceof UnsupportedOperationException);
    // Added after the end, so called straight away
    stream.addEndCallback(ended::add);
    assertEquals(2, ended.size());
    // Already removed by the server, so this does nothing
    stream.remove();
    assertThrows(UnsupportedOperationException.class, () -> stream.get());
  }

  @Test(timeout = 30000)
  public void testEndCallbackOnRemove()
      throws RPCException, StreamException {
    Stream<String> stream = connection.addStream(TestService.class, "int32ToString", 48);
    assertEquals("48", stream.get());
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    stream.addEndCallback(ended::add);
    stream.remove();
    assertEquals(1, ended.size());
    assertTrue(ended.get(0) instanceof StreamException);
  }

  @Test(timeout = 30000)
  public void testEndCallbackAddedAfterTheEnd()
      throws RPCException, StreamException {
    Stream<String> stream = connection.addStream(TestService.class, "int32ToString", 55);
    stream.remove();
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    stream.addEndCallback(ended::add);
    assertEquals(1, ended.size());
    assertTrue(ended.get(0) instanceof StreamException);
  }

  @Test(timeout = 30000)
  public void testRemoveEndCallback()
      throws RPCException, StreamException {
    Stream<String> stream = connection.addStream(TestService.class, "int32ToString", 56);
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    int tag = stream.addEndCallback(ended::add);
    stream.removeEndCallback(tag);
    stream.remove();
    assertEquals(Collections.emptyList(), ended);
  }

  @Test(timeout = 30000)
  public void testValueThatFailsToDecodeEndsOnlyItsStream()
      throws RPCException, StreamException {
    Type type = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
    // Ids the server has not issued, so no update arrives for them
    StreamImpl failing = connection.streamManager.getStream(type, 1000000);
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    failing.addEndCallback(ended::add);
    final StreamImpl working = connection.streamManager.getStream(type, 1000001);
    // A varint that never ends
    ByteString bad = ByteString.copyFrom(new byte[] {-1});
    connection.streamManager.update(1000000, ProcedureResult.newBuilder().setValue(bad).build());
    ByteString good = ByteString.copyFrom(new byte[] {42});
    connection.streamManager.update(1000001, ProcedureResult.newBuilder().setValue(good).build());
    assertEquals(1, ended.size());
    assertThrows(EncodingException.class, () -> failing.getValue());
    assertEquals(42, (int) working.getValue());
  }

  @Test(timeout = 30000)
  public void testCloseWakesWaits()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    Stream<String> stream = conn.addStream(TestService.class, "int32ToString", 49);
    assertEquals("49", stream.get());
    Event event = TestService.newInstance(conn).onTimer(60000, 1);
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    stream.addEndCallback(ended::add);
    event.addEndCallback(ended::add);
    List<Throwable> errors = Collections.synchronizedList(new ArrayList<Throwable>());
    Thread streamWaiter = new Thread(() -> {
      synchronized (stream.getCondition()) {
        try {
          stream.waitForUpdate();
        } catch (RPCException | StreamException exn) {
          errors.add(exn);
        }
      }
    });
    Thread eventWaiter = new Thread(() -> {
      synchronized (event.getCondition()) {
        try {
          event.waitFor();
        } catch (RPCException | StreamException exn) {
          errors.add(exn);
        }
      }
    });
    streamWaiter.start();
    eventWaiter.start();
    while (streamWaiter.getState() != Thread.State.WAITING
        || eventWaiter.getState() != Thread.State.WAITING) {
      Thread.sleep(10);
    }
    conn.close();
    streamWaiter.join();
    eventWaiter.join();
    assertEquals(2, errors.size());
    for (Throwable exn : errors) {
      assertTrue(exn.getCause() instanceof ConnectionException);
    }
    awaitSize(ended, 2);
    StreamException exn = assertThrows(StreamException.class, () -> stream.get());
    assertTrue(exn.getCause() instanceof ConnectionException);
  }

  @Test(timeout = 30000)
  public void testConnectionFailureWakesWaitingThread()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    try {
      Stream<String> stream = conn.addStream(TestService.class, "int32ToString", 50);
      assertEquals("50", stream.get());
      List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
      stream.addEndCallback(ended::add);
      List<Throwable> errors = Collections.synchronizedList(new ArrayList<Throwable>());
      Thread waiter = new Thread(() -> {
        synchronized (stream.getCondition()) {
          try {
            stream.waitForUpdate();
          } catch (RPCException | StreamException exn) {
            errors.add(exn);
          }
        }
      });
      waiter.start();
      while (waiter.getState() != Thread.State.WAITING) {
        Thread.sleep(10);
      }
      // Stands in for the server closing the connection
      conn.streamManager.channel.close();
      waiter.join();
      assertEquals(1, errors.size());
      assertTrue(errors.get(0).getCause() instanceof ConnectionException);
      awaitSize(ended, 1);
      assertThrows(StreamException.class, () -> stream.get());
    } finally {
      conn.close();
    }
  }

  @Test(timeout = 30000)
  public void testCloseWhileHoldingCondition()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    // A new value on every update, which the update thread blocks on the condition to store
    Stream<Integer> stream = conn.addStream(
        TestService.class, "counter", "StreamTest.testCloseWhileHoldingCondition", 1);
    synchronized (stream.getCondition()) {
      stream.waitForUpdate();
      Thread.sleep(100);
      conn.close();
      StreamException exn = assertThrows(StreamException.class, () -> stream.waitForUpdate());
      assertTrue(exn.getCause() instanceof ConnectionException);
    }
  }
}
