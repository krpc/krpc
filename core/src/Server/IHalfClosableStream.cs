namespace KRPC.Server
{
    /// <summary>
    /// A byte stream whose write side can be closed on its own.
    /// </summary>
    interface IHalfClosableStream
    {
        /// <summary>
        /// Close the write side of the stream, leaving the read side open.
        /// </summary>
        void HalfClose ();
    }
}
