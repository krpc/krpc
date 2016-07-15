using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class StreamMessage : IMessage
    {
        public IList<StreamResponse> Responses { get; private set; }

        public StreamMessage ()
        {
            Responses = new List<StreamResponse> ();
        }
    }
}
