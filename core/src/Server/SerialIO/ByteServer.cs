using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using KRPC.IO.Ports;
using KRPC.Utils;

namespace KRPC.Server.SerialIO
{
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLargeClassesRule")]
    sealed class ByteServer : IServer<byte,byte>
    {
        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionEventArgs<byte,byte>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedEventArgs<byte,byte>> OnClientConnected;
        #pragma warning disable 0067
        public event EventHandler<ClientActivityEventArgs<byte,byte>> OnClientActivity;
        #pragma warning restore 0067
        public event EventHandler<ClientDisconnectedEventArgs<byte,byte>> OnClientDisconnected;

        SerialPort port;
        ByteClient client;
        ByteClient pendingClient;
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

        [SuppressMessage ("Gendarme.Rules.Exceptions", "DoNotSwallowErrorsCatchingNonSpecificExceptionsRule")]
        public void Start ()
        {
            if (OnClientRequestingConnection == null)
                throw new ServerException ("Client request handler not set");
            if (Running) {
                Logger.WriteLine ("SerialIO.Server: start requested, but server is already running", Logger.Severity.Warning);
                return;
            }
            Logger.WriteLine ("SerialIO.Server: starting " + Address, Logger.Severity.Debug);
            port = new SerialPort (Address, (int)BaudRate, Parity, DataBits, StopBits);
            Logger.WriteLine ("SerialIO.Server: port name = " + Address, Logger.Severity.Debug);
            Logger.WriteLine ("SerialIO.Server: baud rate = " + port.BaudRate, Logger.Severity.Debug);
            Logger.WriteLine ("SerialIO.Server: data bits = " + port.DataBits, Logger.Severity.Debug);
            Logger.WriteLine ("SerialIO.Server: parity = " + port.Parity, Logger.Severity.Debug);
            Logger.WriteLine ("SerialIO.Server: stop bits = " + port.StopBits, Logger.Severity.Debug);
            try {
                port.Open();
            } catch (Exception exn) {
                Close ();
                Logger.WriteLine("SerialIO.Server: failed to start server; " + exn, Logger.Severity.Error);
                throw new ServerException(exn.GetType() + ": " + exn.Message, exn);
            }
            // Discard stale data from the port
            port.DiscardInBuffer();
            Logger.WriteLine ("SerialIO.Server: started successfully", Logger.Severity.Debug);
            EventHandlerExtensions.Invoke (OnStarted, this);
        }

        public void Stop ()
        {
            if (!Running)
                return;
            Logger.WriteLine ("SerialIO.Server: stop requested", Logger.Severity.Debug);
            Close ();
            Logger.WriteLine ("SerialIO.Server: stopped successfully", Logger.Severity.Debug);
            EventHandlerExtensions.Invoke (OnStopped, this);
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
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
        /// Called by RPCStream.Read when a client sends a connection request message,
        /// but another client is already connected.
        /// </summary>
        internal void ClientConnectionRequest (byte[] data)
        {
            if (client != null) {
                DisconnectClient (client);
                client = null;
            }
            if (pendingClient != null) {
                DisconnectClient (pendingClient, true);
                pendingClient = null;
            }
            Logger.WriteLine (
                "SerialIO.Server[" + Address + "]: " +
                "client requesting connection (overriding previous client connection)",
                Logger.Severity.Debug);
            pendingClient = new ByteClient (port, data);
        }

        public void Update ()
        {
            try {
                if (client == null && pendingClient == null && port.IsOpen && port.BytesToRead > 0) {
                    Logger.WriteLine (
                        "SerialIO.Server[" + Address + "]: client requesting connection",
                        Logger.Severity.Debug);
                    pendingClient = new ByteClient (port);
                }
            } catch (IOException) {
                Stop ();
            } catch (TimeoutException) {
                Stop ();
            } catch (ObjectDisposedException) {
                Stop ();
            }

            if (client == null && pendingClient != null) {
                // Trigger OnClientRequestingConnection events to verify the connection
                var args = new ClientRequestingConnectionEventArgs<byte,byte> (pendingClient);
                EventHandlerExtensions.Invoke (OnClientRequestingConnection, this, args);

                // Deny the connection
                if (args.Request.ShouldDeny) {
                    Logger.WriteLine (
                        "SerialIO.Server[" + Address + "]: client connection denied",
                        Logger.Severity.Debug);
                    DisconnectClient (pendingClient, true);
                    pendingClient = null;
                }

                // Allow the connection
                else if (args.Request.ShouldAllow) {
                    client = pendingClient;
                    pendingClient = null;
                    Logger.WriteLine (
                        "SerialIO.Server[" + Address + "]: " +
                        "client connection accepted", Logger.Severity.Debug);
                    EventHandlerExtensions.Invoke (OnClientConnected, this, new ClientConnectedEventArgs<byte,byte> (client));
                }

                // Still pending, will either be denied or allowed on a subsequent called to Update
                else {
                    Logger.WriteLine (
                        "SerialIO.Server[" + Address + "]: " +
                        "client connection still pending", Logger.Severity.Debug);
                }
            } else if (client != null && !client.Connected) {
                DisconnectClient (client);
                client = null;
            }
        }

        public string Address { get; private set; }

        public uint BaudRate { get; private set; }

        public ushort DataBits { get; private set; }

        public Parity Parity { get; private set; }

        public StopBits StopBits { get; private set; }

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

        public bool Running {
            get { return port != null; }
        }

        public IEnumerable<IClient<byte,byte>> Clients {
            get {
                if (client != null)
                    yield return client;
            }
        }

        public ulong BytesRead {
            get {
                ulong read = closedClientsBytesRead;
                if (client != null)
                    read += client.Stream.BytesRead;
                return read;
            }
        }

        public ulong BytesWritten {
            get {
                ulong written = closedClientsBytesWritten;
                if (client != null)
                    written += client.Stream.BytesWritten;
                return written;
            }
        }

        public void ClearStats ()
        {
            closedClientsBytesRead = 0;
            closedClientsBytesWritten = 0;
            if (client != null)
                client.Stream.ClearStats ();
        }

        void DisconnectClient (IClient<byte, byte> _client, bool noEvent = false)
        {
            var clientAddress = _client.Address;
            try {
                var stream = _client.Stream;
                closedClientsBytesRead += stream.BytesRead;
                closedClientsBytesWritten += stream.BytesWritten;
            } catch (ClientDisconnectedException) {
            }
            _client.Close ();
            if (!noEvent)
                EventHandlerExtensions.Invoke (OnClientDisconnected, this, new ClientDisconnectedEventArgs<byte, byte> (_client));
            Logger.WriteLine ("SerialIO.Server: client disconnected (" + clientAddress + ")");
        }
    }
}
