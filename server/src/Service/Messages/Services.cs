using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Services : IMessage
    {
        public IList<Service> ServicesList { get; private set; }

        public Services ()
        {
            ServicesList = new List<Service> ();
        }
    }
}
