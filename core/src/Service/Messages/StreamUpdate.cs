using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class StreamUpdate : IMessage
    {
        public IList<StreamResult> Results { get; private set; }

        public StreamUpdate ()
        {
            Results = new List<StreamResult> ();
        }
    }
}
