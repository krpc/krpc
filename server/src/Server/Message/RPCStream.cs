using System;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    abstract class RPCStream : IStream<Request,Response>
    {
        internal const int MAX_BUFFER_SIZE = 1 * 1024 * 1024;
        Request bufferedRequest;
        byte[] buffer = new byte[MAX_BUFFER_SIZE];
        int size;

        protected RPCStream (IStream<byte,byte> stream)
        {
            Stream = stream;
        }

        protected IStream<byte,byte> Stream { get; private set; }

        /// <summary>
        /// Read a request. Implementors shoulld try to decode <paramref name="length"/> bytes
        /// from <paramref name="data"/> starting at <paramref name="offset"/>.
        /// If a request is successfully decoded, write it to <paramref name="request"/> and return
        /// the number of bytes read. If no message, or a partial message, is found, don't set
        /// <paramref name="request"/>. The read will be retried when more data has arrived.
        /// When <paramref name="request"/> is left unset, a non-zero number of bytes read can be returned.
        /// This allows non-message bytes to be consumed, for example for control traffic not
        /// related to the RPC server. Implementors should throw a MalformedRequestException if malformed
        /// data is received.
        /// </summary>
        protected abstract int Read (ref Request request, byte[] data, int offset, int length);

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
        /// Read a request from the client.
        /// Throws NoRequestException if there is no available request.
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

            if (size == 0 && !Stream.DataAvailable)
                throw new NoRequestException ();

            // Read as much data as we can from the client
            size += Stream.Read (buffer, size);

            // Try decoding a request
            int read = Read (ref bufferedRequest, buffer, 0, size);

            // Update the buffer
            // Note: copying should happen rarely as Read will usually consume the whole buffer
            size -= read;
            if (read > 0 && size > 0)
                Array.Copy (buffer, read, buffer, 0, size);

            // If no message was decoded and we have filled the buffer then fail
            if (bufferedRequest == null && size == MAX_BUFFER_SIZE)
                throw new RequestBufferOverflowException ();

            // No request decoded
            if (bufferedRequest == null)
                throw new NoRequestException ();
        }
    }
}

