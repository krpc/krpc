using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    abstract class StreamStream : IStream<NoMessage,StreamUpdate>
    {
        protected StreamStream (IStream<byte,byte> stream)
        {
            Stream = stream;
        }

        protected IStream<byte,byte> Stream { get; private set; }

        public bool DataAvailable {
            get { throw new InvalidOperationException (); }
        }

        public NoMessage Read ()
        {
            throw new InvalidOperationException ();
        }

        public int Read (NoMessage[] buffer, int offset)
        {
            throw new InvalidOperationException ();
        }

        public int Read (NoMessage[] buffer, int offset, int size)
        {
            throw new InvalidOperationException ();
        }

        /// <summary>
        /// Write a stream message to the client.
        /// </summary>
        public abstract void Write (StreamUpdate value);

        [SuppressMessage ("Gendarme.Rules.Naming", "ParameterNamesShouldMatchOverriddenMethodRule")]
        public void Write (StreamUpdate[] buffer)
        {
            foreach (var value in buffer)
                Write (value);
        }

        public void Write (StreamUpdate[] buffer, int offset, int size)
        {
            for (int i = 0; i < size; i++)
                Write (buffer [i + offset]);
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
