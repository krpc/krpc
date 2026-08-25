using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KRPC.Client
{
    /// <summary>
    /// An endpoint in the unix domain, named by a path in the file system. The .NET
    /// Framework profile this client is built against has no endpoint for this address
    /// family, so it is implemented here: a sockaddr_un is the address family followed
    /// by the path and a terminating zero. Windows names and lays its own out the same
    /// way, so one encoding serves every platform.
    /// </summary>
    sealed class UnixEndPoint : EndPoint
    {
        readonly string path;

        public UnixEndPoint (string socketPath)
        {
            if (socketPath == null)
                throw new ArgumentNullException (nameof (socketPath));
            path = socketPath;
        }

        public string Path {
            get { return path; }
        }

        public override AddressFamily AddressFamily {
            get { return AddressFamily.Unix; }
        }

        /// <summary>
        /// The size of the path in a socket address, and of the address it sits in. The path
        /// is terminated by the zeros it is copied into rather than by one copied over them,
        /// so a path that fills the field entirely is one byte too long.
        /// </summary>
        public const int PathLength = 108;
        public const int AddressLength = 2 + PathLength;

        /// <summary>
        /// Encode the endpoint as a socket address, which is only as long as the path needs.
        /// The whole field the path sits in would be the more natural size, but Mono refuses
        /// to bind an address that leaves no room in it for a terminator.
        /// </summary>
        public override SocketAddress Serialize ()
        {
            var bytes = Encoding.UTF8.GetBytes (path);
            if (bytes.Length >= PathLength)
                throw new ArgumentException (
                    "Socket path takes " + bytes.Length + " bytes, which is longer than the " +
                    (PathLength - 1) + " a socket address has room for");
            var address = new SocketAddress (AddressFamily.Unix, SerializedLength (bytes.Length));
            for (int i = 0; i < bytes.Length; i++)
                address [2 + i] = bytes [i];
            address [2 + bytes.Length] = 0;
            return address;
        }

        /// <summary>
        /// How long the address of a path of the given size is.
        /// </summary>
        static int SerializedLength (int pathBytes)
        {
            return 2 + pathBytes + 1;
        }

        public override EndPoint Create (SocketAddress socketAddress)
        {
            if (socketAddress == null)
                throw new ArgumentNullException (nameof (socketAddress));
            // A call handed the address this endpoint serialized to writes into it and reports
            // the size it wrote whether or not it was given that much room
            int written = Math.Min (
                socketAddress.Size,
                SerializedLength (Encoding.UTF8.GetByteCount (path)));
            // The path is padded with zeros out to the size of the address
            int size = written - 2;
            while (size > 0 && socketAddress [size + 1] == 0)
                size--;
            var bytes = new byte [size];
            for (int i = 0; i < size; i++)
                bytes [i] = socketAddress [i + 2];
            return new UnixEndPoint (Encoding.UTF8.GetString (bytes));
        }

        public override string ToString ()
        {
            return path;
        }

        public override bool Equals (object obj)
        {
            var other = obj as UnixEndPoint;
            return (object)other != null && other.path == path;
        }

        public override int GetHashCode ()
        {
            return path.GetHashCode ();
        }
    }
}
