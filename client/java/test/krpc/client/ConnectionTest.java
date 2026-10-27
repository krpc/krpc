package krpc.client;

import static org.junit.Assert.assertEquals;
import static org.junit.Assert.assertThrows;
import static org.junit.Assert.assertTrue;

import java.io.IOException;
import java.util.Arrays;
import krpc.client.services.TestService;
import org.junit.Before;
import org.junit.Test;

/** Tests for Connection. */
public class ConnectionTest {

  private Connection connection;

  @Before
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public void setup() throws IOException {
    connection = TestUtils.connect();
  }

  @Test
  public void testStructComparisonOfCollectionFieldThrows() {
    // A collection has no ordering, so comparing two structures whose collection fields
    // differ throws, as comparing two javatuples of the same values does
    TestService.TestStruct a = new TestService.TestStruct(
        1, "alpha", TestService.TestEnum.VALUE_A, Arrays.asList(new Integer[] { 1 }));
    TestService.TestStruct b = new TestService.TestStruct(
        1, "alpha", TestService.TestEnum.VALUE_A, Arrays.asList(new Integer[] { 2 }));
    assertThrows(ClassCastException.class, () -> a.compareTo(b));
  }

  // A method name is matched by value, so one assembled at runtime resolves the same as an
  // interned literal.
  @Test
  public void testGetCallWithNonInternedMethodName() throws RPCException {
    String name = new StringBuilder("floatToString").toString();
    assertEquals(
        connection.getCall(TestService.class, "floatToString", 3.14159f),
        connection.getCall(TestService.class, name, 3.14159f));
  }

  // Arguments of the wrong type do not match, so a method of the same name taking the same
  // number of arguments is not resolved.
  @Test
  public void testGetCallWithWrongArgumentType() {
    RPCException e = assertThrows(RPCException.class,
        () -> connection.getCall(TestService.class, "floatToString", "not a float"));
    assertTrue(e.getMessage().contains("not found"));
  }
}
