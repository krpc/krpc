using System;

namespace KRPC.Server
{
    interface IClientEventArgs
    {
        IClient Client { get; }
    }

    interface IClientEventArgs<In,Out>
    {
        IClient<In,Out> Client { get; }
    }
}

