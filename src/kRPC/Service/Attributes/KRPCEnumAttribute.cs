using System;

namespace KRPC.Service.Attributes
{
    [AttributeUsage (AttributeTargets.Enum)]
    public class KRPCEnumAttribute : Attribute
    {
        public string Service { get; set; }
    }
}
