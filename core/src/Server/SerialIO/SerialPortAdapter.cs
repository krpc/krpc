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
            set { port.ReadTimeout = value; }
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
            return port.Read (buffer, offset, count);
        }

        public void Write (byte[] buffer, int offset, int count)
        {
            port.Write (buffer, offset, count);
        }
    }
}
