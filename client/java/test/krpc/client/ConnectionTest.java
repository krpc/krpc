package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

import krpc.client.services.KRPC;
import krpc.client.services.TestService;
import krpc.schema.KRPC.Status;

import org.javatuples.Pair;
import org.junit.Before;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.ExpectedException;

import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.regex.Pattern;

public class ConnectionTest {

  private Connection connection;
  private KRPC krpc;
  private TestService testService;

  @Rule
  public ExpectedException expectedException = ExpectedException.none();

  @Before
  @SuppressWarnings("checkstyle:javadocmethod")
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
  public void testWrongRpcServer() throws IOException {
    expectedException.expect(ConnectionException.class);
    expectedException.expectMessage(
        "Connection request was for the rpc server, but this is the stream server. "
        + "Did you connect to the wrong port number?");
    Connection.newInstance("JavaClientTestWrongRpcServer", "localhost",
        TestUtils.getStreamPort(), TestUtils.getStreamPort());
  }

  @Test
  public void testWrongStreamServer() throws IOException {
    expectedException.expect(ConnectionException.class);
    expectedException.expectMessage(
        "Connection request was for the stream server, but this is the rpc server. "
        + "Did you connect to the wrong port number?");
    Connection.newInstance("JavaClientTestWrongStreamServer", "localhost",
        TestUtils.getRpcPort(), TestUtils.getRpcPort());
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
    assertEquals(new HashMap<String, Integer>() {
        {
          put("a", 1);
          put("b", 2);
          put("c", 3);
        }
      }, testService.incrementDictionary(new HashMap<String, Integer>() {
          {
            put("a", 0);
            put("b", 1);
            put("c", 2);
          }
        }));
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
    assertEquals(new HashMap<String, List<Integer>>() {
          {
            put("a", Arrays.asList(new Integer[] { 1, 2 }));
            put("b", new ArrayList<Integer>());
            put("c", Arrays.asList(new Integer[] { 3 }));
          }
        },
        testService.incrementNestedCollection(new HashMap<String, List<Integer>>() {
          {
            put("a", Arrays.asList(new Integer[] { 0, 1 }));
            put("b", new ArrayList<Integer>());
            put("c", Arrays.asList(new Integer[] { 2 }));
          }
        }));
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
  public void testInvalidOperationException() throws RPCException {
    expectedException.expect(UnsupportedOperationException.class);
    expectedException.expectMessage("Invalid operation");
    TestService.newInstance(connection).throwInvalidOperationException();
  }

  @Test
  public void testArgumentException() throws RPCException {
    expectedException.expect(IllegalArgumentException.class);
    expectedException.expectMessage("Invalid argument");
    TestService.newInstance(connection).throwArgumentException();
  }

  @Test
  public void testArgumentNullException() throws RPCException {
    expectedException.expect(IllegalArgumentException.class);
    expectedException.expectMessage("Value cannot be null.\nParameter name: foo");
    TestService.newInstance(connection).throwArgumentNullException("");
  }

  @Test
  public void testArgumentOutOfBoundsException() throws RPCException {
    expectedException.expect(IndexOutOfBoundsException.class);
    expectedException.expectMessage(
        "Specified argument was out of the range of valid values.\nParameter name: foo");
    TestService.newInstance(connection).throwArgumentOutOfRangeException(0);
  }

  @Test
  public void testCustomException() throws RPCException {
    expectedException.expect(TestService.CustomException.class);
    expectedException.expectMessage("A custom kRPC exception");
    TestService.newInstance(connection).throwCustomException();
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
