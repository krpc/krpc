package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertNull;
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

import org.javatuples.Pair;
import org.junit.Before;
import org.junit.Rule;
import org.junit.Test;
import org.junit.rules.ExpectedException;

import krpc.client.services.KRPC;
import krpc.client.services.TestService;
import krpc.schema.KRPC.Status;

public class ConnectionTest {

    private Connection connection;
    private KRPC krpc;
    private TestService testService;

    @Before
    public void setup() throws IOException {
        connection = TestUtils.Connect();
        krpc = KRPC.newInstance(connection);
        testService = TestService.newInstance(connection);
    }

    @Test
    public void testGetStatus() throws RPCException, IOException {
        Status status = krpc.getStatus();
        assertTrue(Pattern.matches("^[0-9]+\\.[0-9]+\\.[0-9]+$", status.getVersion()));
        assertTrue(status.getBytesRead() > 0);
    }

    @Test
    public void testCurrentGameScene() throws RPCException, IOException {
        assertEquals(KRPC.GameScene.SPACE_CENTER, krpc.getCurrentGameScene());
    }

    @Rule
    public ExpectedException expectedException = ExpectedException.none();

    @Test
    public void testErrorInvalidArgument() throws RPCException, IOException {
        expectedException.expect(RPCException.class);
        expectedException.expectMessage("Invalid argument");
        TestService.newInstance(connection).throwArgumentException();
    }

    @Test
    public void testErrorInvalidOperation() throws RPCException, IOException {
        expectedException.expect(RPCException.class);
        expectedException.expectMessage("Invalid operation");
        TestService.newInstance(connection).throwInvalidOperationException();
    }

    @Test
    public void testValueParameters() throws RPCException, IOException {
        assertEquals("3.14159", testService.floatToString(3.14159f));
        assertEquals("3.14159", testService.doubleToString(3.14159));
        assertEquals("42", testService.int32ToString(42));
        assertEquals("123456789000", testService.int64ToString(123456789000L));
        assertEquals("True", testService.boolToString(true));
        assertEquals("False", testService.boolToString(false));
        assertEquals(12345, testService.stringToInt32("12345"));
        assertEquals("deadbeef", testService.bytesToHexString(new byte[] { (byte) 0xDE, (byte) 0xAD, (byte) 0xBE, (byte) 0xEF }));
    }

    @Test
    public void testMultipleValueParameters() throws RPCException, IOException {
        assertEquals("3.14159", testService.addMultipleValues(0.14159f, 1, 2));
    }

    @Test
    public void testProperties() throws RPCException, IOException {
        testService.setStringProperty("foo");
        assertEquals("foo", testService.getStringProperty());
        assertEquals("foo", testService.getStringPropertyPrivateSet());
        testService.setStringPropertyPrivateGet("foo");
        TestService.TestClass obj = testService.createTestObject("bar");
        testService.setObjectProperty(obj);
        assertEquals(obj, testService.getObjectProperty());
    }

    @Test
    public void testClassNullValue() throws RPCException, IOException {
        assertNull(testService.echoTestObject(null));
        TestService.TestClass obj = testService.createTestObject("bob");
        assertEquals("bobnull", obj.objectToString(null));
        testService.getObjectProperty();
        testService.setObjectProperty(null);
        assertNull(testService.getObjectProperty());
    }

    @Test
    public void testClassMethods() throws RPCException, IOException {
        TestService.TestClass obj = testService.createTestObject("bob");
        assertEquals("value=bob", obj.getValue());
        assertEquals("bob3.14159", obj.floatToString(3.14159f));
        TestService.TestClass obj2 = testService.createTestObject("bill");
        assertEquals("bobbill", obj.objectToString(obj2));
    }

    @Test
    public void testClassStaticMethods() throws RPCException, IOException {
        // Note: default arguments not supported, so have to pass "", ""
        assertEquals("jeb", TestService.TestClass.staticMethod(this.connection, "", ""));
        assertEquals("jebbobbill", TestService.TestClass.staticMethod(this.connection, "bob", "bill"));
    }

    @Test
    public void testClassProperties() throws RPCException, IOException {
        TestService.TestClass obj = testService.createTestObject("jeb");
        obj.setIntProperty(0);
        assertEquals(0, obj.getIntProperty());
        obj.setIntProperty(42);
        assertEquals(42, obj.getIntProperty());
        TestService.TestClass obj2 = testService.createTestObject("kermin");
        obj.setObjectProperty(obj2);
        assertEquals(obj2, obj.getObjectProperty());
    }

    @Test
    public void testBlockingProcedure() throws RPCException, IOException {
        // Note: must pass 0 as default arguments not possible
        assertEquals(0, testService.blockingProcedure(0, 0));
        assertEquals(1, testService.blockingProcedure(1, 0));
        assertEquals(1 + 2, testService.blockingProcedure(2, 0));
        int sum = 0;
        for (int i = 1; i < 43; sum += i, i++)
            ;
        assertEquals(sum, testService.blockingProcedure(42, 0));
    }

    @Test
    public void testEnums() throws RPCException, IOException {
        assertEquals(TestService.TestEnum.VALUE_B, testService.enumReturn());
        assertEquals(TestService.TestEnum.VALUE_A, testService.enumEcho(TestService.TestEnum.VALUE_A));
        assertEquals(TestService.TestEnum.VALUE_B, testService.enumEcho(TestService.TestEnum.VALUE_B));
        assertEquals(TestService.TestEnum.VALUE_C, testService.enumEcho(TestService.TestEnum.VALUE_C));
    }

    @Test
    public void testCollectionsList() throws RPCException, IOException {
        assertEquals(new ArrayList<Integer>(), testService.incrementList(new ArrayList<Integer>()));
        assertEquals(Arrays.asList(new Integer[] { 1, 2, 3 }), testService.incrementList(Arrays.asList(new Integer[] { 0, 1, 2 })));
    }

    @SuppressWarnings({ "serial" })
    @Test
    public void testCollectionsDictionary() throws RPCException, IOException {
        assertEquals(new HashMap<String, Integer>(), testService.incrementDictionary(new HashMap<String, Integer>()));
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
    public void testCollectionsSet() throws RPCException, IOException {
        assertEquals(new HashSet<Integer>(), testService.incrementSet(new HashSet<Integer>()));
        assertEquals(new HashSet<Integer>(Arrays.asList(new Integer[] { 1, 2, 3 })), testService.incrementSet(new HashSet<Integer>(Arrays.asList(new Integer[] { 0, 1, 2 }))));
    }

    @Test
    public void testCollectionsTuple() throws RPCException, IOException {
        assertEquals(new Pair<Integer, Long>(2, 3L), testService.incrementTuple(new Pair<Integer, Long>(1, 2L)));
    }

    @SuppressWarnings({ "serial" })
    @Test
    public void testCollectionsNested() throws RPCException, IOException {
        assertEquals(new HashMap<String, List<Integer>>(), testService.incrementNestedCollection(new HashMap<String, List<Integer>>()));
        assertEquals(new HashMap<String, List<Integer>>() {
            {
                put("a", Arrays.asList(new Integer[] { 1, 2 }));
                put("b", new ArrayList<Integer>());
                put("c", Arrays.asList(new Integer[] { 3 }));
            }
        }, testService.incrementNestedCollection(new HashMap<String, List<Integer>>() {
            {
                put("a", Arrays.asList(new Integer[] { 0, 1 }));
                put("b", new ArrayList<Integer>());
                put("c", Arrays.asList(new Integer[] { 2 }));
            }
        }));
    }

    @Test
    public void testCollectionsOfObjects() throws RPCException, IOException {
        List<TestService.TestClass> l = testService.addToObjectList(new ArrayList<TestService.TestClass>(), "jeb");
        assertEquals(1, l.size());
        assertEquals("value=jeb", l.get(0).getValue());
        l = testService.addToObjectList(l, "bob");
        assertEquals(2, l.size());
        assertEquals("value=jeb", l.get(0).getValue());
        assertEquals("value=bob", l.get(1).getValue());
    }

    @Test
    public void testLineEndings() throws RPCException, IOException {
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
                    } catch (Exception e) {
                        e.printStackTrace();
                        fail();
                    }
                    latch.countDown();
                }
            }).start();
        }
        assertTrue(latch.await(10, TimeUnit.SECONDS));
    }
}
