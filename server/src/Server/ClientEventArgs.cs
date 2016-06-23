using System;

namespace KRPC.Server
{
    abstract class ClientEventArgs : EventArgs
    {
        protected ClientEventArgs (IClient client)
        {
            Client = client;
        }

        /// <summary>
        /// The client.
        /// </summary>
        public IClient Client { get; }
    }

    abstract class ClientEventArgs<TIn,TOut> : EventArgs
    {
        protected ClientEventArgs (IClient<TIn,TOut> client)
        {
            Client = client;
        }

        /// <summary>
        /// The client.
        /// </summary>
        public IClient<TIn,TOut> Client { get; }
    }
}
