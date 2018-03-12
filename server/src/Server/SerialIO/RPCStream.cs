using System;
using System.Diagnostics.CodeAnalysis;
using Google.Protobuf;
using KRPC.Server.Message;
using KRPC.Server.ProtocolBuffers;
using KRPC.Service.Messages;

namespace KRPC.Server.SerialIO
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class RPCStream : Message.RPCStream
    {
        readonly CodedOutputStream codedOutputStream;
        readonly IServer<byte,byte> server;

        public RPCStream (IStream<byte,byte> stream, IServer<byte,byte> innerServer) : base (stream)
        {
            server = innerServer;
            codedOutputStream = new CodedOutputStream (new ByteOutputAdapterStream (stream), true);
        }

        public override void Write (Response value)
        {
            var result = new Schema.KRPC.MultiplexedResponse ();
            result.Response = value.ToProtobufMessage ();
            codedOutputStream.WriteMessage (result);
            codedOutputStream.Flush ();
        }

        protected override int Read (ref Request request, byte[] data, int offset, int length)
        {
            try {
                Schema.KRPC.MultiplexedRequest multiplexedRequest = null;
                int read = ProtocolBuffers.Utils.ReadMessage(
                    ref multiplexedRequest, Schema.KRPC.MultiplexedRequest.Parser, data, offset, length);
                if (read == 0)
                    return read;
                if (multiplexedRequest.ConnectionRequest != null) {
                    var bufferedData = new byte[length];
                    Array.Copy(data, offset, bufferedData, 0, length);
                    ((ByteServer)server).ClientConnectionRequest (bufferedData);
                    throw new ClientDisconnectedException ();
                }
                request = multiplexedRequest.Request.ToMessage ();
                return read;
            } catch (System.InvalidOperationException e) {
                throw new MalformedRequestException (e.Message);
            } catch (InvalidProtocolBufferException e) {
                throw new MalformedRequestException (e.Message);
            }
        }
    }
}
