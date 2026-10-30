package krpc.client;

import static org.junit.Assert.assertTrue;

import java.io.IOException;
import krpc.schema.KRPC.ProcedureResult;
import krpc.schema.KRPC.Type;
import org.junit.Test;

/**
 * How the client finds that a procedure returns an event, whose stream it registers before it
 * reads the stream's first update. The generated stubs give the return types, and without them
 * the client asks the server for the service definitions.
 */
public class StreamTest {

  /** Invoke a procedure that returns an event, and check its stream is registered. */
  private static void assertEventStreamIsRegistered(ClassLoader stubs)
      throws IOException, RPCException {
    Connection conn = TestUtils.connect();
    try {
      conn.stubsLoader = stubs;
      Type uint32 = Type.newBuilder().setCode(Type.TypeCode.UINT32).build();
      ProcedureResult result = conn.invoke(
          "TestService", "OnTimer", Encoder.encode(60000, uint32), Encoder.encode(1, uint32));
      long id = krpc.schema.KRPC.Event.parseFrom(result.getValue()).getStream().getId();
      assertTrue(conn.streamManager.isRegistered(id));
    } finally {
      conn.close();
    }
  }

  @Test(timeout = 30000)
  public void testStreamIsRegisteredWithoutStubs() throws IOException, RPCException {
    // A class loader with none of the generated stubs
    assertEventStreamIsRegistered(new ClassLoader(null) {});
  }

  @Test(timeout = 30000)
  public void testStreamIsRegisteredWhenStubsDoNotLink() throws IOException, RPCException {
    // A class loader whose classes fail to link
    assertEventStreamIsRegistered(new ClassLoader(null) {
      @Override
      protected Class<?> findClass(String name) {
        throw new NoClassDefFoundError(name);
      }
    });
  }
}
