using System;
#if NET
using System.IO.Ports;
#else
using KRPC.IO.Ports;
#endif

namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// Presents a serial port as an <see cref="IPort"/>.
    /// </summary>
    sealed class SerialPortAdapter : IPort
    {
        readonly SerialPort port;

        public SerialPortAdapter (SerialPort serialPort)
        {
            if (serialPort == null)
                throw new ArgumentNullException (nameof (serialPort));
            port = serialPort;
        }

        public string PortName {
            get { return port.PortName; }
        }

        public bool IsOpen {
            get { return port.IsOpen; }
        }

        public int ReadTimeout {
            get { return port.ReadTimeout; }
            set {
#if NET
                // A read on .NET's Unix serial stream is a request its I/O thread completes,
                // and a read that times out cancels the request, which may drop bytes that
                // arrive as it gives up. So a read there waits with no timeout, until closing
                // the port ends it.
                if (!OperatingSystem.IsWindows ()) {
                    port.ReadTimeout = SerialPort.InfiniteTimeout;
                    return;
                }
#endif
                port.ReadTimeout = value;
            }
        }

        public int BytesToRead {
            get { return port.BytesToRead; }
        }

        public void Open ()
        {
            port.Open ();
        }

        public void Close ()
        {
            port.Close ();
        }

        public void DiscardInBuffer ()
        {
            port.DiscardInBuffer ();
        }

        public int Read (byte[] buffer, int offset, int count)
        {
            // A read returns once any bytes have arrived. One that times out takes nothing: on
            // Mono it polls the port before reading, and on Windows the read completes empty
            return port.Read (buffer, offset, count);
        }

        public void Write (byte[] buffer, int offset, int count)
        {
            port.Write (buffer, offset, count);
        }
    }
}
