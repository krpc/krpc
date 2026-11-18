using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using KRPC.Server;
using NUnit.Framework;

namespace KRPC.Test.Server
{
    [TestFixture]
    public class SocketStreamTest
    {
        TcpListener listener;
        TcpClient peer;
        SocketClient client;

        [SetUp]
        public void SetUp ()
        {
            listener = new TcpListener (IPAddress.Loopback, 0);
            listener.Start ();
            peer = new TcpClient ();
            peer.Connect (IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
            client = new SocketClient (listener.AcceptSocket (), string.Empty);
        }

        [TearDown]
        public void TearDown ()
        {
            client.Close ();
            peer.Close ();
            listener.Stop ();
        }

        SocketStream Stream {
            get { return (SocketStream)client.Stream; }
        }

        static void WaitFor (Func<bool> condition)
        {
            var timer = Stopwatch.StartNew ();
            while (!condition () && timer.ElapsedMilliseconds < 5000)
                Thread.Sleep (1);
            Assert.IsTrue (condition ());
        }

        [Test]
        public void ReadReturnsNothingWithoutBlocking ()
        {
            var buffer = new byte [16];
            Assert.IsFalse (Stream.DataAvailable);
            Assert.AreEqual (0, Stream.Read (buffer, 0, buffer.Length));
        }

        [Test]
        public void ArrivedBytesAreQueuedAndSignaled ()
        {
            var stream = Stream;
            Arrivals.Reset ();
            peer.GetStream ().Write (new byte[] { 1, 2, 3 }, 0, 3);
            Assert.IsTrue (Arrivals.WaitUntil (Stopwatch.GetTimestamp () + Stopwatch.Frequency * 5));
            WaitFor (() => stream.BytesRead == 3);
            var buffer = new byte [2];
            Assert.AreEqual (2, stream.Read (buffer, 0, 2));
            CollectionAssert.AreEqual (new byte[] { 1, 2 }, buffer);
            Assert.AreEqual (1, stream.Read (buffer, 0, 2));
            Assert.AreEqual (3, buffer [0]);
            Assert.IsFalse (stream.DataAvailable);
        }

        [Test]
        public void ConnectedUntilTheBytesSentBeforeAPeerCloseAreRead ()
        {
            var stream = Stream;
            peer.GetStream ().Write (new byte[] { 1, 2, 3 }, 0, 3);
            peer.Close ();
            WaitFor (() => stream.BytesRead == 3);
            Thread.Sleep (50);
            Assert.IsTrue (client.Connected);
            var buffer = new byte [3];
            Assert.AreEqual (3, stream.Read (buffer, 0, 3));
            WaitFor (() => !client.Connected);
        }

        [Test]
        public void WritesAreSentInOrderBeforeAHalfClose ()
        {
            var stream = Stream;
            for (byte i = 0; i < 100; i++)
                stream.Write (new [] { i });
            stream.HalfClose ();
            var received = new byte [101];
            var total = 0;
            var peerStream = peer.GetStream ();
            int read;
            while ((read = peerStream.Read (received, total, received.Length - total)) > 0)
                total += read;
            Assert.AreEqual (100, total);
            for (byte i = 0; i < 100; i++)
                Assert.AreEqual (i, received [i]);
            Assert.Throws<ClientDisconnectedException> (() => stream.Write (new byte[] { 0 }));
        }

        [Test]
        public void WriteFailsWhenThePeerStopsReading ()
        {
            var stream = Stream;
            var chunk = new byte [1024 * 1024];
            var timer = Stopwatch.StartNew ();
            long written = 0;
            Assert.Throws<ServerException> (() => {
                while (written < 1024L * 1024 * 1024) {
                    stream.Write (chunk);
                    written += chunk.Length;
                }
            });
            // The socket buffers hold some of it, and the queue the rest
            Assert.Greater (written, SocketStream.MaxUnsentBytes - chunk.Length);
            Assert.Less (timer.ElapsedMilliseconds, 5000);
        }

        [Test]
        public void ReadingPausesWhileTooManyBytesAreUnread ()
        {
            var stream = Stream;
            const int total = 32 * 1024 * 1024;
            var sender = new Thread (() => {
                var data = new byte [64 * 1024];
                try {
                    for (int sent = 0; sent < total; sent += data.Length)
                        peer.GetStream ().Write (data, 0, data.Length);
                } catch (System.IO.IOException) {
                }
            });
            sender.IsBackground = true;
            sender.Start ();
            // The reader stops near the limit, and the socket buffers and the peer's send
            // hold the rest
            WaitFor (() => stream.BytesRead >= (ulong)SocketStream.MaxUnreadBytes);
            Thread.Sleep (200);
            Assert.That (stream.BytesRead, Is.LessThan ((ulong)(SocketStream.MaxUnreadBytes + 64 * 1024)));
            Assert.IsTrue (sender.IsAlive);
            Assert.IsTrue (client.Connected);
            // Reading resumes as the bytes are read
            var buffer = new byte [64 * 1024];
            var read = 0;
            var timer = Stopwatch.StartNew ();
            while (read < total && timer.ElapsedMilliseconds < 10000)
                read += stream.Read (buffer, 0, buffer.Length);
            Assert.AreEqual (total, read);
            Assert.IsTrue (sender.Join (5000));
        }

        [Test]
        public void CloseEndsTheStream ()
        {
            var stream = Stream;
            client.Close ();
            Assert.IsFalse (stream.Open);
            Assert.IsFalse (client.Connected);
            Assert.Throws<ClientDisconnectedException> (() => stream.Write (new byte[] { 0 }));
        }
    }
}
