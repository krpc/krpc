using System;
using System.Net;
using System.Net.Sockets;

namespace KRPC.Client
{
    /// <summary>
    /// A transport to a server over TCP/IP.
    /// </summary>
    sealed class TCPIPTransport : Transport
    {
        /// <summary>
        /// Connect. A zero timeout waits for as long as the operating system does. Throws a
        /// SocketException if the connection fails.
        /// </summary>
        public TCPIPTransport (IPAddress address, int port, TimeSpan timeout)
            : base (Open (address, port, timeout))
        {
        }

        static Socket Open (IPAddress address, int port, TimeSpan timeout)
        {
            var socket = new Socket (address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (timeout == TimeSpan.Zero) {
                socket.Connect (address, port);
            } else {
                // A network that drops a connection attempt instead of refusing it leaves the
                // client waiting, so bound the wait where one was asked for.
                var pending = socket.BeginConnect (address, port, null, null);
                if (!pending.AsyncWaitHandle.WaitOne (timeout)) {
                    socket.Close ();
                    throw new SocketException ((int)SocketError.TimedOut);
                }
                socket.EndConnect (pending);
            }
            // A message is sent as soon as it is written, so waiting for more data to coalesce
            // with can only add latency
            socket.NoDelay = true;
            return socket;
        }
    }
}
