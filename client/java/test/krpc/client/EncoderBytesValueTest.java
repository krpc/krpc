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
public class EncoderBytesValueTest {
  @Parameters
  @SuppressWarnings("checkstyle:javadocmethod")
  public static Collection<Object[]> data() {
    return Arrays.asList(new Object[][] {
      { "", "00" },
      { "bada55", "03bada55" },
      { "deadbeef", "04deadbeef" }
    });
  }

  @Parameter(value = 0)
  public String value;
  @Parameter(value = 1)
  public String data;

  Type type = Types.createValue(TypeCode.BYTES);

  @Test
  public void testEncode() {
    ByteString encodeResult = Encoder.encode(unhexlify(value).toByteArray(), type);
    assertEquals(data, hexlify(encodeResult));
  }

  @Test
  public void testDecode() {
    byte[] decodeResult = (byte[]) Encoder.decode(unhexlify(data), type, null);
    assertEquals(value, hexlify(decodeResult));
  }
}
