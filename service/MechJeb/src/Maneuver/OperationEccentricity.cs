using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.MechJeb.Maneuver
{
    /// <summary>
    /// Change the eccentricity.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class OperationEccentricity : TimedOperation
    {
        static Func<object, object> timeSelector;
        static Field<object, double> newEccentricity;

        internal static new void Load ()
        {
            var type = Bindings.Type ("MuMech.OperationEccentricity");
            timeSelector = TimeSelectorField (type);
            newEccentricity = Bindings.EditableDouble (type, "NewEcc");
        }

        internal OperationEccentricity () : base ("OperationEccentricity")
        {
        }

        internal override object TimeSelectorInstance {
            get { return timeSelector (Instance); }
        }

        /// <summary>
        /// The new eccentricity.
        /// </summary>
        [KRPCProperty]
        public double NewEccentricity {
            get { return newEccentricity.Get (Instance); }
            set { newEccentricity.Set (Instance, value); }
        }
    }
}
