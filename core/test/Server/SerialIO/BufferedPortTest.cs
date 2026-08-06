using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using KRPC.Server.SerialIO;
using NUnit.Framework;

namespace KRPC.Test.Server.SerialIO
{
    /// <summary>
    /// A port that stands in for serial hardware. Reads return data that the test has queued, and
    /// writes take as long as the test asks them to, standing for the time a real port spends
    /// sending a byte at a time at the configured baud rate.
    /// </summary>
    sealed class TestPort : IPort
    {
        readonly object dataLock = new object ();
        readonly Queue<byte[]> toRead = new Queue<byte[]> ();
        readonly List<byte> written = new List<byte> ();

        public string PortName {
            get { return "TestPort"; }
        }

        public bool IsOpen { get; private set; }

        public int ReadTimeout { get; set; }

        public int BytesToRead {
            get {
                lock (dataLock) {
                    var total = 0;
                    foreach (var data in toRead)
                        total += data.Length;
                    return total;
                }
            }
        }

        /// <summary>
        /// How long each write takes.
        /// </summary>
        public int WriteDelay { get; set; }

        /// <summary>
        /// Set to fail the next read with this exception.
        /// </summary>
        public Exception ReadFailure { get; set; }

        /// <summary>
        /// Set to fail the next write with this exception.
        /// </summary>
        public Exception WriteFailure { get; set; }

        public int Reads { get; private set; }

        public int Writes { get; private set; }

        public void Open ()
        {
            IsOpen = true;
        }

        public void Close ()
        {
            IsOpen = false;
        }

        public void DiscardInBuffer ()
        {
            lock (dataLock)
                toRead.Clear ();
        }

        /// <summary>
        /// Queue data for the port to hand over on a subsequent read.
        /// </summary>
        public void QueueRead (byte[] data)
        {
            lock (dataLock) {
                toRead.Enqueue (data);
                Monitor.PulseAll (dataLock);
            }
        }

        /// <summary>
        /// Everything that has been written to the port so far.
        /// </summary>
        public byte[] Written {
            get {
                lock (dataLock)
                    return written.ToArray ();
            }
        }

        public int Read (byte[] buffer, int offset, int count)
        {
            lock (dataLock) {
                if (ReadFailure != null) {
                    var failure = ReadFailure;
                    ReadFailure = null;
                    throw failure;
                }
                if (toRead.Count == 0) {
                    // Stand in for the port waiting for data that does not arrive
                    Monitor.Wait (dataLock, Math.Max (1, ReadTimeout));
                    if (toRead.Count == 0)
                        throw new TimeoutException ();
                }
                var data = toRead.Dequeue ();
                var size = Math.Min (count, data.Length);
                Array.Copy (data, 0, buffer, offset, size);
                Reads++;
                return size;
            }
        }

        public void Write (byte[] buffer, int offset, int count)
        {
            if (WriteDelay > 0)
                Thread.Sleep (WriteDelay);
            lock (dataLock) {
                if (WriteFailure != null) {
                    var failure = WriteFailure;
                    WriteFailure = null;
                    throw failure;
                }
                for (int i = 0; i < count; i++)
                    written.Add (buffer [offset + i]);
                Writes++;
            }
        }
    }

    [TestFixture]
    public class BufferedPortTest
    {
        const int timeout = 5000;

        static void WaitFor (Func<bool> condition)
        {
            var timer = Stopwatch.StartNew ();
            while (!condition ()) {
                if (timer.ElapsedMilliseconds > timeout)
                    Assert.Fail ("Timed out waiting for the port");
                Thread.Sleep (1);
            }
        }

        [Test]
        public void NotOpen ()
        {
            var port = new BufferedPort (new TestPort ());
            Assert.AreEqual ("TestPort", port.PortName);
            Assert.IsFalse (port.IsOpen);
        }

        [Test]
        public void OpenAndClose ()
        {
            var testPort = new TestPort ();
            var port = new BufferedPort (testPort);
            port.Open ();
            Assert.IsTrue (port.IsOpen);
            port.Close ();
            Assert.IsFalse (port.IsOpen);
            Assert.IsFalse (testPort.IsOpen);
        }

        [Test]
        public void ReceivedDataIsBuffered ()
        {
            var testPort = new TestPort ();
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                testPort.QueueRead (new byte[] { 1, 2, 3 });
                WaitFor (() => port.BytesAvailable == 3);
                var buffer = new byte [8];
                Assert.AreEqual (3, port.Read (buffer, 0, buffer.Length));
                Assert.AreEqual (new byte[] { 1, 2, 3, 0, 0, 0, 0, 0 }, buffer);
                Assert.AreEqual (0, port.BytesAvailable);
            } finally {
                port.Close ();
            }
        }

        /// <summary>
        /// Whatever the port received before the server opened it is left over from something
        /// else, and must not be taken for the start of a client's first message.
        /// </summary>
        [Test]
        public void OpeningDiscardsStaleData ()
        {
            var testPort = new TestPort ();
            testPort.QueueRead (new byte[] { 1, 2, 3 });
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                Thread.Sleep (50);
                Assert.AreEqual (0, port.BytesAvailable);
            } finally {
                port.Close ();
            }
        }

        [Test]
        public void ReadWithNothingReceived ()
        {
            var testPort = new TestPort ();
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                Assert.AreEqual (0, port.BytesAvailable);
                Assert.AreEqual (0, port.Read (new byte [8], 0, 8));
            } finally {
                port.Close ();
            }
        }

        /// <summary>
        /// The reason the port is buffered at all: the caller runs the game loop, so a write must
        /// return to it long before the port has finished sending the data.
        /// </summary>
        [Test]
        public void WriteDoesNotWaitForThePort ()
        {
            var testPort = new TestPort ();
            testPort.WriteDelay = 500;
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                var timer = Stopwatch.StartNew ();
                for (int i = 0; i < 4; i++)
                    port.Write (new byte[] { (byte)i }, 0, 1);
                var elapsed = timer.ElapsedMilliseconds;
                Assert.Less (elapsed, 250, "Writing waited for the port to send the data");
                WaitFor (() => testPort.Written.Length == 4);
            } finally {
                port.Close ();
            }
        }

        [Test]
        public void WrittenDataArrivesInOrder ()
        {
            var testPort = new TestPort ();
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                for (int i = 0; i < 64; i++)
                    port.Write (new byte[] { (byte)i }, 0, 1);
                WaitFor (() => testPort.Written.Length == 64);
                var written = testPort.Written;
                for (int i = 0; i < 64; i++)
                    Assert.AreEqual ((byte)i, written [i]);
            } finally {
                port.Close ();
            }
        }

        [Test]
        public void WritePartOfABuffer ()
        {
            var testPort = new TestPort ();
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                port.Write (new byte[] { 1, 2, 3, 4, 5 }, 1, 3);
                WaitFor (() => testPort.Written.Length == 3);
                Assert.AreEqual (new byte[] { 2, 3, 4 }, testPort.Written);
            } finally {
                port.Close ();
            }
        }

        /// <summary>
        /// Data handed to Write before the port is closed must still be sent, so that a response
        /// the server has already produced is not lost when the server stops.
        /// </summary>
        [Test]
        public void ClosingSendsWhatIsLeft ()
        {
            var testPort = new TestPort ();
            testPort.WriteDelay = 20;
            var port = new BufferedPort (testPort);
            port.Open ();
            for (int i = 0; i < 4; i++)
                port.Write (new byte[] { (byte)i }, 0, 1);
            port.Close ();
            Assert.AreEqual (4, testPort.Written.Length);
        }

        /// <summary>
        /// A port that never finishes sending must not hold up the caller indefinitely.
        /// </summary>
        [Test]
        public void ClosingGivesUpOnAStuckPort ()
        {
            var testPort = new TestPort ();
            testPort.WriteDelay = 30000;
            var port = new BufferedPort (testPort);
            port.Open ();
            port.Write (new byte[] { 1 }, 0, 1);
            var timer = Stopwatch.StartNew ();
            port.Close ();
            Assert.Less (timer.ElapsedMilliseconds, timeout, "Closing waited for the stuck port");
            Assert.IsFalse (port.IsOpen);
        }

        /// <summary>
        /// A caller writing faster than the port can send, for long enough, must not grow the
        /// send buffer without limit. The port cannot pace the caller, so it fails instead.
        /// </summary>
        [Test]
        public void OverfillingTheSendBufferFailsThePort ()
        {
            var testPort = new TestPort ();
            testPort.WriteDelay = 30000;
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                var chunk = new byte [64 * 1024];
                Assert.Throws<System.IO.IOException> (() => {
                    for (int i = 0; i < 32; i++)
                        port.Write (chunk, 0, chunk.Length);
                });
                Assert.IsFalse (port.IsOpen);
            } finally {
                port.Close ();
            }
        }

        [Test]
        public void CannotBeOpenedTwice ()
        {
            var port = new BufferedPort (new TestPort ());
            port.Open ();
            port.Close ();
            Assert.Throws<InvalidOperationException> (() => port.Open ());
        }

        [Test]
        public void FailedReadClosesThePort ()
        {
            var testPort = new TestPort ();
            testPort.ReadFailure = new System.IO.IOException ("read failed");
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                // Reads are only issued for data the port holds, so give it some to provoke the
                // failing read
                testPort.QueueRead (new byte[] { 1 });
                WaitFor (() => !port.IsOpen);
                Assert.Throws<System.IO.IOException> (() => port.Read (new byte [8], 0, 8));
                Assert.Throws<System.IO.IOException> (() => port.Write (new byte[] { 1 }, 0, 1));
            } finally {
                port.Close ();
            }
        }

        [Test]
        public void FailedWriteClosesThePort ()
        {
            var testPort = new TestPort ();
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                testPort.WriteFailure = new System.IO.IOException ("write failed");
                port.Write (new byte[] { 1 }, 0, 1);
                WaitFor (() => !port.IsOpen);
            } finally {
                port.Close ();
            }
        }

        /// <summary>
        /// Reading from and writing to the port at the same time, from the thread that would be
        /// running the game, must lose nothing in either direction.
        /// </summary>
        [Test]
        public void ReadAndWriteTogether ()
        {
            var testPort = new TestPort ();
            var port = new BufferedPort (testPort);
            port.Open ();
            try {
                var received = new List<byte> ();
                var buffer = new byte [256];
                for (int i = 0; i < 200; i++) {
                    testPort.QueueRead (new byte[] { (byte)i });
                    port.Write (new byte[] { (byte)i }, 0, 1);
                    var read = port.Read (buffer, 0, buffer.Length);
                    for (int j = 0; j < read; j++)
                        received.Add (buffer [j]);
                }
                WaitFor (() => testPort.Written.Length == 200);
                WaitFor (() => {
                    var read = port.Read (buffer, 0, buffer.Length);
                    for (int j = 0; j < read; j++)
                        received.Add (buffer [j]);
                    return received.Count == 200;
                });
                for (int i = 0; i < 200; i++) {
                    Assert.AreEqual ((byte)i, received [i]);
                    Assert.AreEqual ((byte)i, testPort.Written [i]);
                }
            } finally {
                port.Close ();
            }
        }
    }
}
