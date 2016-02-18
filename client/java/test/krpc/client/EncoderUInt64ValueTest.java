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
public class EncoderUInt64ValueTest {
    @Parameters
    public static Collection<Object[]> data() {
        return Arrays.asList(new Object[][] {
            { 0, "00" },
            { 1, "01" },
            { 42, "2a" },
            { 300, "ac02" },
            { 1234567890000L, "d088ec8ff723" }
            // { Long.MAX_VALUE, "????"},
            // { Long.MIN_VALUE, "????" }
        });
    }

    @Parameter(value = 0)
    public long value;
    @Parameter(value = 1)
    public String data;

    @Test
    public void testEncode() throws IOException {
        ByteString encodeResult = Encoder.encode(value);
        assertEquals(data, hexlify(encodeResult));
    }

    @Test
    public void testDecode() throws IOException {
        long decodeResult = Encoder.decodeUInt64(unhexlify(data));
        assertEquals(value, decodeResult);
    }
}
