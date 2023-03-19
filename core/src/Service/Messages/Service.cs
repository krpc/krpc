using System.Collections.Generic;

namespace KRPC.Service.Messages
{
    #pragma warning disable 1591
    public class Service : IMessage
    {
        public string Name { get; private set; }

        public IList<Procedure> Procedures { get; private set; }

        public IList<Class> Classes { get; private set; }

        public IList<Enumeration> Enumerations { get; private set; }

        public IList<Exception> Exceptions { get; private set; }

        public string Documentation { get; set; }

        public Service (string name)
        {
            Name = name;
            Procedures = new List<Procedure> ();
            Classes = new List<Class> ();
            Enumerations = new List<Enumeration> ();
            Exceptions = new List<Exception> ();
            Documentation = string.Empty;
        }
    }
}
