using KRPC.Service.Messages;
using KRPC.Utils;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// The calls of an HTTP server's session, one request per HTTP request.
    /// </summary>
    sealed class SessionStream : IMessageStream
    {
        readonly Server server;

        public SessionStream (Server httpServer)
        {
            server = httpServer;
        }

        public bool DataAvailable {
            get { return server.HasRequest; }
        }

        public Request Read ()
        {
            return server.TakeRequest ();
        }

        public void Write (Response value)
        {
            server.Respond (value);
        }

        // An HTTP request cannot add a stream, so there are no updates to send
        public void Write (StreamUpdate value)
        {
        }

        public void StreamRemoved (ulong id)
        {
        }

        public void Disconnect (DisconnectReason reason, string message)
        {
            Logger.WriteLine ("HTTP: " + message, Logger.Severity.Warning);
        }

        public void Close ()
        {
        }
    }
}
