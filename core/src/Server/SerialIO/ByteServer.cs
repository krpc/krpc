using System;
using System.Collections.Generic;
using System.IO;
#if NET
using System.IO.Ports;
#else
using KRPC.IO.Ports;
#endif
using KRPC.Utils;

namespace KRPC.Server.SerialIO
{
    /// <summary>
    /// Byte server implementation over serial I/O.
    /// </summary>
    public sealed class ByteServer : IServer<IByteStream>
    {
        /// <summary>
        /// Event handler for when the server starts.
        /// </summary>
        public event EventHandler OnStarted;

        /// <summary>
        /// Event handler for when the server stops.
        /// </summary>
        public event EventHandler OnStopped;

        /// <summary>
        /// Event handler for when a new client has connected.
        /// </summary>
        public event EventHandler<ClientConnectedEventArgs<IByteStream>> OnClientConnected;

        /// <summary>
        /// Event handler when a client disconnects.
        /// </summary>
        public event EventHandler<ClientDisconnectedEventArgs<IByteStream>> OnClientDisconnected;

        BufferedPort port;
        ByteClient client;
        ulong closedClientsBytesRead;
        ulong closedClientsBytesWritten;

        /// <summary>
        /// Create a SerialIO server. After Start() is called, the server
        /// will listen for connection requests on the port.
        /// </summary>
        public ByteServer (string address, uint baudRate, ushort dataBits, Parity parity, StopBits stopBits)
        {
            Address = address;
            BaudRate = baudRate;
            if (dataBits < 5 || dataBits > 8)
                throw new ArgumentException ("Data bits must be 5, 6, 7 or 8", nameof (dataBits));
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
        }

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start ()
        {
            if (Running) {
                Logger.WriteLine ("SerialIO.Server: start requested, but server is already running", Logger.Severity.Warning);
                return;
            }
            Logger.WriteLine ("SerialIO.Server: starting " + Address, Logger.Severity.Debug);
            var serialPort = new SerialPort (Address, (int)BaudRate, Parity, DataBits, StopBits);
            Logger.WriteLine ("SerialIO.Server: port name = " + Address, Logger.Severity.Debug);
            Logger.WriteLine ("SerialIO.Server: baud rate = " + serialPort.BaudRate, Logger.Severity.Debug);
            Logger.WriteLine ("SerialIO.Server: data bits = " + serialPort.DataBits, Logger.Severity.Debug);
            Logger.WriteLine ("SerialIO.Server: parity = " + serialPort.Parity, Logger.Severity.Debug);
            Logger.WriteLine ("SerialIO.Server: stop bits = " + serialPort.StopBits, Logger.Severity.Debug);
            port = new BufferedPort (new SerialPortAdapter (serialPort));
            try {
                port.Open();
            } catch (Exception exn) {
                Close ();
                Logger.WriteLine("SerialIO.Server: failed to start server; " + exn, Logger.Severity.Error);
                throw new ServerException(exn.GetType() + ": " + exn.Message, exn);
            }
            Logger.WriteLine ("SerialIO.Server: started successfully", Logger.Severity.Debug);
            EventHandlerExtensions.Invoke (OnStarted, this);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop ()
        {
            if (!Running)
                return;
            Logger.WriteLine ("SerialIO.Server: stop requested", Logger.Severity.Debug);
            Close ();
            Logger.WriteLine ("SerialIO.Server: stopped successfully", Logger.Severity.Debug);
            EventHandlerExtensions.Invoke (OnStopped, this);
        }

        void Close ()
        {
            if (client != null) {
                DisconnectClient (client);
                client = null;
            }
            if (port != null) {
                port.Close ();
                port = null;
            }
        }

        /// <summary>
        /// Update the server.
        /// </summary>
        public void Update ()
        {
            // The server is updated whether or not it is running, and has no port when stopped.
            if (port == null)
                return;
            try {
                // The port stops being open once transferring data over it has failed, which the
                // server cannot recover from and so stops.
                if (!port.IsOpen) {
                    Stop ();
                    return;
                }
                if (client == null && port.BytesAvailable > 0) {
                    Logger.WriteLine (
                        "SerialIO.Server[" + Address + "]: client connected",
                        Logger.Severity.Debug);
                    client = new ByteClient (port);
                    EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<IByteStream> (client));
                    return;
                }
            } catch (IOException) {
                Stop ();
                return;
            } catch (TimeoutException) {
                Stop ();
                return;
            } catch (ObjectDisposedException) {
                Stop ();
                return;
            }

            if (client != null && !client.Connected) {
                DisconnectClient (client);
                client = null;
            }
        }

        /// <summary>
        /// Address the server is listening on. The path of the serial port device.
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// Baud rate.
        /// </summary>
        public uint BaudRate { get; private set; }

        /// <summary>
        /// Number of data bits.
        /// </summary>
        public ushort DataBits { get; private set; }

        /// <summary>
        /// Number of parity bits.
        /// </summary>
        public Parity Parity { get; private set; }

        /// <summary>
        /// Number of stop bits.
        /// </summary>
        public StopBits StopBits { get; private set; }

        /// <summary>
        /// Information about the server.
        /// </summary>
        public string Info {
            get {
                var newline = Environment.NewLine;
                return
                    "Baud rate = " + BaudRate + newline +
                    "Data bits = " + DataBits + newline +
                    "Parity = " + Parity + newline +
                    "Stop bits = " + StopBits;
            }
        }

        /// <summary>
        /// Whether the server is running.
        /// </summary>
        public bool Running {
            get { return port != null; }
        }

        /// <summary>
        /// Clients conneted to the server.
        /// </summary>
        public IEnumerable<IClient<IByteStream>> Clients {
            get {
                if (client != null)
                    yield return client;
            }
        }

        /// <summary>
        /// Number of bytes received from clients.
        /// </summary>
        public ulong BytesRead {
            get {
                ulong read = closedClientsBytesRead;
                if (client != null)
                    read += client.Stream.BytesRead;
                return read;
            }
        }

        /// <summary>
        /// Number of bytes sent to clients.
        /// </summary>
        public ulong BytesWritten {
            get {
                ulong written = closedClientsBytesWritten;
                if (client != null)
                    written += client.Stream.BytesWritten;
                return written;
            }
        }

        /// <summary>
        /// Clear statistics.
        /// </summary>
        public void ClearStats ()
        {
            closedClientsBytesRead = 0;
            closedClientsBytesWritten = 0;
            if (client != null)
                client.Stream.ClearStats ();
        }

        void DisconnectClient (IClient<IByteStream> _client, bool noEvent = false)
        {
            var label = _client.Label ();
            var stream = _client.Stream;
            closedClientsBytesRead += stream.BytesRead;
            closedClientsBytesWritten += stream.BytesWritten;
            _client.Close ();
            if (!noEvent)
                EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<IByteStream> (_client));
            Logger.WriteLine ("SerialIO.Server: connection from " + label + " closed", Logger.Severity.Debug);
        }
    }
}
