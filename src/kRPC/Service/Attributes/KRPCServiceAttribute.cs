using System;

namespace KRPC.Service.Attributes
{
    [AttributeUsage (AttributeTargets.Class)]
    public class KRPCServiceAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
