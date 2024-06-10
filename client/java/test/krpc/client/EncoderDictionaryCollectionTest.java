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
import java.util.HashMap;
import java.util.Map;

@RunWith(Parameterized.class)
public class EncoderDictionaryCollectionTest {
  @Parameters
  @SuppressWarnings({"serial", "checkstyle:javadocmethod"})
  public static Collection<Object[]> data() {
    HashMap<String, Integer> m2 = new HashMap<>();
    m2.put("", 0);
    HashMap<String, Integer> m3 = new HashMap<>();
    m3.put("foo", 42);
    m3.put("bar", 365);
    m3.put("baz", 3);
    return Arrays.asList(new Object[][] {
      { new HashMap<>(), "" },
      { m2, "0a060a0100120100" },
      { m3, "0a0a0a04036261721202ed020a090a0403666f6f12012a0a090a040362617a120103" }});
  }

  @Parameter(value = 0)
  public Map<String, Integer> value;
  @Parameter(value = 1)
  public String data;

  Type type = Types.createDictionary(
      Types.createValue(TypeCode.STRING), Types.createValue(TypeCode.UINT32));

  @Test
  public void testEncode() {
    ByteString encodeResult = Encoder.encode(value, type);
    assertEquals(data, hexlify(encodeResult));
  }

  @SuppressWarnings("unchecked")
  @Test
  public void testDecode() {
    Map<String, Integer> decodeResult =
        (Map<String, Integer>) Encoder.decode(unhexlify(data), type, null);
    assertEquals(value, decodeResult);
  }
}
