using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using NUnit.Framework;
using KRPC.Server;
using KRPC.Server.Net;

namespace KRPCTest.Server.Net
{
    [TestFixture]
    public class TCPServerTest
    {
        [Test]
        public void Simple ()
        {
            var server = new TCPServer (IPAddress.Loopback, 0);
            Assert.IsFalse (server.Running);
            server.Start ();
            Assert.IsTrue (server.Running);
            Assert.AreEqual (0, server.Clients.Count ());
            Assert.AreEqual (IPAddress.Loopback, server.Address);
            Assert.IsTrue (server.Port > 0);
            server.Stop ();
            Assert.IsFalse (server.Running);
            Assert.AreEqual (0, server.Clients.Count ());
        }

        [Test]
        public void StartStop ()
        {
            var server = new TCPServer (IPAddress.Loopback, 0);
            Assert.IsFalse (server.Running);
            for (int i = 0; i < 5; i++) {
                server.Start ();
                Assert.IsTrue (server.Running);
                server.Stop ();
                Assert.IsFalse (server.Running);
            }
        }

        [Test]
        public void ClientConnectAndDisconnect ()
        {
            var server = new TCPServer (IPAddress.Loopback, 0);

            bool clientRequestingConnection = false;
            bool clientConnected = false;
            bool clientDisconnected = false;
            server.OnClientRequestingConnection += (s, e) => {
                e.Allow ();
                clientRequestingConnection = true;
            };
            server.OnClientConnected += (s, e) => clientConnected = true;
            server.OnClientDisconnected += (s, e) => clientDisconnected = true;

            server.Start ();

            var tcpClient = new TcpClient (server.Address.ToString(), server.Port);
            UpdateUntil(server, () => { return clientConnected; });

            Assert.IsTrue (tcpClient.Connected);
            Assert.IsFalse (clientDisconnected);
            Assert.AreEqual (1, server.Clients.Count ());

            tcpClient.GetStream ().Close ();
            tcpClient.Close ();
            Assert.IsFalse (tcpClient.Connected);
            UpdateUntil(server, () => { return clientDisconnected; });
            Assert.AreEqual (0, server.Clients.Count ());

            Assert.IsFalse (tcpClient.Connected);
            server.Stop ();

            Assert.IsTrue (clientRequestingConnection);
            Assert.IsTrue (clientConnected);
            Assert.IsTrue (clientDisconnected);
        }

        [Test]
        public void StillPendingByDefault ()
        {
            var server = new TCPServer (IPAddress.Loopback, 0);

            bool clientRequestingConnection = false;
            server.OnClientRequestingConnection += (s, e) => clientRequestingConnection = true;
            server.OnClientConnected += (s, e) => Assert.Fail ();
            server.OnClientDisconnected += (s, e) => Assert.Fail ();

            server.Start ();

            var tcpClient = new TcpClient (server.Address.ToString(), server.Port);
            UpdateUntil(server, () => { return clientRequestingConnection; });

            Assert.IsTrue (clientRequestingConnection);
            Assert.AreEqual (0, server.Clients.Count ());

            server.Stop ();
            tcpClient.Close ();
        }

        [Test]
        public void StopDisconnectsClient ()
        {
            var server = new TCPServer (IPAddress.Loopback, 0);

            bool clientConnected = false;
            bool clientDisconnected = false;
            server.OnClientRequestingConnection += (s, e) => e.Allow ();
            server.OnClientConnected += (s, e) => clientConnected = true;
            server.OnClientDisconnected += (s, e) => clientDisconnected = true;

            server.Start ();

            var tcpClient = new TcpClient (server.Address.ToString(), server.Port);
            UpdateUntil(server, () => { return clientConnected; });

            Assert.IsFalse (clientDisconnected);
            Assert.AreEqual (1, server.Clients.Count ());

            server.Stop ();

            Assert.IsTrue (clientDisconnected);
            Assert.AreEqual (0, server.Clients.Count ());

            tcpClient.Close ();
        }

        delegate bool BooleanPredicate ();

        // Calls server.Update repeatedly every 50 ms, until predicate is true
        // or up to a maximum number of iterations, after which point the test fails
        void UpdateUntil (TCPServer server, BooleanPredicate predicate, int iterations = 10)
        {
            for (int i = 0; i < iterations; i++) {
                server.Update ();
                if (predicate())
                    return;
                System.Threading.Thread.Sleep (50);
            }
            Assert.Fail ();
        }
    }
}

