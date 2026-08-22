using System;
using System.IO;
using System.Net.Sockets;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    /// <summary>
    /// The connection carried over a unix domain socket, against a server the test listens
    /// on itself rather than a kRPC server. The tests are those of the TCP/IP connection:
    /// what differs is only how the socket is opened.
    /// </summary>
    [TestFixture]
    public class LocalSocketConnectionTest : ConnectionTestCase
    {
        string directory;
        string path;

        /// <summary>
        /// A directory to put a socket in, short enough for the path of one to fit in a socket
        /// address. The directory a test is given for its temporary files is nested far deeper
        /// than an address has room for, so the platform's own is used directly.
        /// </summary>
        static string SocketDirectory ()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return "/tmp";
            var local = Environment.GetEnvironmentVariable ("LOCALAPPDATA");
            return string.IsNullOrEmpty (local)
                ? Path.GetTempPath () : Path.Combine (local, "Temp");
        }

        protected override Socket Listen ()
        {
            // A socket path has to fit in the kernel's address structure, which leaves far
            // less room than a path named after the test would take, so it goes in a short
            // temporary directory of its own
            directory = Path.Combine (
                SocketDirectory (), "krpc-" + Guid.NewGuid ().ToString ("N").Substring (0, 8));
            Directory.CreateDirectory (directory);
            path = Path.Combine (directory, "rpc");
            var socket = new Socket (
                AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Bind (new UnixEndPoint (path));
            return socket;
        }

        protected override Connection Connect (string name)
        {
            return Connection.ConnectLocal (name, path, path);
        }

        [TearDown]
        public void RemoveSocket ()
        {
            if (directory != null)
                Directory.Delete (directory, true);
        }

        [Test]
        public void ConnectToAPathNothingIsListeningOn ()
        {
            Assert.Throws<SocketException> (
                () => Connection.ConnectLocal (
                    "CSharpConnectionTestNoServer", path + "-does-not-exist", null));
        }
    }
}
