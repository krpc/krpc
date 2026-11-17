using System;
using KRPC.Service.Messages;

namespace KRPC.Server.Message
{
    /// <summary>
    /// The messages of a client's session, read and written only while the session is current.
    /// </summary>
    sealed class Stream : IMessageStream
    {
        readonly Client client;
        readonly Connection connection;

        public Stream (Client sessionClient, Connection innerConnection)
        {
            client = sessionClient;
            connection = innerConnection;
        }

        /// <summary>
        /// Whether a request is waiting to be read.
        /// </summary>
        public bool DataAvailable {
            get { return connection.Poll (client); }
        }

        public Request Read ()
        {
            return connection.TakeRequest (client);
        }

        public void Write (Response value)
        {
            connection.Write (client, value);
        }

        public void Write (StreamUpdate value)
        {
            connection.Write (client, value);
        }

        public void StreamRemoved (ulong id)
        {
            connection.StreamRemoved (id);
        }

        public void Disconnect (DisconnectReason reason, string message)
        {
            connection.Disconnect (client, reason, message);
        }

        /// <summary>
        /// Close the connection, with no message.
        /// </summary>
        public void Close ()
        {
            connection.Close (client);
        }
    }
}
