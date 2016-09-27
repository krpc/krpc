package krpc.client;

import static krpc.client.TestUtils.hexlify;
import static krpc.client.TestUtils.unhexlify;
import static org.junit.Assert.assertEquals;

import java.io.IOException;
import java.util.Arrays;
import java.util.Collection;

import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.Parameterized;
import org.junit.runners.Parameterized.Parameter;
import org.junit.runners.Parameterized.Parameters;

import com.google.protobuf.ByteString;

import krpc.client.Types;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

@RunWith(Parameterized.class)
public class EncoderBytesValueTest {
    @Parameters
    public static Collection<Object[]> data() {
        return Arrays.asList(new Object[][] { { "", "00" }, { "bada55", "03bada55" }, { "deadbeef", "04deadbeef" } });
    }

    @Parameter(value = 0)
    public String value;
    @Parameter(value = 1)
    public String data;

    Type type = Types.CreateValue(TypeCode.BYTES);

    @Test
    public void testEncode() throws IOException {
        ByteString encodeResult = Encoder.encode(unhexlify(value).toByteArray(), type);
        assertEquals(data, hexlify(encodeResult));
    }

    @Test
    public void testDecode() throws IOException {
        byte[] decodeResult = (byte[]) Encoder.decode(unhexlify(data), type, null);
        assertEquals(value, hexlify(decodeResult));
    }
}
