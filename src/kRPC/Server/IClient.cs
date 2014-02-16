using System;

namespace KRPC.Server
{
    interface IClient
    {
        string Name { get; }
        string Address { get; }
        bool Connected { get; }
        void Close ();
    }

    interface IClient<In,Out> : IEquatable<IClient<In,Out>>, IClient
    {
        IStream<In,Out> Stream { get; }
    }
}
