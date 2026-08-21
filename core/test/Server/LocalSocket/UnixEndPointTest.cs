using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using KRPC.Server.LocalSocket;
using NUnit.Framework;

namespace KRPC.Test.Server.LocalSocket
{
    [TestFixture]
    public class UnixEndPointTest
    {
        [Test]
        public void Simple ()
        {
            var endPoint = new UnixEndPoint ("/tmp/krpc/rpc");
            Assert.AreEqual ("/tmp/krpc/rpc", endPoint.Path);
            Assert.AreEqual ("/tmp/krpc/rpc", endPoint.ToString ());
            Assert.AreEqual (AddressFamily.Unix, endPoint.AddressFamily);
        }

        [Test]
        public void NullPathIsRejected ()
        {
            Assert.Throws<ArgumentNullException> (() => new UnixEndPoint (null));
        }

        [Test]
        public void SerializesAsASocketAddress ()
        {
            var address = new UnixEndPoint ("/tmp/x").Serialize ();
            Assert.AreEqual (AddressFamily.Unix, address.Family);
            // The family, the path, and a terminating zero
            Assert.AreEqual (2 + 6 + 1, address.Size);
            Assert.AreEqual ((byte)'/', address [2]);
            Assert.AreEqual ((byte)'x', address [7]);
            Assert.AreEqual (0, address [8]);
        }

        [Test]
        public void ReportsAPathTooLongToFitInAnAddress ()
        {
            var endPoint = new UnixEndPoint (new string ('x', UnixEndPoint.PathLength));
            Assert.Throws<ArgumentException> (() => endPoint.Serialize ());
        }

        [Test]
        public void ReadsNoMoreOfAnAddressThanItGaveRoomFor ()
        {
            // Accepting a connection reports the size the system wrote whether or not it was
            // given that much room to write it in, and reading the rest would run off the end
            var endPoint = new UnixEndPoint ("/tmp/x");
            var address = new SocketAddress (
                AddressFamily.Unix, UnixEndPoint.AddressLength);
            for (int i = 0; i < 6; i++)
                address [2 + i] = (byte)"/tmp/x" [i];
            Assert.AreEqual (endPoint, endPoint.Create (address));
        }

        [Test]
        public void RoundTripsThroughASocketAddress ()
        {
            var endPoint = new UnixEndPoint ("/tmp/krpc/stream");
            var result = endPoint.Create (endPoint.Serialize ());
            Assert.AreEqual (endPoint, result);
            Assert.AreEqual ("/tmp/krpc/stream", ((UnixEndPoint)result).Path);
        }

        [Test]
        public void EqualityIsByPath()
        {
            Assert.AreEqual (new UnixEndPoint ("/tmp/a"), new UnixEndPoint ("/tmp/a"));
            Assert.AreNotEqual (new UnixEndPoint ("/tmp/a"), new UnixEndPoint ("/tmp/b"));
            Assert.AreEqual (new UnixEndPoint ("/tmp/a").GetHashCode (),
                             new UnixEndPoint ("/tmp/a").GetHashCode ());
        }

        [Test]
        public void DefaultPathIsWithinALengthASocketAccepts ()
        {
            foreach (var name in new [] { "rpc", "stream" }) {
                var path = LocalSocketServer.DefaultPath (name);
                Assert.IsTrue (path.EndsWith (name, StringComparison.Ordinal));
                Assert.IsTrue (LocalSocketServer.PathLength (path) <= LocalSocketServer.MaximumPathLength);
            }
        }

        [Test]
        [Platform (Exclude = "Win", Reason = "the directory it falls back to is one only POSIX has")]
        public void DefaultPathFallsBackToAFixedDirectory ()
        {
            // A client works the same path out in a process of its own, where TMPDIR may
            // well say something else, so the fallback cannot be whichever temporary
            // directory this process was pointed at
            var runtimeDirectory = Environment.GetEnvironmentVariable ("XDG_RUNTIME_DIR");
            var temporaryDirectory = Environment.GetEnvironmentVariable ("TMPDIR");
            try {
                Environment.SetEnvironmentVariable ("XDG_RUNTIME_DIR", null);
                Environment.SetEnvironmentVariable ("TMPDIR", "/somewhere/else");
                Assert.AreEqual ("/tmp/krpc-" + Environment.UserName + "/rpc",
                                 LocalSocketServer.DefaultPath ("rpc"));
            } finally {
                Environment.SetEnvironmentVariable ("XDG_RUNTIME_DIR", runtimeDirectory);
                Environment.SetEnvironmentVariable ("TMPDIR", temporaryDirectory);
            }
        }

        [Test]
        public void DefaultPathUsesTheRuntimeDirectoryWhenThereIsOne ()
        {
            var variable = Environment.OSVersion.Platform == PlatformID.Win32NT ?
                "LOCALAPPDATA" : "XDG_RUNTIME_DIR";
            var directory = Environment.GetEnvironmentVariable (variable);
            try {
                Environment.SetEnvironmentVariable (variable, Path.Combine ("/run", "user"));
                Assert.AreEqual (Path.Combine ("/run", "user", "krpc", "rpc"),
                                 LocalSocketServer.DefaultPath ("rpc"));
            } finally {
                Environment.SetEnvironmentVariable (variable, directory);
            }
        }
    }
}
