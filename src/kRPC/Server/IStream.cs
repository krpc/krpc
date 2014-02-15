using System;

namespace KRPC.Server
{
    interface IStream<In,Out>
    {
        bool DataAvailable { get; }
        In Read();
        int Read (In[] buffer, int offset);
        int Read (In[] buffer, int offset, int size);
        void Write (Out value);
        void Write (Out[] buffer);
        void Close ();
    }
}
