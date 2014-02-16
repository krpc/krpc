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
    }

    interface IServer<In,Out> : IServer
    {
        IEnumerable<IClient<In,Out>> Clients { get; }

        event EventHandler<ClientRequestingConnectionArgs<In,Out>> OnClientRequestingConnection;
        event EventHandler<ClientConnectedArgs<In,Out>> OnClientConnected;
        event EventHandler<ClientDisconnectedArgs<In,Out>> OnClientDisconnected;
    }
}
