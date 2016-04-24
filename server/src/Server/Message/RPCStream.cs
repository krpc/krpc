using System;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    abstract class RPCStream : IStream<Request,Response>
    {
        // 1MB buffer
        internal const int bufferSize = 1 * 1024 * 1024;
        Request bufferedRequest;
        //FIXME: use a circular buffer, so that when index advances forward the space preceeding it can be used
        byte[] buffer = new byte[bufferSize];
        int index;
        int offset;

        protected RPCStream (IStream<byte,byte> stream)
        {
            Stream = stream;
        }

        protected IStream<byte,byte> Stream { get; private set; }

        /// <summary>
        /// Attempt to decode a request object from the given data.
        /// If the decoding fails to decode a message, a NoRequestException is thrown.
        /// If the decoding failed to read a message, but bytes were consumed, the number of
        /// bytes consumed is set in <paramref name="read"/>.
        /// If the decoding succeeds, <paramref name="read"/> is assumed to have been set to length,
        /// i.e. length bytes must have been consumed when decoding the request.
        /// </summary>
        //FIXME: this interface is a bit gnarly - can we do better?
        protected abstract Request Decode (byte[] data, int start, int length, ref int read);

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
        public abstract void Write (Response value);

        public void Write (Response[] value)
        {
            throw new NotSupportedException ();
        }

        public ulong BytesRead {
            get { return Stream.BytesRead; }
        }

        public ulong BytesWritten {
            get { return Stream.BytesWritten; }
        }

        public void ClearStats ()
        {
            Stream.ClearStats ();
        }

        /// <summary>
        /// Close the stream.
        /// </summary>
        public void Close ()
        {
            buffer = null;
            bufferedRequest = null;
            Stream.Close ();
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
            if (!Stream.DataAvailable)
                throw new NoRequestException ();

            // Read as much data as we can from the client into the buffer, up to the buffer size
            offset += Stream.Read (buffer, offset);

            // Try decoding the request
            int read = 0;
            try {
                bufferedRequest = Decode (buffer, index, offset, ref read);
            } catch (NoRequestException e) {
                index += read;
                throw e;
            }

            // Valid request received, reset the buffer
            index = 0;
            offset = 0;
        }
    }
}

