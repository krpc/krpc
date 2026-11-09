package krpc.client;

import static org.junit.Assert.assertEquals;

import krpc.client.services.TestService.TestFlags;
import org.junit.Test;

/** The names a flags enumeration value formats as. */
public class RemoteFlagsTest {
  @Test
  public void testToStringNamesDeclaredValue() {
    assertEquals("NONE", TestFlags.NONE.toString());
    assertEquals("VALUE_AB", TestFlags.VALUE_A.or(TestFlags.VALUE_B).toString());
  }

  @Test
  public void testToStringJoinsFlagsOfCombination() {
    assertEquals(
        "VALUE_A | VALUE_C", TestFlags.VALUE_C.or(TestFlags.VALUE_A).toString());
  }
}
