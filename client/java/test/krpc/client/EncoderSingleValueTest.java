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

@RunWith(Parameterized.class)
public class EncoderSingleValueTest {
    @Parameters
    public static Collection<Object[]> data() {
        return Arrays.asList(new Object[][] {
            { 3.14159265359f, "db0f4940" },
            { -1.0f, "000080bf" },
            { 0.0f, "00000000" },
            { Float.POSITIVE_INFINITY, "0000807f" },
            { Float.NEGATIVE_INFINITY, "000080ff" },
            { Float.NaN, "0000c07f" }
        });
    }

    @Parameter(value = 0)
    public float value;
    @Parameter(value = 1)
    public String data;

    @Test
    public void testEncode() throws IOException {
        ByteString encodeResult = Encoder.encode(value);
        assertEquals(data, hexlify(encodeResult));
    }

    @Test
    public void testDecode() throws IOException {
        float decodeResult = Encoder.decodeFloat(unhexlify(data));
        assertEquals(value, decodeResult, 0.0001);
    }
}
