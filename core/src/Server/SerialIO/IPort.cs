namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// The operations <see cref="BufferedPort"/> needs from the port it transfers data over.
    /// Reading and writing are allowed to wait for the hardware, and are called only from the
    /// threads that <see cref="BufferedPort"/> runs for that purpose.
    /// </summary>
    interface IPort
    {
        /// <summary>
        /// Name identifying the port, such as the path of its device.
        /// </summary>
        string PortName { get; }

        /// <summary>
        /// Whether the port is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// How long a read waits for data, in milliseconds, before it gives up and throws
        /// TimeoutException.
        /// </summary>
        int ReadTimeout { get; set; }

        /// <summary>
        /// Number of bytes the port has received and not yet handed over to a read.
        /// </summary>
        int BytesToRead { get; }

        /// <summary>
        /// Open the port.
        /// </summary>
        void Open ();

        /// <summary>
        /// Close the port.
        /// </summary>
        void Close ();

        /// <summary>
        /// Discard data that the port has received but not yet handed over.
        /// </summary>
        void DiscardInBuffer ();

        /// <summary>
        /// Read up to count bytes into buffer, starting at offset, waiting up to the read timeout
        /// for the first of them. Returns the number of bytes read, and throws TimeoutException if
        /// none arrive in time.
        /// </summary>
        int Read (byte[] buffer, int offset, int count);

        /// <summary>
        /// Write count bytes from buffer, starting at offset, waiting until the port has sent them.
        /// </summary>
        void Write (byte[] buffer, int offset, int count);
    }
}
