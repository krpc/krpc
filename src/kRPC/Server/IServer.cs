using System;
using System.Collections.Generic;

namespace KRPC.Server
{
    interface IServer
    {
        void Start ();
        void Stop ();
        void Update ();
        bool Running { get; }

        IEnumerable<IClient> Clients { get; }

        event EventHandler<ClientRequestingConnectionArgs> OnClientRequestingConnection;
        event EventHandler<ClientConnectedArgs> OnClientConnected;
        event EventHandler<ClientDisconnectedArgs> OnClientDisconnected;
    }

    interface IServer<In,Out>
    {
        void Start ();
        void Stop ();
        void Update ();
        bool Running { get; }

        IEnumerable<IClient<In,Out>> Clients { get; }

        event EventHandler<ClientRequestingConnectionArgs<In,Out>> OnClientRequestingConnection;
        event EventHandler<ClientConnectedArgs<In,Out>> OnClientConnected;
        event EventHandler<ClientDisconnectedArgs<In,Out>> OnClientDisconnected;
    }
}
