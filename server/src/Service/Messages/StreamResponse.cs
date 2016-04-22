using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class StreamResponse : IMessage
    {
        public uint Id;
        public Response Response;
    }
}
