package krpc.client;

import static krpc.client.TestUtils.hexlify;
import static krpc.client.TestUtils.unhexlify;
import static org.junit.Assert.assertEquals;

import com.google.protobuf.ByteString;

import krpc.client.Types;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.Parameterized;
import org.junit.runners.Parameterized.Parameter;
import org.junit.runners.Parameterized.Parameters;

import java.io.IOException;
import java.util.Arrays;
import java.util.Collection;

@RunWith(Parameterized.class)
public class EncoderDoubleValueTest {
  @Parameters
  @SuppressWarnings("checkstyle:javadocmethod")
  public static Collection<Object[]> data() {
    return Arrays.asList(new Object[][] {
      { 0.0, "0000000000000000" },
      { -1.0, "000000000000f0bf" },
      { 3.14159265359, "ea2e4454fb210940" },
      { Double.POSITIVE_INFINITY, "000000000000f07f" },
      { Double.NEGATIVE_INFINITY, "000000000000f0ff" },
      { Double.NaN, "000000000000f87f" }
    });
  }

  @Parameter(value = 0)
  public double value;
  @Parameter(value = 1)
  public String data;

  Type type = Types.createValue(TypeCode.DOUBLE);

  @Test
  public void testEncode() throws IOException {
    ByteString encodeResult = Encoder.encode(value, type);
    assertEquals(data, hexlify(encodeResult));
  }

  @Test
  public void testDecode() throws IOException {
    double decodeResult = (double) Encoder.decode(unhexlify(data), type, null);
    assertEquals(value, decodeResult, 0.0001);
  }
}
