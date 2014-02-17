using System;
using System.IO;
using Google.ProtocolBuffers;
using KRPC.Schema.KRPC;
using KRPC.Server.Net;

namespace KRPC.Server.RPC
{
    sealed class RPCStream : IStream<Request,Response>
    {
        internal const int bufferSize = 1 * 1024 * 1024; // 1MB buffer
        private IStream<byte,byte> stream;
        private Request bufferedRequest;
        private byte[] buffer = new byte[bufferSize];
        private int offset = 0;

        public RPCStream (IStream<byte,byte> stream) {
            this.stream = stream;
        }

        /// Throws MalformedRequestException if malformed data received
        public bool DataAvailable {
            get {
                try {
                    Poll();
                    return true;
                } catch (NoRequestException) {
                    return false;
                }
            }
        }

        /// Throws NoRequestException if no message
        /// Throws MalformedRequestException if malformed data received
        public Request Read () {
            Poll();
            var request = bufferedRequest;
            bufferedRequest = null;
            return request;
        }

        public int Read (Request[] buffer, int offset) {
            throw new NotImplementedException ();
        }

        public int Read (Request[] buffer, int offset, int size) {
            throw new NotImplementedException ();
        }

        public void Write (Response value) {
            var buffer = new MemoryStream ();
            value.WriteDelimitedTo (buffer);
            stream.Write (buffer.ToArray ());
        }

        public void Write (Response[] value) {
            throw new NotImplementedException ();
        }

        public void Close() {
            buffer = null;
            bufferedRequest = null;
            stream.Close ();
        }

        /// Returns quietly if there is a message in bufferedRequest
        /// Throws NoRequestException if not
        /// Throws MalformedRequestException if malformed data received
        /// Throws RequestBufferOverflowException if buffer full but complete request not received
        public void Poll () {
            if (bufferedRequest != null)
                return;

            // If there's no further data, we won't be able to deserialize a request
            if (!stream.DataAvailable)
                throw new NoRequestException ();

            // Read as much data as we can from the client into the buffer, up to the buffer size
            offset += stream.Read (buffer, offset);

            // Attempt to deserialize a partial request from the buffered data
            var bufferStream = new MemoryStream (buffer, false);
            try {
                bufferedRequest = Request.CreateBuilder().MergeDelimitedFrom (bufferStream).BuildPartial();
            } catch (InvalidProtocolBufferException e) {
                Console.WriteLine (e.Message);
                // Failed to deserialize a request
                if (offset >= buffer.Length) {
                    // And the buffer is full
                    throw new RequestBufferOverflowException ();
                }
                // And the buffer not yet full
                // TODO: can we detect if the partial data received is a subset of a valid request?
                // And we read to the end, so we have a valid part part of a request
                throw new NoRequestException ();
            }

            // Partial request is not complete, so some required fields weren't set
            if (!bufferedRequest.IsInitialized) {
                throw new MalformedRequestException ();
            }

            // Valid request received, reset the buffer
            offset = 0;
            bufferStream.Read(buffer, 0, buffer.Length);
        }
    }
}

