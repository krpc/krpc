using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    abstract class RPCStream : IStream<Request,Response>
    {
        Request bufferedRequest;
        const int BUFFER_INITIAL_SIZE = 4 * 1024 * 1024;
        const int BUFFER_INCREASE_SIZE = 1 * 1024 * 1024;
        byte[] receiveBuffer = new byte [BUFFER_INITIAL_SIZE];
        int receiveBufferSize;

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
        /// data is received. Implementors should throw a ClientDisconnectedException if they would
        /// like the client to be disconnected.
        /// </summary>
        protected abstract int Read (ref Request request, byte[] data, int offset, int length);

        /// <summary>
        /// Returns true if there is a request waiting to be read. A Call to Read() will
        /// not throw NoRequestException if this returns true. Throws MalformedRequestException
        /// if a malformed request is received.
        /// </summary>
        public bool DataAvailable {
            get { return Poll (); }
        }

        /// <summary>
        /// Read a request from the client.
        /// Throws NoRequestException if there is no available request.
        /// Throws MalformedRequestException if malformed data is received.
        /// </summary>
        public Request Read ()
        {
            if (!Poll ())
                throw new NoRequestException ();
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

        [SuppressMessage ("Gendarme.Rules.Naming", "ParameterNamesShouldMatchOverriddenMethodRule")]
        public void Write (Response[] buffer)
        {
            throw new NotSupportedException ();
        }

        public void Write (Response[] buffer, int offset, int size)
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
            receiveBuffer = null;
            bufferedRequest = null;
            Stream.Close ();
        }

        /// Returns true if there is a buffered message.
        /// Otherwise attempts to receive a new message.
        /// Returns false if no message could be received.
        /// Closes the stream and throws MalformedRequestException if a malformed message is received.
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        bool Poll ()
        {
            // Check if the stream is closed
            if (receiveBuffer == null)
                return false;

            // If there is already a request, don't need to poll for another one
            if (bufferedRequest != null)
                return true;

            // No data is available, so there is no request to receive
            if (receiveBufferSize == 0 && !Stream.DataAvailable)
                return false;

            // Read as much data as we can from the client
            while (Stream.DataAvailable) {
                // Increase the size of the buffer if the remaining space is low
                if (receiveBuffer.Length - receiveBufferSize < BUFFER_INCREASE_SIZE) {
                    var newBuffer = new byte [receiveBuffer.Length + BUFFER_INCREASE_SIZE];
                    Array.Copy (receiveBuffer, newBuffer, receiveBufferSize);
                    receiveBuffer = newBuffer;
                }
                receiveBufferSize += Stream.Read (receiveBuffer, receiveBufferSize);
            }

            // Try decoding a request
            int read;
            try {
                read = Read (ref bufferedRequest, receiveBuffer, 0, receiveBufferSize);
            } catch (MalformedRequestException) {
                Close ();
                throw;
            }

            // Sanity check the bytes read by the class implementing Read()
            if (read > receiveBufferSize) {
                Close ();
                throw new InvalidOperationException ("Read too many bytes");
            }

            // Update the buffer
            // Note: shuffling the buffer by copying should happen rarely as Read() usually consumes the entire buffer
            if (read == receiveBufferSize)
                receiveBufferSize = 0;
            else if (read > 0 && receiveBufferSize > 0) {
                Array.Copy (receiveBuffer, read, receiveBuffer, 0, receiveBufferSize - read);
                receiveBufferSize -= read;
            }

            // Return whether a request was decoded and is available
            return (bufferedRequest != null);
        }
    }
}
