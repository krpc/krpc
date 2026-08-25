using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using KRPC.Server;
using KRPC.Server.LocalSocket;
using NUnit.Framework;

namespace KRPC.Test.Server.LocalSocket
{
    [TestFixture]
    public class LocalSocketServerTest
    {
        string path;

        [SetUp]
        public void SetUp ()
        {
            // A path per test, so tests running in the same directory cannot collide
            path = Path.Combine (TestingTools.SocketDirectory (), "krpc-test-" + Guid.NewGuid ().ToString ("N").Substring (0, 8));
        }

        [TearDown]
        public void TearDown ()
        {
            if (Directory.Exists (path))
                Directory.Delete (path, true);
            else
                File.Delete (path);
        }

        Socket Connect ()
        {
            var socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect (new UnixEndPoint (path));
            return socket;
        }

        [Test]
        public void Simple ()
        {
            bool serverStarted = false;
            bool serverStopped = false;
            var server = new LocalSocketServer (path);
            server.OnStarted += (s, e) => serverStarted = true;
            server.OnStopped += (s, e) => serverStopped = true;
            server.OnClientRequestingConnection += (s, e) => {
                return;
            };
            Assert.IsFalse (server.Running);
            server.Start ();
            Assert.IsTrue (server.Running);
            Assert.AreEqual (0, server.Clients.Count ());
            Assert.AreEqual (path, server.ListenPath);
            Assert.AreEqual (path, server.Address);
            server.Stop ();
            Assert.IsFalse (server.Running);
            Assert.AreEqual (0, server.Clients.Count ());
            Assert.IsTrue (serverStarted);
            Assert.IsTrue (serverStopped);
            Assert.AreEqual (0, server.BytesRead);
            Assert.AreEqual (0, server.BytesWritten);
        }

        [Test]
        public void StartStop ()
        {
            int serverStarted = 0;
            int serverStopped = 0;
            var server = new LocalSocketServer (path);
            server.OnStarted += (s, e) => serverStarted++;
            server.OnStopped += (s, e) => serverStopped++;
            server.OnClientRequestingConnection += (s, e) => {
                return;
            };
            Assert.IsFalse (server.Running);
            for (int i = 0; i < 5; i++) {
                server.Start ();
                Assert.IsTrue (server.Running);
                server.Stop ();
                Assert.IsFalse (server.Running);
            }
            Assert.AreEqual (5, serverStarted);
            Assert.AreEqual (5, serverStopped);
        }

        [Test]
        public void ClientConnectAndDisconnect ()
        {
            var server = new LocalSocketServer (path);

            bool clientRequestingConnection = false;
            bool clientConnected = false;
            bool clientDisconnected = false;
            server.OnClientRequestingConnection += (s, e) => {
                e.Request.Allow ();
                clientRequestingConnection = true;
            };
            server.OnClientConnected += (s, e) => clientConnected = true;
            server.OnClientDisconnected += (s, e) => clientDisconnected = true;

            server.Start ();

            var client = Connect ();
            UpdateUntil (server, () => clientConnected);

            Assert.IsFalse (clientDisconnected);
            Assert.AreEqual (1, server.Clients.Count ());

            client.Shutdown (SocketShutdown.Both);
            client.Close ();
            UpdateUntil (server, () => clientDisconnected);
            Assert.AreEqual (0, server.Clients.Count ());

            server.Stop ();

            Assert.IsTrue (clientRequestingConnection);
            Assert.IsTrue (clientConnected);
            Assert.IsTrue (clientDisconnected);
        }

        [Test]
        public void StillPendingByDefault ()
        {
            var server = new LocalSocketServer (path);

            bool clientRequestingConnection = false;
            server.OnClientRequestingConnection += (s, e) => clientRequestingConnection = true;
            server.OnClientConnected += (s, e) => Assert.Fail ();
            server.OnClientDisconnected += (s, e) => Assert.Fail ();

            server.Start ();

            var client = Connect ();
            UpdateUntil (server, () => clientRequestingConnection);

            Assert.IsTrue (clientRequestingConnection);
            Assert.AreEqual (0, server.Clients.Count ());

            server.Stop ();
            client.Close ();
        }

        [Test]
        public void StopDisconnectsClient ()
        {
            var server = new LocalSocketServer (path);

            bool clientConnected = false;
            bool clientDisconnected = false;
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            server.OnClientConnected += (s, e) => clientConnected = true;
            server.OnClientDisconnected += (s, e) => clientDisconnected = true;

            server.Start ();

            var client = Connect ();
            UpdateUntil (server, () => clientConnected);

            Assert.IsFalse (clientDisconnected);
            Assert.AreEqual (1, server.Clients.Count ());

            server.Stop ();

            Assert.IsTrue (clientDisconnected);
            Assert.AreEqual (0, server.Clients.Count ());

            client.Close ();
        }

        [Test]
        public void SendAndReceive ()
        {
            var server = new LocalSocketServer (path);
            bool clientConnected = false;
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            server.OnClientConnected += (s, e) => clientConnected = true;
            server.Start ();

            var client = Connect ();
            UpdateUntil (server, () => clientConnected);
            var serverClient = server.Clients.First ();

            client.Send (new byte [] { 1, 2, 3 });
            UpdateUntil (server, () => serverClient.Stream.DataAvailable);
            var buffer = new byte [3];
            Assert.AreEqual (3, serverClient.Stream.Read (buffer, 0));
            CollectionAssert.AreEqual (new byte [] { 1, 2, 3 }, buffer);

            serverClient.Stream.Write (new byte [] { 4, 5 });
            var back = new byte [2];
            Assert.AreEqual (2, client.Receive (back));
            CollectionAssert.AreEqual (new byte [] { 4, 5 }, back);

            Assert.AreEqual (3, server.BytesRead);
            Assert.AreEqual (2, server.BytesWritten);

            server.Stop ();
            client.Close ();
        }

        [Test]
        public void StaleSocketFileDoesNotPreventStart ()
        {
            // A run that is killed rather than stopped leaves the socket file behind,
            // which would otherwise make every later bind fail
            File.WriteAllText (path, string.Empty);
            Assert.IsTrue (File.Exists (path));
            var server = new LocalSocketServer (path);
            server.OnClientRequestingConnection += (s, e) => {
                return;
            };
            server.Start ();
            Assert.IsTrue (server.Running);
            server.Stop ();
        }

        [Test]
        public void StartFailsWhenAnotherServerIsListening ()
        {
            // The socket file alone does not say whether the server that made it is still
            // there, and taking the path from a running server would leave it unreachable
            var running = new LocalSocketServer (path);
            running.OnClientRequestingConnection += (s, e) => {
                return;
            };
            running.Start ();
            var second = new LocalSocketServer (path);
            second.OnClientRequestingConnection += (s, e) => {
                return;
            };
            Assert.Throws<ServerException> (() => second.Start ());
            Assert.IsFalse (second.Running);
            Assert.IsTrue (running.Running);
            // The first server still has the path
            using (var client = Connect ())
                Assert.IsTrue (client.Connected);
            running.Stop ();
        }

        [Test]
        public void StartDoesNotDeleteAFileThatIsNotASocket ()
        {
            // A mistyped path points at a file that has nothing to do with the server, and
            // the server does not remove it
            File.WriteAllText (path, "not a socket");
            var server = new LocalSocketServer (path);
            server.OnClientRequestingConnection += (s, e) => {
                return;
            };
            Assert.Throws<ServerException> (() => server.Start ());
            Assert.IsFalse (server.Running);
            Assert.AreEqual ("not a socket", File.ReadAllText (path));
        }

        [Test]
        public void StartFailsWhenThePathIsADirectory ()
        {
            Directory.CreateDirectory (path);
            var server = new LocalSocketServer (path);
            server.OnClientRequestingConnection += (s, e) => {
                return;
            };
            Assert.Throws<ServerException> (() => server.Start ());
            Assert.IsFalse (server.Running);
            Assert.IsTrue (Directory.Exists (path));
        }

        [Test]
        [Platform (Exclude = "Win", Reason = "made with the link command POSIX has")]
        public void StartReportsAPathItCannotAsk ()
        {
            // A link with nothing on the end of it gives neither a connection nor a refusal,
            // so whether a server is there stays unknown
            Link ("/nowhere-at-all", path);
            var server = new LocalSocketServer (path);
            server.OnClientRequestingConnection += (s, e) => {
                return;
            };
            var exn = Assert.Throws<ServerException> (() => server.Start ());
            Assert.That (exn.Message, Does.Contain ("Could not find out"));
            Assert.IsFalse (server.Running);
        }

        [Test]
        [Platform (Exclude = "Win", Reason = "made with the link command POSIX has")]
        public void StartDoesNotFollowASymbolicLinkToWhatItPointsAt ()
        {
            // Removing the link would be one thing, removing what someone pointed it at another
            var target = path + "-target";
            File.WriteAllText (target, "precious");
            try {
                Link (target, path);
                var server = new LocalSocketServer (path);
                server.OnClientRequestingConnection += (s, e) => {
                    return;
                };
                Assert.Throws<ServerException> (() => server.Start ());
                Assert.AreEqual ("precious", File.ReadAllText (target));
            } finally {
                File.Delete (target);
            }
        }

        static void Link (string target, string name)
        {
            using (var link = System.Diagnostics.Process.Start ("ln", "-s " + target + " " + name))
                link.WaitForExit ();
        }

        [Test]
        public void StopRemovesTheSocketFile ()
        {
            var server = new LocalSocketServer (path);
            server.OnClientRequestingConnection += (s, e) => {
                return;
            };
            server.Start ();
            Assert.IsTrue (File.Exists (path));
            server.Stop ();
            Assert.IsFalse (File.Exists (path));
        }

        [Test]
        public void StartWithNoRequestHandlerFails ()
        {
            var server = new LocalSocketServer (path);
            Assert.Throws<KRPC.Server.ServerException> (() => server.Start ());
        }

        [Test]
        public void StartWithATooLongPathFails ()
        {
            // The limit is on the bytes a path takes, not the characters it is written with,
            // so a path in multi-byte characters reaches it with fewer characters than the
            // limit names
            var longPath = new string ('\u00e9', LocalSocketServer.MaximumPathLength / 2 + 1);
            Assert.IsTrue (longPath.Length <= LocalSocketServer.MaximumPathLength);
            Assert.IsTrue (LocalSocketServer.PathLength (longPath) > LocalSocketServer.MaximumPathLength);
            var server = new LocalSocketServer (longPath);
            server.OnClientRequestingConnection += (s, e) => e.Request.Allow ();
            var exn = Assert.Throws<KRPC.Server.ServerException> (() => server.Start ());
            StringAssert.Contains ("longer than the", exn.Message);
        }

        // Calls server.Update repeatedly every 50 ms, until predicate is true
        // or up to a maximum number of iterations, after which point the test fails
        static void UpdateUntil (KRPC.Server.IServer<byte, byte> server, Func<bool> predicate, int iterations = 10)
        {
            for (int i = 0; i < iterations; i++) {
                server.Update ();
                if (predicate ())
                    return;
                System.Threading.Thread.Sleep (50);
            }
            Assert.Fail ();
        }
    }
}
