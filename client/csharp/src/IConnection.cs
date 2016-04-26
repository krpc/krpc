using System.Collections.Generic;
using Google.Protobuf;

namespace KRPC.Client
{
    /// <summary>
    /// A connection to the KRPC server
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Invoke a remote procedure call
        /// </summary>
        ByteString Invoke (string service, string procedure, IList<ByteString> arguments = null);
    }
}
