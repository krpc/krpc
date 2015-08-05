using System;
using System.IO;
using Google.ProtocolBuffers;
using KRPC.Schema.KRPC;

namespace KRPC.Server.RPC
{
    sealed class RPCStream : IStream<Request,Response>
    {
        // 1MB buffer
        internal const int bufferSize = 1 * 1024 * 1024;
        readonly IStream<byte,byte> stream;
        Request bufferedRequest;
        byte[] buffer = new byte[bufferSize];
        int offset;

        public RPCStream (IStream<byte,byte> stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// Returns true if there is a request waiting to be read. A Call to Read() will
        /// not throw NoRequestException if this returns true. Throws MalformedRequestException
        /// if a malformed request is received.
        /// </summary>
        public bool DataAvailable {
            get {
                try {
                    Poll ();
                    return true;
                } catch (NoRequestException) {
                    return false;
                }
            }
        }

        /// <summary>
        /// Read a request from the client. Blocks until a request is available.
        /// Throws NoRequestException if there is no request.
        /// Throws MalformedRequestException if malformed data is received.
        /// </summary>
        public Request Read ()
        {
            Poll ();
            var request = bufferedRequest;
            bufferedRequest = null;
            return request;
        }

        public int Read (Request[] buffer, int offset)
        {
            throw new NotSupportedException ();
        }

        public int Read (Request[] buffer, int offset, int size)
        {
            throw new NotSupportedException ();
        }

        /// <summary>
        /// Write a response to the client.
        /// </summary>
        public void Write (Response value)
        {
            var tempBuffer = new MemoryStream ();
            value.WriteDelimitedTo (tempBuffer);
            stream.Write (tempBuffer.ToArray ());
        }

        public void Write (Response[] value)
        {
            throw new NotSupportedException ();
        }

        public ulong BytesRead {
            get { return stream.BytesRead; }
        }

        public ulong BytesWritten {
            get { return stream.BytesWritten; }
        }

        public void ClearStats ()
        {
            stream.ClearStats ();
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        public void Close ()
        {
            buffer = null;
            bufferedRequest = null;
            stream.Close ();
        }

        /// Returns quietly if there is a message in bufferedRequest
        /// Throws NoRequestException if not
        /// Throws MalformedRequestException if malformed data received
        /// Throws RequestBufferOverflowException if buffer full but complete request not received
        void Poll ()
        {
            if (bufferedRequest != null)
                return;

            // If there's no further data, we won't be able to deserialize a request
            if (!stream.DataAvailable)
                throw new NoRequestException ();

            // Read as much data as we can from the client into the buffer, up to the buffer size
            offset += stream.Read (buffer, offset);

            // Attempt to deserialize a request from the buffered data
            var bufferStream = new MemoryStream (buffer, false);
            try {
                bufferedRequest = Request.CreateBuilder ().MergeDelimitedFrom (bufferStream).BuildPartial ();
            } catch (InvalidProtocolBufferException) {
                // Failed to deserialize a request
                if (offset >= buffer.Length) {
                    // And the buffer is full
                    throw new RequestBufferOverflowException ();
                }
                // And the buffer not yet full
                // TODO: can we detect if the partial data received is a subset of a valid request?
                // And we read to the end, so we have a valid part of a request
                throw new NoRequestException ();
            }

            // Partial request is not complete, so some required fields weren't set
            if (!bufferedRequest.IsInitialized) {
                throw new MalformedRequestException ();
            }

            // Valid request received, reset the buffer
            offset = 0;
            bufferStream.Read (buffer, 0, buffer.Length);
        }
    }
}

