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
public class EncoderUInt32ValueTest {
    @Parameters
    public static Collection<Object[]> data() {
        return Arrays.asList(new Object[][] {
            { 0, "00" },
            { 1, "01" },
            { 42, "2a" },
            { 300, "ac02" }
        });
    }

    @Parameter(value = 0)
    public int value;
    @Parameter(value = 1)
    public String data;

    Type type = Types.CreateValue(TypeCode.UINT32);

    @Test
    public void testEncode() throws IOException {
        ByteString encodeResult = Encoder.encode(value, type);
        assertEquals(data, hexlify(encodeResult));
    }

    @Test
    public void testDecode() throws IOException {
        int decodeResult = (int) Encoder.decode(unhexlify(data), type, null);
        assertEquals(value, decodeResult);
    }
}
