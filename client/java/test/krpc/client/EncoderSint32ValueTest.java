package krpc.client;

import static krpc.client.TestUtils.hexlify;
import static krpc.client.TestUtils.unhexlify;
import static org.junit.Assert.assertEquals;

import com.google.protobuf.ByteString;
import java.util.Arrays;
import java.util.Collection;
import krpc.client.Types;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.Parameterized;
import org.junit.runners.Parameterized.Parameter;
import org.junit.runners.Parameterized.Parameters;

/** Tests for EncoderSint32Value. */
@RunWith(Parameterized.class)
public class EncoderSint32ValueTest {
  @Parameters
  @SuppressWarnings("checkstyle:missingjavadocmethod")
  public static Collection<Object[]> data() {
    return Arrays.asList(new Object[][] {
      { 0, "00" },
      { 1, "02" },
      { 42, "54" },
      { 300, "d804" },
      { -33, "41" },
      { 2147483647, "feffffff0f" },
      { -2147483648, "ffffffff0f" }
    });
  }

  @Parameter(value = 0)
  public int value;
  @Parameter(value = 1)
  public String data;

  Type type = Types.createValue(TypeCode.SINT32);

  @Test
  public void testEncode() {
    ByteString encodeResult = Encoder.encode(value, type);
    assertEquals(data, hexlify(encodeResult));
  }

  @Test
  public void testDecode() {
    int decodeResult = (int) Encoder.decode(unhexlify(data), type, null);
    assertEquals(value, decodeResult);
  }
}
