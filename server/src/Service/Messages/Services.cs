using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    //TODO: Unnecessary? Just replace with a List<Service> ?
    #pragma warning disable 1591
    public class Services : IMessage
    {
        public IList<Service> Services_ = new List<Service> ();
    }
}
