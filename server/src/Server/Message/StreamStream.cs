using System;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    abstract class StreamStream : IStream<NoMessage,StreamMessage>
    {
        protected StreamStream (IStream<byte,byte> stream)
        {
            Stream = stream;
        }

        protected IStream<byte,byte> Stream { get; private set; }

        public bool DataAvailable {
            get { throw new NotImplementedException (); }
        }

        public NoMessage Read ()
        {
            throw new NotImplementedException ();
        }

        public int Read (NoMessage[] buffer, int offset)
        {
            throw new NotImplementedException ();
        }

        public int Read (NoMessage[] buffer, int offset, int size)
        {
            throw new NotImplementedException ();
        }

        /// <summary>
        /// Write a stream message to the client.
        /// </summary>
        public abstract void Write (StreamMessage value);

        public void Write (StreamMessage[] value)
        {
            throw new NotImplementedException ();
        }

        public void Write (StreamMessage[] value, int offset, int size)
        {
            throw new NotImplementedException ();
        }

        public ulong BytesRead {
            get { return Stream.BytesRead; }
        }

        public ulong BytesWritten {
            get { return Stream.BytesWritten; }
        }

        public void ClearStats ()
        {
            Stream.ClearStats ();
        }

        public void Close ()
        {
            Stream.Close ();
        }
    }
}

