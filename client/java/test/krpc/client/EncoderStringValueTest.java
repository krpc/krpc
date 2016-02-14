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
public class EncoderStringValueTest {
    @Parameters
    public static Collection<Object[]> data() {
        return Arrays.asList(new Object[][] { { "", "00" }, { "testing", "0774657374696e67" },
                { "One small step for Kerbal-kind!",
                        "1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421" },
                { "\u2122", "03e284a2" }, { "Mystery Goo\u2122 Containment Unit",
                        "1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974" } });
    }

    @Parameter(value = 0)
    public String value;
    @Parameter(value = 1)
    public String data;

    @Test
    public void testEncode() throws IOException {
        ByteString encodeResult = Encoder.encode(value);
        assertEquals(data, hexlify(encodeResult));
    }

    @Test
    public void testDecode() throws IOException {
        String decodeResult = Encoder.decodeString(unhexlify(data));
        assertEquals(value, decodeResult);
    }
}
