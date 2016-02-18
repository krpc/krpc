package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertNotEquals;
import static org.junit.Assert.assertTrue;

import java.io.IOException;

import org.junit.Before;
import org.junit.Test;

import krpc.client.services.TestService;

public class RemoteObjectTest {

    private Connection connection;
    private TestService testService;

    @Before
    public void setup() throws IOException {
        connection = TestUtils.Connect();
        testService = TestService.newInstance(connection);
    }

    @Test
    public void testEquality() throws RPCException, IOException {
        TestService.TestClass obj1 = testService.createTestObject("jeb");
        TestService.TestClass obj2 = testService.createTestObject("jeb");
        assertTrue(obj1.equals(obj2));

        TestService.TestClass obj3 = testService.createTestObject("bob");
        assertFalse(obj1.equals(obj3));

        testService.setObjectProperty(obj1);
        TestService.TestClass obj1a = testService.getObjectProperty();
        assertEquals(obj1, obj1a);

        assertFalse(obj1.equals(null));
    }

    @Test
    public void testHash() throws RPCException, IOException {
        TestService.TestClass obj1 = testService.createTestObject("jeb");
        TestService.TestClass obj2 = testService.createTestObject("jeb");
        TestService.TestClass obj3 = testService.createTestObject("bob");
        assertEquals(obj1.hashCode(), obj2.hashCode());
        assertNotEquals(obj1.hashCode(), obj3.hashCode());

        testService.setObjectProperty(obj1);
        TestService.TestClass obj1a = testService.getObjectProperty();
        assertEquals(obj1.hashCode(), obj1a.hashCode());
    }

    @Test
    public void testMemoryAllocation() throws RPCException, IOException {
        TestService.TestClass obj1 = testService.createTestObject("jeb");
        TestService.TestClass obj2 = testService.createTestObject("jeb");
        TestService.TestClass obj3 = testService.createTestObject("bob");
        assertEquals(obj1, obj2);
        assertNotEquals(obj1, obj3);
    }
}
