using System;

namespace KRPC.Server
{
    interface IClientEventArgs<In,Out>
    {
        IClient<In,Out> Client { get; }
    }
}

