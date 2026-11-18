using KRPC.Server;
using KRPC.Service.Attributes;

namespace KRPC.Service.KRPC
{
    /// <summary>
    /// A client connected to the server.
    /// </summary>
    /// <remarks>
    /// See <see cref="KRPC.Clients"/>.
    /// </remarks>
    [KRPCStruct (Service = "KRPC")]
    public struct ClientInfo
    {
        internal ClientInfo (IClient client) : this ()
        {
            Id = client.Guid.ToByteArray ();
            Name = client.Name;
            Address = client.Address;
        }

        /// <summary>
        /// The identifier of the client.
        /// </summary>
        [KRPCProperty]
        public byte[] Id { get; set; }

        /// <summary>
        /// The name of the client, or an empty string if it has no name.
        /// </summary>
        [KRPCProperty]
        public string Name { get; set; }

        /// <summary>
        /// The address of the client.
        /// </summary>
        [KRPCProperty]
        public string Address { get; set; }
    }
}
