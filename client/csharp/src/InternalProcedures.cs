using System.Collections.Generic;
using KRPC.Schema.KRPC;
using TypeCode = KRPC.Schema.KRPC.Type.Types.TypeCode;

namespace KRPC.Client
{
    /// <summary>
    /// Calls the internal procedures of the KRPC service.
    /// </summary>
    static class InternalProcedures
    {
        static readonly TypeSpec procedureCallSpec = TypeSpec.For (typeof (ProcedureCall));
        static readonly TypeSpec uint64Spec = TypeSpec.For (typeof (ulong));
        static readonly TypeSpec floatSpec = TypeSpec.For (typeof (float));
        static readonly TypeSpec expressionSpec = TypeSpec.For (typeof (Services.KRPC.Expression));
        static readonly TypeSpec argumentsSpec = new TypeSpec (
            typeof (IList<byte[]>), TypeSpec.Null (typeof (byte[])));
        static readonly TypeSpec bytesSpec = TypeSpec.For (typeof (byte[]));
        static readonly TypeSpec eventSpec = TypeSpec.For (typeof (Event));

        internal static ulong AddStream (Connection connection, ProcedureCall call)
        {
            var args = new [] { Encoder.Encode (call, procedureCallSpec) };
            var result = connection.Invoke ("KRPC", "AddStream", args, TypeCode.Stream);
            return (ulong)DecodeNonNull (connection, result, uint64Spec);
        }

        internal static void SetStreamRate (Connection connection, ulong id, float rate)
        {
            var args = new [] { Encoder.Encode (id, uint64Spec), Encoder.Encode (rate, floatSpec) };
            connection.Invoke ("KRPC", "SetStreamRate", args);
        }

        internal static void RemoveStream (Connection connection, ulong id)
        {
            connection.Invoke ("KRPC", "RemoveStream", new [] { Encoder.Encode (id, uint64Spec) });
        }

        internal static ulong AddFunctionStream (Connection connection, Services.KRPC.Expression function)
        {
            var args = new [] { Encoder.Encode (function, expressionSpec) };
            var result = connection.Invoke ("KRPC", "AddFunctionStream", args, TypeCode.Stream);
            return (ulong)DecodeNonNull (connection, result, uint64Spec);
        }

        internal static void CompileFunction (Connection connection, Services.KRPC.Expression function)
        {
            connection.Invoke ("KRPC", "CompileFunction", new [] { Encoder.Encode (function, expressionSpec) });
        }

        /// <summary>
        /// Run a server side function. Returns null for a null value.
        /// </summary>
        internal static byte[] RunFunction (
            Connection connection, Services.KRPC.Expression function, IList<byte[]> arguments = null)
        {
            var args = new [] {
                Encoder.Encode (function, expressionSpec),
                Encoder.Encode (arguments ?? new List<byte[]> (), argumentsSpec)
            };
            var result = connection.Invoke ("KRPC", "RunFunction", args);
            if (result.IsNull)
                return null;
            return (byte[])Encoder.Decode (result.Value, bytesSpec, connection);
        }

        internal static Event AddEvent (Connection connection, Services.KRPC.Expression function)
        {
            var args = new [] { Encoder.Encode (function, expressionSpec) };
            var result = connection.Invoke ("KRPC", "AddEvent", args, TypeCode.Event);
            return (Event)DecodeNonNull (connection, result, eventSpec);
        }

        // A stream id or an event, which cannot be null
        static object DecodeNonNull (Connection connection, ProcedureResult result, TypeSpec spec)
        {
            if (result.IsNull)
                throw new EncodingException ("A null result for a value that cannot be null");
            return Encoder.Decode (result.Value, spec, connection);
        }
    }
}
