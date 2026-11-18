using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using KRPC.Server.SerialIO;
using NUnit.Framework;

namespace KRPC.Test.Server.SerialIO
{
    /// <summary>
    /// A real serial port, over a pair of pseudo-terminals that socat connects. Skipped where
    /// socat is not installed.
    /// </summary>
    [TestFixture]
    public class SerialPortTest
    {
        Process socat;
        string directory;
        string serverPort;
        string clientPort;

        static string FindSocat ()
        {
            var path = Environment.GetEnvironmentVariable ("PATH") ?? string.Empty;
            foreach (var dir in path.Split (Path.PathSeparator)) {
                var candidate = Path.Combine (dir, "socat");
                if (File.Exists (candidate))
                    return candidate;
            }
            return null;
        }

        static void WaitFor (Func<bool> condition)
        {
            var timer = Stopwatch.StartNew ();
            while (!condition ()) {
                if (timer.ElapsedMilliseconds > 5000)
                    Assert.Fail ("Timed out waiting for the port");
                Thread.Sleep (1);
            }
        }

        [SetUp]
        public void SetUp ()
        {
            var executable = OperatingSystem.IsWindows () ? null : FindSocat ();
            if (executable == null)
                Assert.Ignore ("socat is not installed");
            directory = Path.Combine (Path.GetTempPath (), "krpc-serial-" + Guid.NewGuid ().ToString ("N"));
            Directory.CreateDirectory (directory);
            serverPort = Path.Combine (directory, "server-port");
            clientPort = Path.Combine (directory, "client-port");
            socat = Process.Start (new ProcessStartInfo (executable,
                "PTY,raw,echo=0,link=" + serverPort + " PTY,raw,echo=0,link=" + clientPort) {
                UseShellExecute = false
            });
            WaitFor (() => File.Exists (serverPort) && File.Exists (clientPort));
        }

        [TearDown]
        public void TearDown ()
        {
            if (socat != null) {
                socat.Kill ();
                socat.WaitForExit ();
                socat = null;
            }
            if (directory != null)
                Directory.Delete (directory, true);
        }

        [Test]
        public void ReceiveThenClose ()
        {
            var port = new BufferedPort (new SerialPortAdapter (new SerialPort (serverPort)));
            port.Open ();
            using (var client = new FileStream (clientPort, FileMode.Open, FileAccess.Write)) {
                var data = new byte [1000];
                for (int i = 0; i < data.Length; i++)
                    data [i] = (byte)i;
                // In pieces with gaps, so the read thread waits for data between them
                for (int i = 0; i < data.Length; i += 100) {
                    client.Write (data, i, 100);
                    client.Flush ();
                    Thread.Sleep (5);
                }
                WaitFor (() => port.BytesAvailable == data.Length);
                var received = new byte [data.Length];
                Assert.AreEqual (data.Length, port.Read (received, 0, received.Length));
                CollectionAssert.AreEqual (data, received);
            }
            // The read thread is waiting for data, and closing the port ends the wait
            var timer = Stopwatch.StartNew ();
            port.Close ();
            Assert.Less (timer.ElapsedMilliseconds, 450);
            Assert.IsFalse (port.IsOpen);
        }
    }
}
