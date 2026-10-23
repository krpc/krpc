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
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import krpc.client.services.KRPC;
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

  @Test(timeout = 30000)
  public void testAddStreamTwice()
      throws RPCException, StreamException {
    Stream<String> stream0 = connection.addStream(TestService.class, "int32ToString", 42);
    Stream<String> stream1 = connection.addStream(TestService.class, "int32ToString", 42);
    assertFalse(stream0.equals(stream1));
    assertEquals("42", stream0.get());
    assertEquals("42", stream1.get());
    stream0.remove();
    assertThrows(StreamException.class, () -> stream0.get());
    synchronized (stream1.getCondition()) {
      // Times out, as the value does not change, but the stream stays live
      stream1.waitForUpdateWithTimeout(0.1);
    }
    assertEquals("42", stream1.get());
    stream1.remove();
  }

  @Test(timeout = 30000)
  public void testRemoveThenAddStream()
      throws RPCException, StreamException {
    Stream<String> first = connection.addStream(TestService.class, "int32ToString", 46);
    assertEquals("46", first.get());
    first.remove();
    Stream<String> second = connection.addStream(TestService.class, "int32ToString", 46);
    assertFalse(first.equals(second));
    assertEquals("46", second.get());
    second.remove();
  }

  @Test(timeout = 30000)
  public void testRemovingStreamAgainThroughAnotherHandle()
      throws RPCException, StreamException {
    Stream<String> first = connection.addStream(TestService.class, "int32ToString", 53);
    Stream<String> second = new Stream<String>(
        connection, first.stream.getReturnType(), first.stream.getId());
    first.remove();
    Stream<String> third = connection.addStream(TestService.class, "int32ToString", 53);
    second.remove();
    assertEquals("53", third.get());
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
        assertThrows(IllegalStateException.class, () -> stream.waitForUpdate());
      }
      assertThrows(IllegalStateException.class, () -> stream.get());
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

  @Test(timeout = 30000)
  public void testReadStraightAfterAdd()
      throws RPCException, StreamException {
    Stream<String> stream = connection.addStream(TestService.class, "int32ToString", 51);
    assertEquals("51", stream.get());
    stream.remove();
  }

  @Test(timeout = 30000)
  public void testCallbackAddedAfterTheFirstValue()
      throws RPCException, StreamException {
    Stream<String> stream = connection.addStream(TestService.class, "int32ToString", 52);
    assertEquals("52", stream.get());
    List<String> values = Collections.synchronizedList(new ArrayList<String>());
    stream.addCallback(values::add);
    assertEquals(Arrays.asList("52"), values);
    stream.remove();
  }

  @Test(timeout = 30000)
  public void testResultBeforeTheAddReplyIsKept()
      throws RPCException, StreamException {
    StreamManager manager = connection.streamManager;
    // Ids the server has not issued, so no update arrives for them. One is noted but not
    // registered, and one is not yet noted.
    manager.noteIssued(2000000);
    manager.update(2000000, uint32Result(42));
    manager.update(2000001, uint32Result(43));
    Type type = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
    assertEquals(42, (int) manager.getStream(type, 2000000).getValue());
    assertEquals(43, (int) manager.getStream(type, 2000001).getValue());
  }

  @Test(timeout = 30000)
  public void testResultForAnEndedStreamIsDropped()
      throws RPCException, StreamException {
    Type type = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
    StreamManager manager = connection.streamManager;
    manager.noteIssued(2000005);
    manager.getStream(type, 2000005);
    manager.update(2000002, uint32Result(42));
    StreamImpl ended = manager.getStream(type, 2000002);
    assertFalse(ended.getUpdated());
  }

  private static ProcedureResult uint32Result(int value) {
    return ProcedureResult.newBuilder()
        .setValue(ByteString.copyFrom(new byte[] {(byte) value}))
        .build();
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
    } catch (IllegalStateException exn) {
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
    } catch (IllegalStateException exn) {
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
    while (!testCallbackThrowsStop) {
    }
    stream.remove();
    // The callback registered after the one that threw still ran
    assertTrue(testCallbackThrowsCount > 0);
  }

  private volatile int testRemoveCallbackValue = -1;

  @Test(timeout = 30000)
  public void testRemoveCallback()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testRemoveCallback", 1);
    stream.addCallback(
        (Integer value) -> {
          testRemoveCallbackValue = value;
        }
    );
    List<Integer> removedValues = Collections.synchronizedList(new ArrayList<Integer>());
    int removedTag = stream.addCallback(removedValues::add);
    stream.removeCallback(removedTag);
    // A value stored before the removal can still reach the removed callback
    int removedAt = stream.get();
    while (testRemoveCallbackValue <= removedAt + 1) {
    }
    stream.remove();
    synchronized (removedValues) {
      for (int value : removedValues) {
        assertTrue(value <= removedAt);
      }
    }
  }

  private volatile boolean testRateError = false;
  private volatile boolean testRateStop = false;

  @Test
  public void testRate()
      throws RPCException, StreamException {
    Stream<Integer> stream = connection.addStream(
        TestService.class, "counter", "StreamTest.testRate", 1);
    stream.setRate(5);
    List<Integer> values = Collections.synchronizedList(new ArrayList<Integer>());
    stream.addCallback(
        (Integer value) -> {
          // The stream can update before its rate is set, so counting starts from the first
          // value passed
          if (!values.isEmpty() && values.get(values.size() - 1) + 1 != value) {
            testRateError = true;
            testRateStop = true;
          }
          values.add(value);
          if (values.size() > 5) {
            testRateStop = true;
          }
      });
    long startTime = System.nanoTime();
    while (!testRateStop) {
    }
    long elapsed = System.nanoTime() - startTime;
    // At 5Hz, five increments take about 1s. The upper bound is a generous hang detector.
    assertTrue(elapsed > 1000000000);
    assertTrue(elapsed < 3000000000L);
    stream.remove();
    assertFalse(testRateError);
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

    // Each add creates a new stream, even for an equal call
    assertTrue(stream0.equals(stream0));
    assertFalse(stream0.equals(stream1));
    assertFalse(stream0.equals(stream2));
    assertFalse(stream0.equals(null));
    assertFalse(stream0.hashCode() == stream1.hashCode());
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
      assertThrows(IllegalStateException.class, () -> stream.waitForUpdate());
      assertThrows(
          IllegalStateException.class, () -> stream.waitForUpdateWithTimeout(1));
    }
    assertThrows(IllegalStateException.class, () -> stream.get());
    awaitSize(ended, 1);
    assertTrue(ended.get(0) instanceof IllegalStateException);
    // Added after the end, so called straight away
    stream.addEndCallback(ended::add);
    assertEquals(2, ended.size());
    // Already removed by the server, so this does nothing
    stream.remove();
    assertThrows(IllegalStateException.class, () -> stream.get());
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
    assertEquals("The stream has been removed", ended.get(0).getMessage());
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

  @Test(timeout = 30000)
  public void testKeptResultsAreTheLatestValueAndTheEnding()
      throws RPCException, StreamException {
    Type type = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
    StreamManager manager = connection.streamManager;
    // Ids the server has not issued, so no update arrives for them
    manager.update(3000000, uint32Result(1));
    manager.update(3000000, uint32Result(2));
    assertEquals(2, (int) manager.getStream(type, 3000000).getValue());
    manager.update(3000001, uint32Result(1));
    manager.update(3000001, errorResult());
    manager.update(3000001, uint32Result(3));
    StreamImpl ended = manager.getStream(type, 3000001);
    assertTrue(ended.getUpdated());
    assertThrows(RPCException.class, () -> ended.getValue());
  }

  @Test(timeout = 30000)
  public void testValueThatFailsToDecodeRemovesTheStreamOnTheServer()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    try {
      KRPC krpc = KRPC.newInstance(conn);
      final int before = krpc.getStatus().getStreamRpcs();
      Stream<Integer> stream = conn.addStream(
          TestService.class, "counter", "StreamTest.testValueThatFailsToDecode", 1);
      stream.get();
      // A varint that never ends
      ByteString bad = ByteString.copyFrom(new byte[] {-1});
      conn.streamManager.update(
          stream.stream.getId(), ProcedureResult.newBuilder().setValue(bad).build());
      assertThrows(EncodingException.class, () -> stream.get());
      long deadline = System.currentTimeMillis() + 10000;
      while (krpc.getStatus().getStreamRpcs() > before
          && System.currentTimeMillis() < deadline) {
        Thread.sleep(10);
      }
      assertTrue(krpc.getStatus().getStreamRpcs() <= before);
    } finally {
      conn.close();
    }
  }

  @Test(timeout = 30000)
  public void testCallbackRunsWithNoLockHeld()
      throws RPCException, StreamException, InterruptedException {
    Type type = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
    StreamManager manager = connection.streamManager;
    // An id the server has not issued, so no update arrives for it
    StreamImpl stream = manager.getStream(type, 3000002);
    manager.update(3000002, uint32Result(1));
    List<Object> values = Collections.synchronizedList(new ArrayList<Object>());
    Thread updater = new Thread(() -> manager.update(3000002, uint32Result(2)));
    stream.addCallback((Object value) -> {
      values.add(value);
      if (updater.getState() == Thread.State.NEW) {
        // A newer value is stored while the first call is in progress, and is passed to the
        // callback once that call returns
        updater.start();
        try {
          synchronized (stream.getCondition()) {
            while (stream.getVersion() < 2) {
              stream.getCondition().wait();
            }
          }
        } catch (InterruptedException exn) {
          throw new RuntimeException(exn);
        }
      }
    });
    updater.join();
    assertEquals(Arrays.asList(1, 2), values);
  }

  @Test(timeout = 30000)
  public void testCallbackNeverGetsAnOlderValue()
      throws RPCException, StreamException {
    Type type = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
    StreamManager manager = connection.streamManager;
    // An id the server has not issued, so no update arrives for it
    StreamImpl stream = manager.getStream(type, 3000003);
    manager.update(3000003, uint32Result(1));
    List<Object> values = Collections.synchronizedList(new ArrayList<Object>());
    int tag = stream.addCallback(values::add);
    manager.update(3000003, uint32Result(2));
    // The first value, delivered late, as when the update thread passes the second value to a
    // callback before its first call
    stream.call(stream.getCallbacks().get(tag), 1, 1);
    assertEquals(Arrays.asList(1, 2), values);
  }

  private static final Type UINT32 = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();

  private static boolean await(CountDownLatch latch) {
    try {
      return latch.await(10, TimeUnit.SECONDS);
    } catch (InterruptedException exn) {
      throw new RuntimeException(exn);
    }
  }

  @Test(timeout = 30000)
  public void testValuesDuringFirstCallAreQueued()
      throws InterruptedException {
    StreamManager manager = connection.streamManager;
    // An id the server has not issued, so no update arrives for it
    StreamImpl stream = manager.getStream(UINT32, 3000010);
    manager.update(3000010, uint32Result(1));
    List<Object> values = Collections.synchronizedList(new ArrayList<Object>());
    List<Thread> threads = Collections.synchronizedList(new ArrayList<Thread>());
    CountDownLatch entered = new CountDownLatch(1);
    CountDownLatch release = new CountDownLatch(1);
    Thread adder = new Thread(() -> stream.addCallback((Object value) -> {
      values.add(value);
      threads.add(Thread.currentThread());
      if (value.equals(1)) {
        entered.countDown();
        await(release);
      }
    }));
    adder.start();
    assertTrue(await(entered));
    Thread updater = new Thread(() -> manager.update(3000010, uint32Result(2)));
    updater.start();
    // The update thread queues the value, with no wait for the first call
    updater.join(5000);
    assertFalse(updater.isAlive());
    release.countDown();
    adder.join(10000);
    assertEquals(Arrays.asList(1, 2), values);
    assertEquals(Arrays.asList(adder, adder), threads);
  }

  @Test(timeout = 30000)
  public void testFirstCallOfCallbackWaitsForUpdates()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    try {
      Stream<Integer> stream = conn.addStream(
          TestService.class, "counter", "StreamTest.testFirstCallOfCallbackWaitsForUpdates", 1);
      stream.get();
      List<Object> waited = Collections.synchronizedList(new ArrayList<Object>());
      Thread[] adder = new Thread[1];
      adder[0] = new Thread(() -> stream.addCallback((Integer value) -> {
        // The values queued during the first call are passed on the same thread
        if (Thread.currentThread() == adder[0] && waited.isEmpty()) {
          synchronized (stream.getCondition()) {
            try {
              waited.add(stream.waitForUpdateWithTimeout(5));
              waited.add(stream.waitForUpdateWithTimeout(5));
            } catch (RPCException | StreamException exn) {
              waited.add(exn);
            }
          }
        }
      }));
      adder[0].start();
      adder[0].join(30000);
      assertEquals(Arrays.asList(true, true), waited);
    } finally {
      conn.close();
    }
  }

  @Test(timeout = 30000)
  public void testCloseWhileUpdateWaitsForFirstCall()
      throws IOException, InterruptedException {
    Connection conn = TestUtils.connect();
    StreamManager manager = conn.streamManager;
    // An id the server has not issued, so no update arrives for it
    StreamImpl stream = manager.getStream(UINT32, 3000011);
    manager.update(3000011, uint32Result(1));
    List<Object> values = Collections.synchronizedList(new ArrayList<Object>());
    CountDownLatch entered = new CountDownLatch(1);
    CountDownLatch release = new CountDownLatch(1);
    Thread adder = new Thread(() -> stream.addCallback((Object value) -> {
      values.add(value);
      entered.countDown();
      await(release);
    }));
    adder.start();
    assertTrue(await(entered));
    StreamImpl.Callback callback = stream.getCallbacks().values().iterator().next();
    // Stands in for the window before the adding thread marks the first call
    callback.adding = false;
    Thread updater = new Thread(() -> stream.call(callback, 2, 2));
    updater.start();
    updater.join(100);
    assertTrue(updater.isAlive());
    conn.close();
    // Joined before the callback stops waiting for the release
    updater.join(5000);
    assertFalse(updater.isAlive());
    release.countDown();
    adder.join(10000);
    assertEquals(Arrays.asList(1), values);
  }

  @Test(timeout = 30000)
  public void testCloseFromFirstCallOfCallback()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    Stream<String> stream = conn.addStream(TestService.class, "int32ToString", 53);
    assertEquals("53", stream.get());
    stream.addCallback((String value) -> {
      try {
        conn.close();
      } catch (IOException exn) {
        throw new RuntimeException(exn);
      }
    });
    assertThrows(StreamException.class, () -> stream.get());
  }

  @Test(timeout = 30000)
  public void testCallbackClosesTheConnection()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    Thread testThread = Thread.currentThread();
    Stream<Integer> stream = conn.addStream(
        TestService.class, "counter", "StreamTest.testCallbackClosesTheConnection", 1);
    List<Thread> closers = Collections.synchronizedList(new ArrayList<Thread>());
    CountDownLatch ended = new CountDownLatch(1);
    stream.addEndCallback((Exception exn) -> ended.countDown());
    stream.addCallback((Integer value) -> {
      // Closed on the update thread, as a first call runs on this thread
      if (Thread.currentThread() != testThread && closers.isEmpty()) {
        closers.add(Thread.currentThread());
        try {
          conn.close();
        } catch (IOException exn) {
          throw new RuntimeException(exn);
        }
      }
    });
    assertTrue(await(ended));
    assertEquals(1, closers.size());
    assertThrows(StreamException.class, () -> stream.get());
  }

  @Test(timeout = 30000)
  public void testRemovedCallbackIsNotCalled() {
    // An id the server has not issued, so no update arrives for it
    StreamImpl stream = connection.streamManager.getStream(UINT32, 3000012);
    List<Object> values = Collections.synchronizedList(new ArrayList<Object>());
    int tag = stream.addCallback(values::add);
    StreamImpl.Callback callback = stream.getCallbacks().get(tag);
    stream.removeCallback(tag);
    stream.call(callback, 1, "value");
    assertEquals(Collections.emptyList(), values);
  }

  @Test(timeout = 30000)
  public void testFirstCallPropagatesError() {
    StreamManager manager = connection.streamManager;
    // An id the server has not issued, so no update arrives for it
    StreamImpl stream = manager.getStream(UINT32, 3000013);
    manager.update(3000013, uint32Result(1));
    Error error = new Error("thrown by the callback");
    List<Object> values = Collections.synchronizedList(new ArrayList<Object>());
    Error thrown = assertThrows(Error.class, () -> stream.addCallback((Object value) -> {
      values.add(value);
      throw error;
    }));
    assertTrue(thrown == error);
    // The failed add leaves no callback
    assertTrue(stream.getCallbacks().isEmpty());
    manager.update(3000013, uint32Result(2));
    assertEquals(Arrays.asList(1), values);
  }

  @Test(timeout = 30000)
  public void testCloserHoldingConditionKeepsServerEnding()
      throws IOException {
    Connection conn = TestUtils.connect();
    StreamManager manager = conn.streamManager;
    // An id the server has not issued, so no update arrives for it
    StreamImpl stream = manager.getStream(UINT32, 3000014);
    synchronized (stream.getCondition()) {
      // Closed once the update thread has decoded the ending and before it applies it
      StreamManager.Decoded decoded;
      synchronized (manager.updateLock) {
        decoded = manager.decode(stream, errorResult());
      }
      conn.close();
      assertThrows(RPCException.class, () -> stream.getValue());
      manager.apply(decoded);
      assertThrows(RPCException.class, () -> stream.getValue());
    }
  }

  @Test(timeout = 30000)
  public void testRemoveKeepsServerEnding() throws RPCException {
    StreamManager manager = connection.streamManager;
    // An id the server has not issued, so no update arrives for it
    StreamImpl stream = manager.getStream(UINT32, 3000015);
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    stream.addEndCallback(ended::add);
    // Removed once the update thread has decoded the ending and before it applies it
    StreamManager.Decoded decoded;
    synchronized (manager.updateLock) {
      decoded = manager.decode(stream, errorResult());
    }
    stream.remove();
    assertThrows(RPCException.class, () -> stream.getValue());
    manager.apply(decoded);
    assertEquals(1, ended.size());
    assertTrue(ended.get(0) instanceof RPCException);
  }

  @Test(timeout = 30000)
  public void testCloseWithEndCallbackThatThrowsError() throws IOException {
    Connection conn = TestUtils.connect();
    try {
      StreamManager manager = conn.streamManager;
      // Ids the server has not issued, so no update arrives for them
      StreamImpl first = manager.getStream(UINT32, 3000016);
      StreamImpl second = manager.getStream(UINT32, 3000017);
      Error error = new Error("thrown by the end callback");
      List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
      for (StreamImpl stream : Arrays.asList(first, second)) {
        stream.addEndCallback((Exception exn) -> {
          throw error;
        });
        stream.addEndCallback(ended::add);
      }
      // Called directly, as a close can also end the streams on the update thread, which reports
      // the Error
      Error thrown = assertThrows(Error.class, () -> manager.notifyClosed());
      assertTrue(thrown == error);
      assertEquals(2, ended.size());
    } finally {
      conn.close();
    }
  }

  @Test(timeout = 30000)
  public void testRemoveWithEndCallbackThatThrowsError()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    try {
      KRPC krpc = KRPC.newInstance(conn);
      final int before = krpc.getStatus().getStreamRpcs();
      Stream<String> stream = conn.addStream(TestService.class, "int32ToString", 54);
      assertEquals("54", stream.get());
      Error error = new Error("thrown by the end callback");
      stream.addEndCallback((Exception exn) -> {
        throw error;
      });
      Error thrown = assertThrows(Error.class, () -> stream.remove());
      assertTrue(thrown == error);
      // The server removes the stream on its next update
      long deadline = System.currentTimeMillis() + 10000;
      while (krpc.getStatus().getStreamRpcs() > before
          && System.currentTimeMillis() < deadline) {
        Thread.sleep(10);
      }
      assertTrue(krpc.getStatus().getStreamRpcs() <= before);
    } finally {
      conn.close();
    }
  }

  @Test(timeout = 30000)
  public void testStreamIdIsNotedWithoutStubs()
      throws IOException, RPCException {
    Connection conn = TestUtils.connect();
    try {
      // A class loader with none of the generated stubs
      conn.stubsLoader = new ClassLoader(null) {};
      Type uint32 = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
      ProcedureResult result = conn.invoke(
          "TestService", "OnTimer", Encoder.encode(60000, uint32), Encoder.encode(1, uint32));
      long id = krpc.schema.KRPC.Event.parseFrom(result.getValue()).getStream().getId();
      assertTrue(conn.streamManager.unregistered.contains(id));
    } finally {
      conn.close();
    }
  }

  /**
   * Starts a thread that holds the condition of one stream while it waits on another, and a
   * thread that waits on the first. Returns once both are waiting.
   */
  private static List<Thread> waitWhileHoldingAnotherCondition(
      Stream<String> held, Stream<String> waited, List<Throwable> errors)
      throws InterruptedException {
    Thread heldWaiter = new Thread(() -> {
      synchronized (held.getCondition()) {
        try {
          held.waitForUpdate();
        } catch (RPCException | StreamException exn) {
          errors.add(exn);
        }
      }
    });
    heldWaiter.start();
    while (heldWaiter.getState() != Thread.State.WAITING) {
      Thread.sleep(10);
    }
    Thread holder = new Thread(() -> {
      synchronized (held.getCondition()) {
        synchronized (waited.getCondition()) {
          try {
            waited.waitForUpdate();
          } catch (RPCException | StreamException exn) {
            errors.add(exn);
          }
        }
      }
    });
    holder.start();
    while (holder.getState() != Thread.State.WAITING) {
      Thread.sleep(10);
    }
    return Arrays.asList(heldWaiter, holder);
  }

  @Test(timeout = 30000)
  public void testCloseWhileOneWaiterHoldsAnotherCondition()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    // Streams whose values never change, so the update thread never takes their conditions
    Stream<String> held = conn.addStream(TestService.class, "int32ToString", 54);
    Stream<String> waited = conn.addStream(TestService.class, "int32ToString", 55);
    assertEquals("54", held.get());
    assertEquals("55", waited.get());
    List<Throwable> errors = Collections.synchronizedList(new ArrayList<Throwable>());
    List<Thread> threads = waitWhileHoldingAnotherCondition(held, waited, errors);
    conn.close();
    for (Thread thread : threads) {
      thread.join();
    }
    assertEquals(2, errors.size());
    for (Throwable exn : errors) {
      assertTrue(exn.getCause() instanceof ConnectionException);
    }
  }

  @Test(timeout = 30000)
  public void testServerDisconnectWhileOneWaiterHoldsAnotherCondition()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    try {
      Stream<String> held = conn.addStream(TestService.class, "int32ToString", 56);
      Stream<String> waited = conn.addStream(TestService.class, "int32ToString", 57);
      assertEquals("56", held.get());
      assertEquals("57", waited.get());
      List<Throwable> errors = Collections.synchronizedList(new ArrayList<Throwable>());
      List<Thread> threads = waitWhileHoldingAnotherCondition(held, waited, errors);
      // Stands in for the server closing the connection
      conn.streamManager.channel.close();
      for (Thread thread : threads) {
        thread.join();
      }
      assertEquals(2, errors.size());
      for (Throwable exn : errors) {
        assertTrue(exn.getCause() instanceof ConnectionException);
      }
    } finally {
      conn.close();
    }
  }

  @Test(timeout = 30000)
  public void testTimedWaitWokenWithNothingNewWaitsToTheDeadline()
      throws RPCException, StreamException, InterruptedException {
    Type type = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
    // An id the server has not issued, so no update arrives for it
    Stream<Integer> stream = new Stream<Integer>(connection, type, 3000004);
    Thread notifier = new Thread(() -> {
      try {
        Thread.sleep(100);
      } catch (InterruptedException exn) {
        throw new RuntimeException(exn);
      }
      synchronized (stream.getCondition()) {
        stream.getCondition().notifyAll();
      }
    });
    synchronized (stream.getCondition()) {
      long startTime = System.currentTimeMillis();
      notifier.start();
      assertFalse(stream.waitForUpdateWithTimeout(0.5));
      assertTrue(System.currentTimeMillis() - startTime >= 450);
    }
    notifier.join();
  }

  @Test(timeout = 30000)
  public void testEndCallbackClosesTheConnection()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    Stream<String> stream = conn.addStream(TestService.class, "int32ToString", 58);
    assertEquals("58", stream.get());
    Event event = TestService.newInstance(conn).onTimer(100, 1);
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    stream.addEndCallback(ended::add);
    // The server removes the event after it fires, which the update thread ends it on
    event.addEndCallback((Exception exn) -> {
      try {
        conn.close();
      } catch (IOException closeExn) {
        throw new RuntimeException(closeExn);
      }
    });
    awaitSize(ended, 1);
    assertTrue(ended.get(0).getCause() instanceof ConnectionException);
  }

  @Test(timeout = 30000)
  public void testCallbackIsNotCalledAfterTheStreamEnds()
      throws RPCException, StreamException {
    Stream<String> stream = connection.addStream(TestService.class, "int32ToString", 59);
    assertEquals("59", stream.get());
    List<Object> values = Collections.synchronizedList(new ArrayList<Object>());
    int tag = stream.stream.addCallback(values::add);
    stream.remove();
    // A value decoded before the removal, delivered after it
    stream.stream.call(stream.stream.getCallbacks().get(tag), 100, "60");
    assertEquals(Arrays.asList("59"), values);
  }

  @Test(timeout = 30000)
  public void testStreamsAddedOnSeveralThreads()
      throws RPCException, StreamException, InterruptedException {
    int count = 8;
    CountDownLatch start = new CountDownLatch(1);
    List<String> values = Collections.synchronizedList(new ArrayList<String>());
    List<Thread> threads = new ArrayList<Thread>();
    for (int i = 0; i < count; i++) {
      final int value = 100 + i;
      threads.add(new Thread(() -> {
        try {
          start.await();
          Stream<String> stream = connection.addStream(TestService.class, "int32ToString", value);
          values.add(stream.get());
          stream.remove();
        } catch (InterruptedException | RPCException | StreamException exn) {
          values.add(exn.toString());
        }
      }));
    }
    for (Thread thread : threads) {
      thread.start();
    }
    start.countDown();
    for (Thread thread : threads) {
      thread.join();
    }
    List<String> expected = new ArrayList<String>();
    for (int i = 0; i < count; i++) {
      expected.add(Integer.toString(100 + i));
    }
    Collections.sort(values);
    assertEquals(expected, values);
  }

  @Test(timeout = 30000)
  public void testEventIdIsNotedWithStubs()
      throws IOException, RPCException {
    Type uint32 = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
    ProcedureResult result = connection.invoke(
        "TestService", "OnTimer", Encoder.encode(60000, uint32), Encoder.encode(1, uint32));
    long id = krpc.schema.KRPC.Event.parseFrom(result.getValue()).getStream().getId();
    assertTrue(connection.streamManager.unregistered.contains(id));
  }

  @Test(timeout = 30000)
  public void testRemovedIdIsForgotten()
      throws RPCException {
    StreamManager manager = connection.streamManager;
    KRPC krpc = KRPC.newInstance(connection);
    long id = krpc.addStream(connection.getCall(TestService.class, "int32ToString", 54)).getId();
    manager.update(id, uint32Result(42));
    assertTrue(manager.unregistered.contains(id));
    assertTrue(manager.kept.containsKey(id));
    Type uint64 = Type.newBuilder().setCode(Type.TypeCode.UINT64).build();
    connection.invoke("KRPC", "RemoveStream", Encoder.encode(id, uint64));
    assertFalse(manager.unregistered.contains(id));
    assertFalse(manager.kept.containsKey(id));
    // A result still in flight is dropped
    manager.update(id, uint32Result(43));
    assertFalse(manager.kept.containsKey(id));
  }

  @Test(timeout = 30000)
  public void testStreamIdIsNotedWhenStubsDoNotLink()
      throws IOException, RPCException {
    Connection conn = TestUtils.connect();
    try {
      // A class loader whose classes fail to link
      conn.stubsLoader = new ClassLoader(null) {
        @Override
        protected Class<?> findClass(String name) {
          throw new NoClassDefFoundError(name);
        }
      };
      Type uint32 = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
      ProcedureResult result = conn.invoke(
          "TestService", "OnTimer", Encoder.encode(60000, uint32), Encoder.encode(1, uint32));
      long id = krpc.schema.KRPC.Event.parseFrom(result.getValue()).getStream().getId();
      assertTrue(conn.streamManager.unregistered.contains(id));
    } finally {
      conn.close();
    }
  }

  private static ProcedureResult errorResult() {
    return ProcedureResult.newBuilder()
        .setError(krpc.schema.KRPC.Error.newBuilder().setDescription("failed"))
        .build();
  }
}
