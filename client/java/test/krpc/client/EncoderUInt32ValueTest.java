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
public class EncoderUInt32ValueTest {
    @Parameters
    public static Collection<Object[]> data() {
        return Arrays.asList(new Object[][] {
            { 0, "00" },
            { 1, "01" },
            { 42, "2a" },
            { 300, "ac02" }
            // { Integer.MAX_VALUE, "ffffffffffffffff7f"},
            // { Integer.MIN_VALUE, "808080808080808001" }
        });
    }

    @Parameter(value = 0)
    public int value;
    @Parameter(value = 1)
    public String data;

    @Test
    public void testEncode() throws IOException {
        ByteString encodeResult = Encoder.encode(value);
        assertEquals(data, hexlify(encodeResult));
    }

    @Test
    public void testDecode() throws IOException {
        int decodeResult = Encoder.decodeUInt32(unhexlify(data));
        assertEquals(value, decodeResult);
    }
}
