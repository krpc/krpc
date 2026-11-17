namespace KRPC.Server
{
    /// <summary>
    /// A client's stream, of bytes or of messages.
    /// </summary>
    public interface IStream
    {
        /// <summary>
        /// Whether there is data to read.
        /// </summary>
        bool DataAvailable { get; }

        /// <summary>
        /// Close the stream and free its resources.
        /// </summary>
        void Close ();
    }

    /// <summary>
    /// A stream of bytes, for a transport to frame messages on.
    /// </summary>
    public interface IByteStream : IStream
    {
        /// <summary>
        /// Read up to <paramref name="size"/> bytes into the buffer, starting at
        /// <paramref name="offset"/>. Returns the number of bytes read.
        /// </summary>
        int Read (byte[] buffer, int offset, int size);

        /// <summary>
        /// Write the bytes in the buffer.
        /// </summary>
        void Write (byte[] buffer);

        /// <summary>
        /// Write <paramref name="size"/> bytes from the buffer, starting at
        /// <paramref name="offset"/>.
        /// </summary>
        void Write (byte[] buffer, int offset, int size);

        /// <summary>
        /// The total number of bytes read from the stream.
        /// </summary>
        ulong BytesRead { get; }

        /// <summary>
        /// The total number of bytes written to the stream.
        /// </summary>
        ulong BytesWritten { get; }

        /// <summary>
        /// Clear the bytes read and bytes written counts.
        /// </summary>
        void ClearStats ();
    }
}
