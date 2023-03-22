using System.Diagnostics.CodeAnalysis;

namespace KRPC.Server
{
    /// <summary>
    /// Arguments passed to a client requesting connection event
    /// </summary>
    public sealed class ClientRequestingConnectionEventArgs : ClientEventArgs
    {
        /// <summary>
        /// The request.
        /// </summary>
        public ClientConnectionRequest Request { get; private set; }

        internal ClientRequestingConnectionEventArgs (IClient client, ClientConnectionRequest request) : base (client)
        {
            Request = request;
        }
    }

    /// <summary>
    /// Arguments passed to a client requesting connection event
    /// </summary>
    public sealed class ClientRequestingConnectionEventArgs<TIn,TOut> : ClientEventArgs<TIn,TOut>
    {
        /// <summary>
        /// The request.
        /// </summary>
        public ClientConnectionRequest Request { get; private set; }

        internal ClientRequestingConnectionEventArgs (IClient<TIn,TOut> client) : base (client)
        {
            Request = new ClientConnectionRequest ();
        }

        /// <summary>
        /// Convert a generic client requesting connection event to a non-generic one.
        /// </summary>
        [SuppressMessage ("Gendarme.Rules.Correctness", "CheckParametersNullityInVisibleMethodsRule")]
        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotDeclareStaticMembersOnGenericTypesRule")]
        public static implicit operator ClientRequestingConnectionEventArgs (ClientRequestingConnectionEventArgs<TIn,TOut> args)
        {
            return new ClientRequestingConnectionEventArgs (args.Client, args.Request);
        }
    }
}
