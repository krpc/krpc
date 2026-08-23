using System;
using System.Collections.Generic;
using System.IO;
using Google.Protobuf;
using KRPC.Schema.KRPC;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    /// <summary>
    /// Reads are made a block at a time, and a block has nothing to do with where a message
    /// starts or ends, so the reader has to take one out of whatever the stream hands over.
    /// Nothing here opens a socket, so it holds for every transport rather than for the one
    /// it happened to be run over.
    /// </summary>
    [TestFixture]
    public class MessageReaderTest
    {
        /// <summary>
        /// A stream that hands over a fixed script of reads, so that a message can be made to
        /// arrive in whatever pieces a test needs. An exhausted script reads as end of file,
        /// as a closed connection does.
        /// </summary>
        sealed class ScriptedStream : System.IO.Stream
        {
            readonly Queue<byte[]> chunks;

            public ScriptedStream (params byte[][] scriptedChunks)
            {
                chunks = new Queue<byte[]> (scriptedChunks);
            }

            public override int Read (byte[] buffer, int offset, int count)
            {
                if (chunks.Count == 0)
                    return 0;
                var chunk = chunks.Dequeue ();
                if (chunk.Length > count)
                    throw new ArgumentException ("scripted read is larger than the buffer");
                System.Buffer.BlockCopy (chunk, 0, buffer, offset, chunk.Length);
                return chunk.Length;
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException (); } }
            public override long Position {
                get { throw new NotSupportedException (); }
                set { throw new NotSupportedException (); }
            }
            public override void Flush () { }
            public override long Seek (long o, SeekOrigin origin) {
                throw new NotSupportedException ();
            }
            public override void SetLength (long value) { throw new NotSupportedException (); }
            public override void Write (byte[] buffer, int offset, int count) {
                throw new NotSupportedException ();
            }
        }

        /// <summary>
        /// A response carrying the given value, encoded with its size prefix.
        /// </summary>
        static byte[] Encoded (string value)
        {
            var response = new Response ();
            var result = new ProcedureResult ();
            result.Value = ByteString.CopyFromUtf8 (value);
            response.Results.Add (result);
            using (var stream = new MemoryStream ()) {
                var output = new CodedOutputStream (stream, true);
                output.WriteLength (response.CalculateSize ());
                response.WriteTo (output);
                output.Flush ();
                return stream.ToArray ();
            }
        }

        static string Receive (MessageReader reader)
        {
            Assert.IsTrue (reader.Read ());
            var response = Response.Parser.ParseFrom (
                               reader.Buffer, reader.Offset, reader.Size);
            return response.Results [0].Value.ToStringUtf8 ();
        }

        static byte[] Slice (byte[] data, int offset, int length)
        {
            var slice = new byte [length];
            System.Buffer.BlockCopy (data, offset, slice, 0, length);
            return slice;
        }

        [Test]
        public void WholeMessageInOneRead ()
        {
            var reader = new MessageReader (new ScriptedStream (Encoded ("foo")));
            Assert.AreEqual ("foo", Receive (reader));
        }

        [Test]
        public void MessageSplitAcrossReads ()
        {
            var data = Encoded ("foo");
            var reader = new MessageReader (new ScriptedStream (
                Slice (data, 0, 1), Slice (data, 1, 2), Slice (data, 3, data.Length - 3)));
            Assert.AreEqual ("foo", Receive (reader));
        }

        [Test]
        public void SizePrefixSplitAcrossReads ()
        {
            // A payload this size needs a two byte size prefix, so a read can end part way
            // through the prefix itself
            var value = new string ('x', 300);
            var data = Encoded (value);
            var reader = new MessageReader (new ScriptedStream (
                Slice (data, 0, 1), Slice (data, 1, data.Length - 1)));
            Assert.AreEqual (value, Receive (reader));
        }

        [Test]
        public void TwoMessagesInOneRead ()
        {
            var first = Encoded ("foo");
            var second = Encoded ("bar");
            var both = new byte [first.Length + second.Length];
            System.Buffer.BlockCopy (first, 0, both, 0, first.Length);
            System.Buffer.BlockCopy (second, 0, both, first.Length, second.Length);
            var reader = new MessageReader (new ScriptedStream (both));
            Assert.AreEqual ("foo", Receive (reader));
            Assert.AreEqual ("bar", Receive (reader));
        }

        [Test]
        public void MessageAndTheStartOfTheNextInOneRead ()
        {
            var first = Encoded ("foo");
            var second = Encoded ("bar");
            var head = new byte [first.Length + 2];
            System.Buffer.BlockCopy (first, 0, head, 0, first.Length);
            System.Buffer.BlockCopy (second, 0, head, first.Length, 2);
            var reader = new MessageReader (new ScriptedStream (
                head, Slice (second, 2, second.Length - 2)));
            Assert.AreEqual ("foo", Receive (reader));
            Assert.AreEqual ("bar", Receive (reader));
        }

        [Test]
        public void ConnectionClosedPartwayThroughAMessage ()
        {
            var data = Encoded ("foo");
            var reader = new MessageReader (new ScriptedStream (Slice (data, 0, 2)));
            Assert.Throws<EndOfStreamException> (() => reader.Read ());
        }

        [Test]
        public void MalformedSizePrefix ()
        {
            // A size is a uint32, so a varint that has not ended within five bytes is not one
            var reader = new MessageReader (new ScriptedStream (
                new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }));
            Assert.Throws<InvalidDataException> (() => reader.Read ());
        }
    }
}
