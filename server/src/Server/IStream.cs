using System.Diagnostics.CodeAnalysis;

namespace KRPC.Server
{
    /// <summary>
    /// A non-generic stream.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public interface IStream
    {
        /// <summary>
        /// Returns true if the stream contains data to read.
        /// </summary>
        bool DataAvailable { get; }

        /// <summary>
        /// Close the stream and free its resources.
        /// </summary>
        void Close ();

        /// <summary>
        /// Gets the total number of bytes read from the stream.
        /// </summary>
        ulong BytesRead { get; }

        /// <summary>
        /// Gets the total number of bytes written to the stream.
        /// </summary>
        ulong BytesWritten { get; }

        /// <summary>
        /// Clear the bytes read and bytes written counts.
        /// </summary>
        void ClearStats ();
    }

    /// <summary>
    /// A generic stream, from which values of type In can be read and values of type Out can be written.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    public interface IStream<TIn,TOut> : IStream
    {
        /// <summary>
        /// Read a single value from the stream.
        /// </summary>
        TIn Read ();

        /// <summary>
        /// Read multiple values from the stream, into buffer starting at offset
        /// and up to the end of the buffer.
        /// </summary>
        int Read (TIn[] buffer, int offset);

        /// <summary>
        /// Read multiple values from the stream, into buffer starting at offset
        /// and up to the end of the buffer or size items, whichever comes first.
        /// </summary>
        int Read (TIn[] buffer, int offset, int size);

        /// <summary>
        /// Write a value to the stream.
        /// </summary>
        void Write (TOut value);

        /// <summary>
        /// Write multiple values to the stream.
        /// </summary>
        void Write (TOut[] buffer);

        /// <summary>
        /// Write size values to the stream, from the buffer starting at offset.
        /// </summary>
        void Write (TOut[] buffer, int offset, int size);
    }
}
