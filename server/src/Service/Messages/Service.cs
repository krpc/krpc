using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Service : IMessage
    {
        public string Name = "";
        public IList<Procedure> Procedures = new List<Procedure> ();
        public IList<Class> Classes = new List<Class> ();
        public IList<Enumeration> Enumerations = new List<Enumeration> ();
        public string Documentation = "";
    }
}
