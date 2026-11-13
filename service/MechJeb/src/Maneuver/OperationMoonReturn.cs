using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Return from a moon to the body it orbits.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationMoonReturn : Operation
    {
        static Field<object, double> moonReturnAltitude;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationMoonReturn");
            moonReturnAltitude = Bindings.EditableDouble (type, "MoonReturnAltitude");
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
