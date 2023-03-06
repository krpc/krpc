using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Server
{
    /// <summary>
    /// A byte server that never has any clients.
    /// Used when a server instance is required, but the protocol does not require it.
    /// </summary>
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    sealed class NullServer : IServer<byte,byte>
    {
        #pragma warning disable 0067
        public event EventHandler OnStarted;
        public event EventHandler OnStopped;
        public event EventHandler<ClientRequestingConnectionEventArgs<byte,byte>> OnClientRequestingConnection;
        public event EventHandler<ClientConnectedEventArgs<byte,byte>> OnClientConnected;
        public event EventHandler<ClientActivityEventArgs<byte,byte>> OnClientActivity;
        public event EventHandler<ClientDisconnectedEventArgs<byte,byte>> OnClientDisconnected;
        #pragma warning restore 0067

        public NullServer (string address, string info)
        {
            Address = address;
            Info = info;
        }

        public void Start ()
        {
            Running = true;
        }

        public void Stop ()
        {
            Running = false;
        }

        public void Update ()
        {
        }

        public string Address { get; private set; }

        public string Info { get; private set; }

        public bool Running { get; private set; }

        public IEnumerable<IClient<byte,byte>> Clients {
            get { return new List<IClient<byte,byte>> (); }
        }

        public ulong BytesRead {
            get { return 0; }
        }

        public ulong BytesWritten {
            get { return 0; }
        }

        public void ClearStats ()
        {
        }
    }
}
