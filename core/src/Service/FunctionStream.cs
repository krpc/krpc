using Google.Protobuf;

namespace KRPC.Service
{
    /// <summary>
    /// A stream that evaluates a server side function on each update and
    /// streams its value to the client.
    /// </summary>
    sealed class FunctionStream : Stream
    {
        readonly global::KRPC.Service.KRPC.Expression function;
        readonly TypeSpec spec;

        public FunctionStream (global::KRPC.Service.KRPC.Expression function)
        {
            function.CheckNoParameters ();
            // Check that the type of the value produced can be sent to the client
            function.GetValidReturnType ();
            spec = function.FunctionSpec;
            function.Check ();
            function.Compile ();
            this.function = function;
        }

        public override void UpdateInternal ()
        {
            var result = StreamResult.Result;
            object value;
            try {
                value = function.Evaluate ();
            } catch (System.Exception e) {
                SetError (e);
                return;
            }
            // An encoding error, such as a null element in a list of objects, fails only this
            // stream. The update sends this encoding.
            ByteString encoded = null;
            if (!ReferenceEquals (value, null)) {
                try {
                    encoded = Server.ProtocolBuffers.Encoder.Encode (value, spec);
                } catch (ServiceException e) {
                    SetError (e);
                    return;
                }
            }
            // Compare by the encoding, as a collection changed in place is the same object
            if (result.HasValue && Equals (encoded, result.EncodedValue))
                return;
            result.Reset ();
            result.Spec = spec;
            result.Value = value;
            result.EncodedValue = encoded;
            Changed = true;
        }
    }
}
