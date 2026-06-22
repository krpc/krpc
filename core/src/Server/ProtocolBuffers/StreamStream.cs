using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    sealed class StreamStream : Message.StreamStream
    {
        // A lightweight IMessage wrapper around a plain List<StreamResult>.
        // Unlike RepeatedField<T>, List<T>.Clear() keeps its backing array,
        // so we avoid a new T[] allocation on every tick.
        sealed class StreamUpdateMessage : Google.Protobuf.IMessage
        {
            readonly List<Schema.KRPC.StreamResult> results = new List<Schema.KRPC.StreamResult> ();

            public MessageDescriptor Descriptor { get { return null; } }

            public void Add (Schema.KRPC.StreamResult result) { results.Add (result); }
            public void Clear () { results.Clear (); }

            public int CalculateSize ()
            {
                int size = 0;
                for (int i = 0; i < results.Count; i++) {
                    int rs = results [i].CalculateSize ();
                    size += 1 + CodedOutputStream.ComputeLengthSize (rs) + rs;
                }
                return size;
            }

            public void WriteTo (CodedOutputStream output)
            {
                for (int i = 0; i < results.Count; i++) {
                    output.WriteTag (1, WireFormat.WireType.LengthDelimited);
                    output.WriteMessage (results [i]);
                }
            }

            public void MergeFrom (CodedInputStream input)
            {
                throw new System.NotImplementedException ();
            }
        }

        readonly CodedOutputStream codedOutputStream;
        readonly StreamUpdateMessage streamUpdateMessage = new StreamUpdateMessage ();
        readonly Dictionary<ulong, Schema.KRPC.StreamResult> protoResultCache = new Dictionary<ulong, Schema.KRPC.StreamResult> ();

        public StreamStream (IStream<byte,byte> stream) : base (stream)
        {
            codedOutputStream = new CodedOutputStream (new ByteOutputAdapterStream (stream), true);
        }

        public override void Write (StreamUpdate value)
        {
            streamUpdateMessage.Clear ();
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
                streamUpdateMessage.Add (protoResult);
            }
            codedOutputStream.WriteMessage (streamUpdateMessage);
            codedOutputStream.Flush ();
        }
    }
}
