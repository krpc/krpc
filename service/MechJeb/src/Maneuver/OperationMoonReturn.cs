using KRPC.Service.Attributes;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Return from a moon to the body it orbits.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationMoonReturn : Operation
    {
        static Value<double> moonReturnAltitude;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.OperationMoonReturn");
            moonReturnAltitude = Reflection.EditableDouble (type, "MoonReturnAltitude");
        }

        internal OperationMoonReturn () : base ("OperationMoonReturn")
        {
        }

        /// <summary>
        /// The approximate periapsis altitude at the body, in meters.
        /// </summary>
        [KRPCProperty]
        public double MoonReturnAltitude {
            get { return moonReturnAltitude.Get (Instance); }
            set { moonReturnAltitude.Set (Instance, value); }
        }
    }
}
