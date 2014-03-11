using KRPC.Service.Attributes;

namespace KRPCServices.Services
{
    /// <summary>
    /// Class representing a vessel. For example, can be used to control the vessel and get orbital data.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Vessel
    {
        VesselData vesselData;

        internal Vessel (global::Vessel vessel)
        {
            vesselData = new VesselData (vessel);
            Flight = new Flight (vesselData);
            Orbit = new Orbit (vesselData);
            Control = new Control (vessel);
            Resources = new Resources (vessel);
        }

        [KRPCProperty]
        public Flight Flight { get; private set; }

        [KRPCProperty]
        public Orbit Orbit { get; private set; }

        [KRPCProperty]
        public Control Control { get; private set; }

        [KRPCProperty]
        public Resources Resources { get; private set; }
    }
}
