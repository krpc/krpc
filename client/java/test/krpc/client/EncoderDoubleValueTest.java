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
public class EncoderDoubleValueTest {
    @Parameters
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

    @Test
    public void testEncode() throws IOException {
        ByteString encodeResult = Encoder.encode(value);
        assertEquals(data, hexlify(encodeResult));
    }

    @Test
    public void testDecode() throws IOException {
        double decodeResult = Encoder.decodeDouble(unhexlify(data));
        assertEquals(value, decodeResult, 0.0001);
    }
}
