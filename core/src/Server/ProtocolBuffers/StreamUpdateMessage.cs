using System.Collections.Generic;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using KRPC.Service.Messages;
using IMessage = Google.Protobuf.IMessage;

namespace KRPC.Server.ProtocolBuffers
{
    /// <summary>
    /// A StreamUpdate encoded from a reused list of reused results. List.Clear keeps its
    /// backing array, so an update allocates nothing.
    /// </summary>
    sealed class StreamUpdateMessage : IMessage
    {
        readonly List<Schema.KRPC.StreamResult> results = new List<Schema.KRPC.StreamResult> ();
        readonly Dictionary<ulong, Schema.KRPC.StreamResult> cache = new Dictionary<ulong, Schema.KRPC.StreamResult> ();

        public MessageDescriptor Descriptor { get { return null; } }

        /// <summary>
        /// Fill the message from a stream update.
        /// </summary>
        public void Set (StreamUpdate value)
        {
            results.Clear ();
            var updateResults = value.Results;
            for (int i = 0; i < updateResults.Count; i++) {
                var r = updateResults [i];
                Schema.KRPC.StreamResult protoResult;
                if (!cache.TryGetValue (r.Id, out protoResult)) {
                    protoResult = new Schema.KRPC.StreamResult ();
                    protoResult.Id = r.Id;
                    protoResult.Result = new Schema.KRPC.ProcedureResult ();
                    cache [r.Id] = protoResult;
                }
                var pr = protoResult.Result;
                pr.Value = ByteString.Empty;
                pr.IsNull = false;
                pr.Error = null;
                if (r.Result.HasValue) {
                    if (r.Result.Value == null)
                        pr.IsNull = true;
                    else
                        pr.Value = r.Result.EncodedValue ?? Encoder.Encode (r.Result.Value, r.Result.Spec);
                } else if (r.Result.HasError)
                    pr.Error = r.Result.Error.ToProtobufMessage ();
                results.Add (protoResult);
            }
        }

        /// <summary>
        /// Free the result kept for a stream.
        /// </summary>
        public void Removed (ulong id)
        {
            cache.Remove (id);
        }

        /// <summary>
        /// Free the results kept for every stream.
        /// </summary>
        public void Clear ()
        {
            results.Clear ();
            cache.Clear ();
        }

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
}
