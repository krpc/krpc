using System;

namespace KRPC.Server
{
    interface IStream
    {
        bool DataAvailable { get; }
        void Close ();
    }

    interface IStream<In,Out> : IStream
    {
        In Read();
        int Read (In[] buffer, int offset);
        int Read (In[] buffer, int offset, int size);
        void Write (Out value);
        void Write (Out[] buffer);
    }
}
