using System;

namespace KRPC.Service.Attributes
{
    [AttributeUsage (AttributeTargets.Class)]
    public class KRPCClassAttribute : Attribute
    {
        public string Service { get; set; }
    }
}
