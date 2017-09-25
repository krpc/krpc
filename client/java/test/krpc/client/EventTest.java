package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

import krpc.client.services.KRPC;
import krpc.client.services.KRPC.Expression;
import krpc.client.services.TestService;
import krpc.client.services.TestService.CustomException;
import krpc.client.services.TestService.TestClass;

import org.junit.Before;
import org.junit.Test;

import java.io.IOException;

public class EventTest {

  private Connection connection;
  private KRPC krpc;
  private TestService testService;

  /** Set up test. */
  @Before
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
      assertTrue(150 < time && time < 250);
      assertTrue(event.getStream().get());
    }
  }

  @Test
  public void testEventTimeoutShort()
      throws RPCException, StreamException {
    Event event = testService.onTimer(200, 1);
    synchronized (event.getCondition()) {
      long startTime = System.currentTimeMillis();
      event.waitForWithTimeout(0.1);
      long time = System.currentTimeMillis() - startTime;
      assertTrue(50 < time && time < 150);
      event.waitFor();
      assertTrue(event.getStream().get());
    }
  }

  @Test
  public void testEventTimeoutLong()
      throws RPCException, StreamException {
    Event event = testService.onTimer(200, 1);
    synchronized (event.getCondition()) {
      long startTime = System.currentTimeMillis();
      event.waitForWithTimeout(1);
      long time = System.currentTimeMillis() - startTime;
      assertTrue(150 < time && time < 250);
      assertTrue(event.getStream().get());
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
    event.start();
    while (!testEventCallbackCalled) {
    }
    long time = System.currentTimeMillis() - startTime;
    assertTrue(150 < time && time < 250);
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
    event.start();
    while (!testEventCallbackTimeoutCalled) {
      if (System.currentTimeMillis() - startTime > 100) {
        break;
      }
    }
    long time = System.currentTimeMillis() - startTime;
    assertTrue(50 < time && time < 150);
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
    event.start();
    while (testEventCallbackLoopCount < 5) {
    }
    long time = System.currentTimeMillis() - startTime;
    assertTrue(950 < time && time < 1050);
    assertEquals(testEventCallbackLoopCount, 5);
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
      assertEquals(testService.counter("TestEvent.TestCustomEvent", 1), 21);
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
}
