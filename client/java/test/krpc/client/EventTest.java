package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertSame;
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
import krpc.client.services.KRPC.Expression;
import krpc.client.services.TestService;
import krpc.client.services.TestService.CustomException;
import krpc.client.services.TestService.TestClass;
import krpc.schema.KRPC.ProcedureResult;
import org.junit.Before;
import org.junit.Test;

/** Tests for Event. */
public class EventTest {

  private Connection connection;
  private KRPC krpc;
  private TestService testService;

  /** Set up test. */
  @Before
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void setup() throws IOException {
    connection = TestUtils.connect();
    krpc = KRPC.newInstance(connection);
    testService = TestService.newInstance(connection);
  }

  @Test
  public void testEvent()
      throws RPCException, StreamException {
    Event event = testService.onTimer(200, 1);
    synchronized (event.getCondition()) {
      long startTime = System.currentTimeMillis();
      event.waitFor();
      long time = System.currentTimeMillis() - startTime;
      // Lower bound is a correctness check: the event must not fire before its timer.
      // The upper bound is a generous hang detector, kept loose so the test does not
      // flake under parallel load.
      assertTrue(150 < time && time < 2000);
    }
  }

  @Test
  public void testEventTimeoutShort()
      throws RPCException, StreamException {
    Event event = testService.onTimer(200, 1);
    synchronized (event.getCondition()) {
      long startTime = System.currentTimeMillis();
      assertFalse(event.waitForWithTimeout(0.1));
      long time = System.currentTimeMillis() - startTime;
      // waitFor must return on its timeout, before the 200ms event fires.
      assertTrue(50 < time && time < 200);
      assertTrue(event.waitForWithTimeout(10));
    }
  }

  @Test
  public void testEventTimeoutLong()
      throws RPCException, StreamException {
    Event event = testService.onTimer(200, 1);
    synchronized (event.getCondition()) {
      long startTime = System.currentTimeMillis();
      assertTrue(event.waitForWithTimeout(1));
      long time = System.currentTimeMillis() - startTime;
      assertTrue(150 < time && time < 2000);
    }
  }

  private volatile boolean testEventCallbackCalled = false;

  @Test
  public void testEventCallback()
      throws RPCException, StreamException {
    Event event = testService.onTimer(200, 1);
    event.addCallback(
        () -> {
          testEventCallbackCalled = true;
        });
    long startTime = System.currentTimeMillis();
    while (!testEventCallbackCalled) {
    }
    long time = System.currentTimeMillis() - startTime;
    assertTrue(150 < time && time < 2000);
  }

  private volatile boolean testEventCallbackTimeoutCalled = false;

  @Test
  public void testEventCallbackTimeout()
      throws RPCException, StreamException {
    Event event = testService.onTimer(1000, 1);
    event.addCallback(
        () -> {
          testEventCallbackTimeoutCalled = true;
        });
    long startTime = System.currentTimeMillis();
    while (!testEventCallbackTimeoutCalled) {
      if (System.currentTimeMillis() - startTime > 100) {
        break;
      }
    }
    long time = System.currentTimeMillis() - startTime;
    // Loop breaks on its own 100ms timeout, before the 1000ms event fires.
    assertTrue(50 < time && time < 1000);
  }

  private volatile int testEventCallbackLoopCount = 0;

  @Test
  public void testEventCallbackLoop()
      throws RPCException, StreamException {
    Event event = testService.onTimer(200, 5);
    event.addCallback(
        () -> {
          testEventCallbackLoopCount++;
        });
    long startTime = System.currentTimeMillis();
    while (testEventCallbackLoopCount < 5) {
    }
    long time = System.currentTimeMillis() - startTime;
    assertTrue(950 < time && time < 3000);
    assertEquals(testEventCallbackLoopCount, 5);
  }

  private volatile boolean testEventRemoveCallbackCalled1 = false;
  private volatile boolean testEventRemoveCallbackCalled2 = false;

  @Test
  public void testEventRemoveCallback()
      throws RPCException, StreamException {
    Event event = testService.onTimer(200, 1);
    event.addCallback(
        () -> {
          testEventRemoveCallbackCalled1 = true;
        });
    int called2Tag = event.addCallback(
        () -> {
          testEventRemoveCallbackCalled2 = true;
        });
    event.removeCallback(called2Tag);
    long startTime = System.currentTimeMillis();
    while (!testEventRemoveCallbackCalled1) {
    }
    long time = System.currentTimeMillis() - startTime;
    assertTrue(150 < time && time < 2000);
    assertFalse(testEventRemoveCallbackCalled2);
  }

  @Test
  public void testCustomEvent()
      throws RPCException, StreamException {
    Expression counter = Expression.call(connection,
                            connection.getCall(
                              TestService.class, "counter", "TestEvent.TestCustomEvent", 1));
    Expression expr = Expression.equal(connection,
        Expression.multiply(connection,
            Expression.constantInt(connection, 2),
            Expression.constantInt(connection, 10)),
        counter);
    Event event = krpc.addEvent(expr);
    synchronized (event.getCondition()) {
      event.waitFor();
      // The event fires when the server-side counter reaches 20. The counter increments
      // on every expression evaluation, so the value read back is at least 21: 20 at the
      // trigger, plus this read. The exact figure depends on how many more times the
      // server evaluated the expression, so assert the lower bound to avoid flaking under
      // load. See issue #540.
      assertTrue(testService.counter("TestEvent.TestCustomEvent", 1) >= 21);
    }
  }

  @Test
  public void testEquality()
      throws RPCException, StreamException {
    Event event0 = testService.onTimer(100, 1);
    Event event1 = event0;
    Event event2 = testService.onTimer(100, 1);

    assertTrue(event0.equals(event0));
    assertTrue(event0.equals(event1));
    assertFalse(event0.equals(event2));
    assertFalse(event0.equals(null));
    assertTrue(event0.hashCode() == event1.hashCode());
    assertFalse(event0.hashCode() == event2.hashCode());
  }

  @Test(timeout = 30000)
  public void testFiringBetweenWaits()
      throws RPCException, StreamException, InterruptedException {
    Event event = testService.onTimer(200, 2);
    synchronized (event.getCondition()) {
      event.waitFor();
    }
    // The second firing lands while no wait is in progress
    Thread.sleep(500);
    synchronized (event.getCondition()) {
      long startTime = System.currentTimeMillis();
      assertTrue(event.waitForWithTimeout(1));
      assertTrue(System.currentTimeMillis() - startTime < 100);
    }
  }

  @Test(timeout = 30000)
  public void testServerDisconnectWakesEventWait()
      throws IOException, RPCException, StreamException, InterruptedException {
    Connection conn = TestUtils.connect();
    try {
      Event event = TestService.newInstance(conn).onTimer(60000, 1);
      List<Throwable> errors = Collections.synchronizedList(new ArrayList<Throwable>());
      Thread waiter = new Thread(() -> {
        synchronized (event.getCondition()) {
          try {
            event.waitFor();
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
    } finally {
      conn.close();
    }
  }

  @Test(timeout = 30000)
  public void testServerRemovedEvent()
      throws RPCException, StreamException, InterruptedException {
    Event event = testService.onTimer(100, 1);
    List<Exception> ended = Collections.synchronizedList(new ArrayList<Exception>());
    CountDownLatch endCalled = new CountDownLatch(1);
    event.addEndCallback((Exception exn) -> {
      ended.add(exn);
      endCalled.countDown();
    });
    synchronized (event.getCondition()) {
      assertTrue(event.waitForWithTimeout(10));
      assertThrows(KRPC.StreamRemovedException.class, () -> event.waitFor());
      assertThrows(KRPC.StreamRemovedException.class, () -> event.waitForWithTimeout(0.1));
    }
    // The end callbacks run on the update thread after it wakes the waiters
    assertTrue(endCalled.await(10, TimeUnit.SECONDS));
    assertEquals(1, ended.size());
    assertTrue(ended.get(0) instanceof KRPC.StreamRemovedException);
  }

  @Test(timeout = 30000)
  public void testFiringBeforeWait()
      throws RPCException, StreamException, InterruptedException {
    Event event = testService.onTimer(100, 1);
    Thread.sleep(500);
    synchronized (event.getCondition()) {
      long startTime = System.currentTimeMillis();
      assertTrue(event.waitForWithTimeout(1));
      assertTrue(System.currentTimeMillis() - startTime < 100);
    }
  }

  @Test(timeout = 30000)
  public void testFiringsCoalesce()
      throws RPCException, StreamException, InterruptedException {
    Event event = testService.onTimer(50, 3);
    Thread.sleep(500);
    synchronized (event.getCondition()) {
      event.waitFor();
      assertThrows(KRPC.StreamRemovedException.class, () -> event.waitFor());
    }
  }

  @Test(timeout = 30000)
  public void testTwoThreadsWaitOnOneEvent()
      throws RPCException, StreamException, InterruptedException {
    Event event = testService.onTimer(1000, 1);
    List<Boolean> woke = Collections.synchronizedList(new ArrayList<Boolean>());
    List<Thread> threads = new ArrayList<Thread>();
    for (int i = 0; i < 2; i++) {
      threads.add(new Thread(() -> {
        synchronized (event.getCondition()) {
          try {
            woke.add(event.waitForWithTimeout(5));
          } catch (RPCException | StreamException exn) {
            woke.add(false);
          }
        }
      }));
    }
    for (Thread thread : threads) {
      thread.start();
    }
    for (Thread thread : threads) {
      thread.join();
    }
    assertEquals(Arrays.asList(true, true), woke);
  }

  @Test(timeout = 30000)
  public void testTimedWaitWokenWithNothingNewWaitsToTheDeadline()
      throws RPCException, StreamException, InterruptedException {
    Event event = testService.onTimer(60000, 1);
    Thread notifier = new Thread(() -> {
      try {
        Thread.sleep(100);
      } catch (InterruptedException exn) {
        throw new RuntimeException(exn);
      }
      synchronized (event.getCondition()) {
        event.getCondition().notifyAll();
      }
    });
    synchronized (event.getCondition()) {
      long startTime = System.currentTimeMillis();
      notifier.start();
      assertFalse(event.waitForWithTimeout(0.5));
      assertTrue(System.currentTimeMillis() - startTime >= 450);
    }
    notifier.join();
  }

  @Test(timeout = 30000)
  public void testFiringPassedToValueCallbackIsReplayed() {
    Event event = unissuedEvent(4000003);
    List<Object> values = Collections.synchronizedList(new ArrayList<Object>());
    event.getStream().addCallback(values::add);
    fire(4000003);
    assertEquals(Arrays.asList(true), values);
    List<Integer> calls = Collections.synchronizedList(new ArrayList<Integer>());
    event.addCallback(() -> calls.add(1));
    assertEquals(Arrays.asList(1), calls);
  }

  @Test(timeout = 30000)
  public void testRemovedCallbackDoesNotTakeFiring() {
    Event event = unissuedEvent(4000004);
    StreamImpl stream = event.getStream().stream;
    List<Integer> calls = Collections.synchronizedList(new ArrayList<Integer>());
    int tag = event.addCallback(() -> calls.add(1));
    StreamImpl.Callback callback = stream.getCallbacks().get(tag);
    event.removeCallback(tag);
    fire(4000004);
    // Stands in for an update thread that copied the callback before its removal
    stream.call(callback, 1, true);
    event.addCallback(() -> calls.add(2));
    assertEquals(Arrays.asList(2), calls);
  }

  /** An event over an id the server has not issued, so no update arrives for it. */
  private Event unissuedEvent(long id) {
    return new Event(connection, TestUtils.eventMessage(id));
  }

  private void fire(long id) {
    connection.streamManager.update(id, ProcedureResult.newBuilder()
        .setValue(ByteString.copyFrom(new byte[] {1}))
        .build());
  }

  @Test(timeout = 30000)
  public void testCallbackAddedAfterWaitIsNotCalled()
      throws RPCException, StreamException {
    Event event = unissuedEvent(4000000);
    fire(4000000);
    synchronized (event.getCondition()) {
      assertTrue(event.waitForWithTimeout(1));
    }
    List<Integer> calls = Collections.synchronizedList(new ArrayList<Integer>());
    event.addCallback(() -> calls.add(1));
    assertEquals(0, calls.size());
  }

  @Test(timeout = 30000)
  public void testCallbackAddedAfterAnUnseenFiringIsCalledOnce() {
    Event event = unissuedEvent(4000001);
    fire(4000001);
    List<Integer> calls = Collections.synchronizedList(new ArrayList<Integer>());
    event.addCallback(() -> calls.add(1));
    assertEquals(Arrays.asList(1), calls);
    // The firing has been passed to a callback
    event.addCallback(() -> calls.add(2));
    assertEquals(Arrays.asList(1), calls);
  }

  @Test(timeout = 30000)
  public void testCallbackAddedAfterFiringPassedToAnotherIsNotCalled() {
    Event event = unissuedEvent(4000002);
    List<Integer> calls = Collections.synchronizedList(new ArrayList<Integer>());
    event.addCallback(() -> calls.add(1));
    fire(4000002);
    assertEquals(Arrays.asList(1), calls);
    event.addCallback(() -> calls.add(2));
    assertEquals(Arrays.asList(1), calls);
  }

  @Test(timeout = 30000)
  public void testCallbackRemovedBeforeItsReplayTakesNoFiring() {
    Event event = unissuedEvent(4000005);
    StreamImpl stream = event.getStream().stream;
    List<Integer> calls = Collections.synchronizedList(new ArrayList<Integer>());
    int tag = event.addCallback(() -> calls.add(1));
    final StreamImpl.Callback callback = stream.getCallbacks().get(tag);
    event.removeCallback(tag);
    fire(4000005);
    // Stands in for a late callback that reserves the firing and is removed before its call
    long firing = stream.reserveReplay(0);
    assertEquals(1, firing);
    stream.replay(callback, firing);
    event.addCallback(() -> calls.add(2));
    assertEquals(Arrays.asList(2), calls);
  }

  @Test(timeout = 30000)
  public void testTwoLateCallbacksDoNotBothReplayFiring() {
    Event event = unissuedEvent(4000006);
    StreamImpl stream = event.getStream().stream;
    fire(4000006);
    List<Integer> calls = Collections.synchronizedList(new ArrayList<Integer>());
    // Stands in for a late callback that is added and reserves the firing
    StreamImpl.Callback first = new StreamImpl.Callback((Object value) -> calls.add(1), true, -1);
    synchronized (connection.streamManager.updateLock) {
      stream.getCallbacks().put(first.tag, first);
    }
    long firing = stream.reserveReplay(0);
    // Added between the reservation of the firing for the first callback and its replay
    event.addCallback(() -> calls.add(2));
    stream.replay(first, firing);
    assertEquals(Arrays.asList(1), calls);
  }

  @Test(timeout = 30000)
  public void testErrorEndsTheEvent()
      throws RPCException, StreamException {
    Expression expr = Expression.equal(connection,
        Expression.call(connection,
            connection.getCall(TestService.class, "throwInvalidOperationException")),
        Expression.constantInt(connection, 0));
    Event event = krpc.addEvent(expr);
    synchronized (event.getCondition()) {
      Exception first =
          assertThrows(UnsupportedOperationException.class, () -> event.waitFor());
      Exception second = assertThrows(Exception.class, () -> event.waitFor());
      assertSame(first, second);
    }
  }
}
