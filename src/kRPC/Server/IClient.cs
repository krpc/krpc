using System;

namespace KRPC.Server
{
    interface IClient<In,Out> : IEquatable<IClient<In,Out>>
    {
        IStream<In,Out> Stream { get; }
        string Name { get; }
        string Address { get; }
        bool Connected { get; }
        void Close ();
    }
}
