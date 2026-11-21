using KRPC.Service.Attributes;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A row of widgets, laid out from left to right. Added to a window or a row.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public sealed class Row : Container
    {
        internal Row (bool visible) : base (visible)
        {
        }

        internal override void DrawWidget ()
        {
            DrawChildren (true);
        }
    }
}
