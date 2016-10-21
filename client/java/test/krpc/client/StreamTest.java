package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertTrue;
import static org.junit.Assert.fail;

import java.io.IOException;

import org.junit.Before;
import org.junit.Test;

import krpc.client.services.TestService;
import krpc.client.services.TestService.TestClass;

public class StreamTest {

    private Connection connection;
    private TestService testService;

    @Before
    public void setup() throws IOException {
        connection = TestUtils.Connect();
        testService = TestService.newInstance(connection);
    }

    private void pause() {
        try {
            Thread.sleep(50);
        } catch (InterruptedException e) {
            throw new RuntimeException(e);
        }
    }

    @Test
    public void testMethod() throws RPCException, StreamException, IOException, NoSuchMethodException {
        Stream<String> x = connection.addStream(TestService.class, "floatToString", 3.14159f);
        for (int i = 0; i < 5; i++) {
            assertEquals("3.14159", x.get());
            pause();
        }
    }

    @Test
    public void testProperty() throws RPCException, StreamException, IOException, NoSuchMethodException {
        testService.setStringProperty("foo");
        Stream<String> x = connection.addStream(TestService.class, "getStringProperty");
        for (int i = 0; i < 5; i++) {
            assertEquals("foo", x.get());
            pause();
        }
    }

    @Test
    public void testClassMethod() throws RPCException, StreamException, IOException, NoSuchMethodException {
        TestClass obj = testService.createTestObject("bob");
        Stream<String> x = connection.addStream(obj, "floatToString", 3.14159f);
        for (int i = 0; i < 5; i++) {
            assertEquals("bob3.14159", x.get());
            pause();
        }
    }

    @Test
    public void testClassStaticMethod() throws RPCException, StreamException, IOException, NoSuchMethodException {
        // FIXME: Java does not support default parameter values, so have to pass ""
        Stream<String> x = connection.addStream(TestClass.class, "staticMethod", connection, "foo", "");
        for (int i = 0; i < 5; i++) {
            assertEquals("jebfoo", x.get());
            pause();
        }
    }

    @Test
    public void testClassProperty() throws RPCException, StreamException, IOException, NoSuchMethodException {
        TestClass obj = testService.createTestObject("jeb");
        obj.setIntProperty(42);
        Stream<Integer> x = connection.addStream(obj, "getIntProperty");
        for (int i = 0; i < 5; i++) {
            assertEquals(42, (int) x.get());
            pause();
        }
    }

    @Test
    public void testCounter() throws RPCException, StreamException, IOException, NoSuchMethodException {
        int count = -1;
        Stream<Integer> x = connection.addStream(TestService.class, "counter", 0);
        for (int i = 0; i < 5; i++) {
            assertTrue(count < x.get());
            count = x.get();
            pause();
        }
    }

    @Test
    public void testNested() throws RPCException, StreamException, IOException, NoSuchMethodException {
        Stream<String> x0 = connection.addStream(TestService.class, "floatToString", 0.123f);
        Stream<String> x1 = connection.addStream(TestService.class, "floatToString", 1.234f);
        for (int i = 0; i < 5; i++) {
            assertEquals("0.123", x0.get());
            assertEquals("1.234", x1.get());
            pause();
        }
    }

    @Test
    public void testInerleaved() throws RPCException, StreamException, IOException, NoSuchMethodException {
        Stream<String> s0 = connection.addStream(TestService.class, "int32ToString", 0);
        assertEquals("0", s0.get());

        pause();
        assertEquals("0", s0.get());

        Stream<String> s1 = connection.addStream(TestService.class, "int32ToString", 1);
        assertEquals("0", s0.get());
        assertEquals("1", s1.get());

        pause();
        assertEquals("0", s0.get());
        assertEquals("1", s1.get());

        s1.remove();
        assertEquals("0", s0.get());
        try {
            s1.get();
            fail();
        } catch (StreamException e) {
        }

        pause();
        assertEquals("0", s0.get());
        try {
            s1.get();
            fail();
        } catch (StreamException e) {
        }

        Stream<String> s2 = connection.addStream(TestService.class, "int32ToString", 2);
        assertEquals("0", s0.get());
        try {
            s1.get();
            fail();
        } catch (StreamException e) {
        }
        assertEquals("2", s2.get());

        pause();
        assertEquals("0", s0.get());
        try {
            s1.get();
            fail();
        } catch (StreamException e) {
        }
        assertEquals("2", s2.get());

        s0.remove();
        try {
            s0.get();
            fail();
        } catch (StreamException e) {
        }
        try {
            s1.get();
            fail();
        } catch (StreamException e) {
        }
        assertEquals("2", s2.get());

        pause();
        try {
            s0.get();
            fail();
        } catch (StreamException e) {
        }
        try {
            s1.get();
            fail();
        } catch (StreamException e) {
        }
        assertEquals("2", s2.get());

        s2.remove();
        try {
            s0.get();
            fail();
        } catch (StreamException e) {
        }
        try {
            s1.get();
            fail();
        } catch (StreamException e) {
        }
        try {
            s2.get();
            fail();
        } catch (StreamException e) {
        }

        pause();
        try {
            s0.get();
            fail();
        } catch (StreamException e) {
        }
        try {
            s1.get();
            fail();
        } catch (StreamException e) {
        }
        try {
            s2.get();
            fail();
        } catch (StreamException e) {
        }
    }

    @Test
    public void testRemoveStreamTwice() throws RPCException, StreamException, IOException, NoSuchMethodException {
        Stream<String> s = connection.addStream(TestService.class, "int32ToString", 0);
        assertEquals("0", s.get());

        pause();
        assertEquals("0", s.get());

        s.remove();
        try {
            s.get();
            fail();
        } catch (StreamException e) {
        }
        s.remove();
        try {
            s.get();
            fail();
        } catch (StreamException e) {
        }
    }

    @Test
    public void testAddStreamTwice() throws RPCException, StreamException, IOException, NoSuchMethodException {
        Stream<String> s0 = connection.addStream(TestService.class, "int32ToString", 42);
        // var streamId = s0.Id;
        assertEquals("42", s0.get());

        pause();
        assertEquals("42", s0.get());

        Stream<String> s1 = connection.addStream(TestService.class, "int32ToString", 42);
        // assertEquals(streamId, s1.Id);
        assertEquals("42", s0.get());
        assertEquals("42", s1.get());

        pause();
        assertEquals("42", s0.get());
        assertEquals("42", s1.get());
    }
}
