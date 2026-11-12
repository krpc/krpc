package krpc.client;

import com.google.protobuf.ByteString;
import java.util.List;
import krpc.client.services.KRPC.Expression;
import krpc.schema.KRPC.ProcedureCall;
import krpc.schema.KRPC.ProcedureResult;
import krpc.schema.KRPC.Request;
import krpc.schema.KRPC.Type;
import krpc.schema.KRPC.Type.TypeCode;

/** Calls the internal procedures of the KRPC service. */
final class InternalProcedures {
  private static final Type PROCEDURE_CALL = Types.createMessage(TypeCode.PROCEDURE_CALL);
  private static final Type UINT64 = Types.createValue(TypeCode.UINT64);
  private static final Type FLOAT = Types.createValue(TypeCode.FLOAT);
  private static final Type BYTES = Types.createValue(TypeCode.BYTES);
  private static final Type EXPRESSION = Types.createClass("KRPC", "Expression");
  private static final Type ARGUMENTS = Types.createList(Types.nullable(BYTES));
  private static final Type EVENT = Types.createMessage(TypeCode.EVENT);

  private InternalProcedures() {}

  static long addStream(Connection connection, ProcedureCall call) throws RPCException {
    ProcedureResult result =
        connection.invokeKrpc("AddStream", true, Encoder.encode(call, PROCEDURE_CALL));
    return (long) decodeNonNull(result, UINT64, connection);
  }

  static void setStreamRate(Connection connection, long id, float rate) throws RPCException {
    connection.invokeKrpc(
        "SetStreamRate", false, Encoder.encode(id, UINT64), Encoder.encode(rate, FLOAT));
  }

  static void removeStream(Connection connection, long id) throws RPCException {
    connection.invokeKrpc("RemoveStream", false, Encoder.encode(id, UINT64));
  }

  /** A request to remove a stream, which the session sends itself. */
  static Request removeStreamRequest(Connection connection, long id) {
    return Request.newBuilder()
        .addCalls(connection.buildCall("KRPC", "RemoveStream", Encoder.encode(id, UINT64)))
        .build();
  }

  static long addFunctionStream(Connection connection, Expression function)
      throws RPCException {
    ProcedureResult result =
        connection.invokeKrpc("AddFunctionStream", true, Encoder.encode(function, EXPRESSION));
    return (long) decodeNonNull(result, UINT64, connection);
  }

  static void compileFunction(Connection connection, Expression function) throws RPCException {
    connection.invokeKrpc("CompileFunction", false, Encoder.encode(function, EXPRESSION));
  }

  /** Run a server side function. Returns null for a null value. */
  static byte[] runFunction(Connection connection, Expression function, List<byte[]> arguments)
      throws RPCException {
    ByteString[] args = new ByteString[] {
      Encoder.encode(function, EXPRESSION), Encoder.encode(arguments, ARGUMENTS)
    };
    ProcedureResult result = connection.invokeKrpc("RunFunction", false, args);
    if (result.getIsNull()) {
      return null;
    }
    return (byte[]) Decoder.decode(result.getValue(), BYTES, connection);
  }

  static Event addEvent(Connection connection, Expression function) throws RPCException {
    ProcedureResult result =
        connection.invokeKrpc("AddEvent", true, Encoder.encode(function, EXPRESSION));
    return (Event) decodeNonNull(result, EVENT, connection);
  }

  /** Decode a stream id or an event, which cannot be null. */
  private static Object decodeNonNull(ProcedureResult result, Type type, Connection connection) {
    if (result.getIsNull()) {
      throw new EncodingException("A null result for a value that cannot be null");
    }
    return Decoder.decode(result.getValue(), type, connection);
  }
}
