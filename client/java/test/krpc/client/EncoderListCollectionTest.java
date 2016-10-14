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

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.List;

@RunWith(Parameterized.class)
public class EncoderListCollectionTest {
  @Parameters
  @SuppressWarnings("checkstyle:javadocmethod")
  public static Collection<Object[]> data() {
    return Arrays.asList(new Object[][] {
      { new ArrayList<Integer>(), "" },
      { Arrays.asList(1), "0a0101" },
      { Arrays.asList(1, 2, 3, 4), "0a01010a01020a01030a0104" }
    });
  }

  @Parameter(value = 0)
  public List<Integer> value;
  @Parameter(value = 1)
  public String data;

  Type type = Types.createList(Types.createValue(TypeCode.UINT32));

  @Test
  public void testEncode() {
    ByteString encodeResult = Encoder.encode(value, type);
    assertEquals(data, hexlify(encodeResult));
  }

  @Test
  @SuppressWarnings("unchecked")
  public void testDecode() {
    List<Integer> decodeResult = (List<Integer>) Encoder.decode(unhexlify(data), type, null);
    assertEquals(value, decodeResult);
  }
}
