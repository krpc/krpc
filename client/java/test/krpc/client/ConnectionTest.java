package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertThrows;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.regex.Pattern;
import krpc.client.services.KRPC;
import krpc.client.services.TestService;
import krpc.schema.KRPC.Status;
import org.javatuples.Pair;
import org.junit.Before;
import org.junit.Test;

/** Tests for Connection. */
public class ConnectionTest {

  private Connection connection;
  private KRPC krpc;
  private TestService testService;

  @Before
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void setup() throws IOException {
    connection = TestUtils.connect();
    krpc = KRPC.newInstance(connection);
    testService = TestService.newInstance(connection);
  }

  @Test
  public void testGetStatus() throws RPCException {
    Status status = krpc.getStatus();
    assertTrue(Pattern.matches("^[0-9]+\\.[0-9]+\\.[0-9]+$", status.getVersion()));
    assertTrue(status.getBytesRead() > 0);
  }

  @Test(expected = IOException.class)
  public void testWrongRpcPort() throws IOException {
    Connection.newInstance("JavaClientTestWrongRpcPort", "localhost",
        TestUtils.getRpcPort() ^ TestUtils.getStreamPort(), TestUtils.getStreamPort());
  }

  @Test(expected = IOException.class)
  public void testWrongStreamPort() throws IOException {
    Connection.newInstance("JavaClientTestWrongStreamPort", "localhost",
        TestUtils.getRpcPort(), TestUtils.getRpcPort() ^ TestUtils.getStreamPort());
  }

  @Test
  public void testWrongRpcServer() {
    ConnectionException e = assertThrows(ConnectionException.class, () ->
        Connection.newInstance("JavaClientTestWrongRpcServer", "localhost",
            TestUtils.getStreamPort(), TestUtils.getStreamPort()));
    assertTrue(e.getMessage().contains(
        "Connection request was for the rpc server, but this is the stream server. "
        + "Did you connect to the wrong port number?"));
  }

  @Test
  public void testWrongStreamServer() {
    ConnectionException e = assertThrows(ConnectionException.class, () ->
        Connection.newInstance("JavaClientTestWrongStreamServer", "localhost",
            TestUtils.getRpcPort(), TestUtils.getRpcPort()));
    assertTrue(e.getMessage().contains(
        "Connection request was for the stream server, but this is the rpc server. "
        + "Did you connect to the wrong port number?"));
  }

  @Test
  public void testValueParameters() throws RPCException {
    assertEquals("3.14159", testService.floatToString(3.14159f));
    assertEquals("3.14159", testService.doubleToString(3.14159));
    assertEquals("42", testService.int32ToString(42));
    assertEquals("123456789000", testService.int64ToString(123456789000L));
    assertEquals("True", testService.boolToString(true));
    assertEquals("False", testService.boolToString(false));
    assertEquals(12345, testService.stringToInt32("12345"));
    assertEquals("deadbeef", testService.bytesToHexString(
                   new byte[] { (byte) 0xDE, (byte) 0xAD, (byte) 0xBE, (byte) 0xEF }));
  }

  @Test
  public void testMultipleValueParameters() throws RPCException {
    assertEquals("3.14159", testService.addMultipleValues(0.14159f, 1, 2));
  }

  @Test
  public void testProperties() throws RPCException {
    testService.setStringProperty("foo");
    assertEquals("foo", testService.getStringProperty());
    assertEquals("foo", testService.getStringPropertyPrivateSet());
    testService.setStringPropertyPrivateGet("foo");
    TestService.TestClass obj = testService.createTestObject("bar");
    testService.setObjectProperty(obj);
    assertEquals(obj, testService.getObjectProperty());
  }

  @Test
  public void testClassNullValue() throws RPCException {
    assertNull(testService.echoTestObject(null));
    TestService.TestClass obj = testService.createTestObject("bob");
    assertEquals("bobnull", obj.objectToString(null));
    testService.getObjectProperty();
    testService.setObjectProperty(null);
    assertNull(testService.getObjectProperty());
  }

  @Test
  public void testClassMethods() throws RPCException {
    TestService.TestClass obj = testService.createTestObject("bob");
    assertEquals("value=bob", obj.getValue());
    assertEquals("bob3.14159", obj.floatToString(3.14159f));
    TestService.TestClass obj2 = testService.createTestObject("bill");
    assertEquals("bobbill", obj.objectToString(obj2));
  }

  @Test
  public void testClassStaticMethods() throws RPCException {
    // Note: default arguments not supported, so have to pass "", ""
    assertEquals("jeb", TestService.TestClass.staticMethod(this.connection, "", ""));
    assertEquals("jebbobbill", TestService.TestClass.staticMethod(this.connection, "bob", "bill"));
  }

  @Test
  public void testClassProperties() throws RPCException {
    TestService.TestClass obj = testService.createTestObject("jeb");
    obj.setIntProperty(0);
    assertEquals(0, obj.getIntProperty());
    obj.setIntProperty(42);
    assertEquals(42, obj.getIntProperty());
    TestService.TestClass obj2 = testService.createTestObject("kermin");
    obj.setObjectProperty(obj2);
    assertEquals(obj2, obj.getObjectProperty());
    obj.setStringPropertyPrivateGet("bob");
    assertEquals("bob", obj.getStringPropertyPrivateSet());
  }

  @Test
  public void testBlockingProcedure() throws RPCException {
    // Note: must pass 0 as default arguments not possible
    assertEquals(0, testService.blockingProcedure(0, 0));
    assertEquals(1, testService.blockingProcedure(1, 0));
    assertEquals(1 + 2, testService.blockingProcedure(2, 0));
    int sum = 0;
    for (int i = 1; i < 43; sum += i, i++) {
    }
    assertEquals(sum, testService.blockingProcedure(42, 0));
  }

  @Test
  public void testEnums() throws RPCException {
    assertEquals(TestService.TestEnum.VALUE_B, testService.enumReturn());
    assertEquals(TestService.TestEnum.VALUE_A, testService.enumEcho(TestService.TestEnum.VALUE_A));
    assertEquals(TestService.TestEnum.VALUE_B, testService.enumEcho(TestService.TestEnum.VALUE_B));
    assertEquals(TestService.TestEnum.VALUE_C, testService.enumEcho(TestService.TestEnum.VALUE_C));
  }

  @Test
  public void testCollectionsList() throws RPCException {
    assertEquals(new ArrayList<Integer>(), testService.incrementList(new ArrayList<Integer>()));
    assertEquals(Arrays.asList(new Integer[] { 1, 2, 3 }),
                 testService.incrementList(Arrays.asList(new Integer[] { 0, 1, 2 })));
  }

  @Test
  @SuppressWarnings("serial")
  public void testCollectionsDictionary() throws RPCException {
    assertEquals(new HashMap<String, Integer>(),
                 testService.incrementDictionary(new HashMap<String, Integer>()));
    HashMap<String, Integer> m1 = new HashMap<>();
    m1.put("a", 0);
    m1.put("b", 1);
    m1.put("c", 2);
    HashMap<String, Integer> m2 = new HashMap<>();
    m2.put("a", 1);
    m2.put("b", 2);
    m2.put("c", 3);
    assertEquals(m2, testService.incrementDictionary(m1));
  }

  @Test
  public void testCollectionsSet() throws RPCException {
    assertEquals(new HashSet<Integer>(), testService.incrementSet(new HashSet<Integer>()));
    assertEquals(new HashSet<Integer>(Arrays.asList(new Integer[] { 1, 2, 3 })),
                 testService.incrementSet(
                   new HashSet<Integer>(Arrays.asList(new Integer[] { 0, 1, 2 }))));
  }

  @Test
  public void testCollectionsTuple() throws RPCException {
    assertEquals(new Pair<Integer, Long>(2, 3L),
                 testService.incrementTuple(new Pair<Integer, Long>(1, 2L)));
  }

  @Test
  @SuppressWarnings("serial")
  public void testCollectionsNested() throws RPCException {
    assertEquals(new HashMap<String, List<Integer>>(),
                 testService.incrementNestedCollection(new HashMap<String, List<Integer>>()));
    HashMap<String, List<Integer>> m1 = new HashMap<>();
    m1.put("a", Arrays.asList(new Integer[] { 0, 1 }));
    m1.put("b", new ArrayList<Integer>());
    m1.put("c", Arrays.asList(new Integer[] { 2 }));
    HashMap<String, List<Integer>> m2 = new HashMap<>();
    m2.put("a", Arrays.asList(new Integer[] { 1, 2 }));
    m2.put("b", new ArrayList<Integer>());
    m2.put("c", Arrays.asList(new Integer[] { 3 }));
    assertEquals(m2, testService.incrementNestedCollection(m1));
  }

  @Test
  public void testCollectionsOfObjects() throws RPCException {
    List<TestService.TestClass> list =
        testService.addToObjectList(new ArrayList<TestService.TestClass>(), "jeb");
    assertEquals(1, list.size());
    assertEquals("value=jeb", list.get(0).getValue());
    list = testService.addToObjectList(list, "bob");
    assertEquals(2, list.size());
    assertEquals("value=jeb", list.get(0).getValue());
    assertEquals("value=bob", list.get(1).getValue());
  }

  @Test
  public void testInvalidOperationException() {
    UnsupportedOperationException e = assertThrows(UnsupportedOperationException.class,
        () -> TestService.newInstance(connection).throwInvalidOperationException());
    assertTrue(e.getMessage().contains("Invalid operation"));
  }

  @Test
  public void testArgumentException() {
    IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
        () -> TestService.newInstance(connection).throwArgumentException());
    assertTrue(e.getMessage().contains("Invalid argument"));
  }

  @Test
  public void testArgumentNullException() {
    IllegalArgumentException e = assertThrows(IllegalArgumentException.class,
        () -> TestService.newInstance(connection).throwArgumentNullException(""));
    assertTrue(e.getMessage().contains("Value cannot be null.\nParameter name: foo"));
  }

  @Test
  public void testArgumentOutOfBoundsException() {
    IndexOutOfBoundsException e = assertThrows(IndexOutOfBoundsException.class,
        () -> TestService.newInstance(connection).throwArgumentOutOfRangeException(0));
    assertTrue(e.getMessage().contains(
        "Specified argument was out of the range of valid values.\nParameter name: foo"));
  }

  @Test
  public void testCustomException() {
    TestService.CustomException e = assertThrows(TestService.CustomException.class,
        () -> TestService.newInstance(connection).throwCustomException());
    assertTrue(e.getMessage().contains("A custom kRPC exception"));
  }

  @Test
  public void testLineEndings() throws RPCException {
    String[] strings = new String[] { "foo\nbar", "foo\rbar", "foo\n\rbar", "foo\r\nbar" };
    for (String string : strings) {
      testService.setStringProperty(string);
      assertEquals(string, testService.getStringProperty());
    }
  }

  @Test
  public void testThreadSafe() throws InterruptedException {
    int threadCount = 4;
    final int repeats = 1000;
    final CountDownLatch latch = new CountDownLatch(threadCount);
    for (int i = 0; i < threadCount; i++) {
      new Thread(new Runnable() {
          public void run() {
            try {
              for (int j = 0; j < repeats; j++) {
                assertEquals("False", testService.boolToString(false));
                assertEquals(12345, testService.stringToInt32("12345"));
              }
            } catch (Exception exn) {
              exn.printStackTrace();
              fail();
            }
            latch.countDown();
          }
        }).start();
    }
    assertTrue(latch.await(10, TimeUnit.SECONDS));
  }
}
