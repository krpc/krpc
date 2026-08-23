using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    /// <summary>
    /// The connection carried over TCP/IP, against a server the test listens on itself
    /// rather than a kRPC server.
    /// </summary>
    [TestFixture]
    public class TCPIPConnectionTest : ConnectionTestCase
    {
        protected override Socket Listen ()
        {
            var socket = new Socket (
                AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Port zero, so the system picks one nothing else is using
            socket.Bind (new IPEndPoint (IPAddress.Loopback, 0));
            return socket;
        }

        protected override Connection Connect (string name)
        {
            var endPoint = (IPEndPoint)Server.EndPoint;
            return new Connection (
                name, IPAddress.Loopback, endPoint.Port, endPoint.Port);
        }
    }
}
