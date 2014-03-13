using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    /// <summary>
    /// Class used to represent a celestial body, such as Kerbin or the Mun.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class CelestialBody
    {
    }
}
