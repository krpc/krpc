using System.Collections.Generic;
using Google.Protobuf;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class StreamStream : Message.StreamStream
    {
        readonly CodedOutputStream codedOutputStream;
        readonly Schema.KRPC.StreamUpdate protoStreamUpdate = new Schema.KRPC.StreamUpdate ();
        readonly Dictionary<ulong, Schema.KRPC.StreamResult> protoResultCache = new Dictionary<ulong, Schema.KRPC.StreamResult> ();

        public StreamStream (IStream<byte,byte> stream) : base (stream)
        {
            codedOutputStream = new CodedOutputStream (new ByteOutputAdapterStream (stream), true);
        }

        public override void Write (StreamUpdate value)
        {
            protoStreamUpdate.Results.Clear ();
            var results = value.Results;
            for (int i = 0; i < results.Count; i++) {
                var r = results [i];
                Schema.KRPC.StreamResult protoResult;
                if (!protoResultCache.TryGetValue (r.Id, out protoResult)) {
                    protoResult = new Schema.KRPC.StreamResult ();
                    protoResult.Id = r.Id;
                    protoResult.Result = new Schema.KRPC.ProcedureResult ();
                    protoResultCache [r.Id] = protoResult;
                }
                var pr = protoResult.Result;
                pr.Value = ByteString.Empty;
                pr.Error = null;
                if (r.Result.HasValue)
                    pr.Value = Encoder.Encode (r.Result.Value);
                else if (r.Result.HasError)
                    pr.Error = r.Result.Error.ToProtobufMessage ();
                protoStreamUpdate.Results.Add (protoResult);
            }
            codedOutputStream.WriteMessage (protoStreamUpdate);
            codedOutputStream.Flush ();
        }
    }
}
