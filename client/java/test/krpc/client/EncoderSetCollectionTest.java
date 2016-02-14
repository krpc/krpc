package krpc.client;

import static krpc.client.TestUtils.hexlify;
import static krpc.client.TestUtils.unhexlify;
import static org.junit.Assert.assertEquals;

import java.io.IOException;
import java.util.Arrays;
import java.util.Collection;
import java.util.HashSet;
import java.util.Set;

import org.junit.Test;
import org.junit.runner.RunWith;
import org.junit.runners.Parameterized;
import org.junit.runners.Parameterized.Parameter;
import org.junit.runners.Parameterized.Parameters;

import com.google.protobuf.ByteString;

@RunWith(Parameterized.class)
public class EncoderSetCollectionTest {
    @Parameters
    public static Collection<Object[]> data() {
        return Arrays.asList(new Object[][] {
            { new HashSet<Integer>(), "" },
            { new HashSet<Integer>(Arrays.asList(1)), "0a0101" },
            { new HashSet<Integer>(Arrays.asList(1, 2, 3, 4)), "0a01010a01020a01030a0104" }
        });
    }

    @Parameter(value = 0)
    public Set<Integer> value;
    @Parameter(value = 1)
    public String data;

    @Test
    public void testEncode() throws IOException {
        ByteString encodeResult = Encoder.encode(value);
        assertEquals(data, hexlify(encodeResult));
    }

    @SuppressWarnings({ "unchecked" })
    @Test
    public void testDecode() throws IOException {
        TypeSpecification typeSpec = new TypeSpecification(Set.class, new TypeSpecification(Integer.class));
        Set<Integer> decodeResult = (Set<Integer>) Encoder.decode(unhexlify(data), typeSpec, null);
        assertEquals(value, decodeResult);
    }
}
