using KRPC.Service.Attributes;

namespace Extending
{
    // [docs]
    [KRPCClass (Service = "EVA")]
    public class Marker {
        [KRPCProperty]
        public string Label { get; set; }
    }

    [KRPCClass (Service = "EVA")]
    [KRPCProcedureId (nameof (Marker.Label), GetterId = 42)]
    public class Flag : Marker {
        // ...
    }
    // [/docs]
}
