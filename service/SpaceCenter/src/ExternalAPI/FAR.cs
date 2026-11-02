using System;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.ExternalAPI
{
    static class FAR
    {
        public static void Load ()
        {
            var type = APILoader.Load( typeof(FAR), "FerramAerospaceResearch", "FerramAerospaceResearch.FARAPI", new Version (0, 15));
            IsAvailable = (type != null);
            if (IsAvailable) {
                calculateVesselAeroForces = Accessor.Method<AeroForces> (type, "CalculateVesselAeroForces");
                vesselAeroType = type.Assembly.GetType ("FerramAerospaceResearch.FARAeroComponents.FARVesselAero");
                if (vesselAeroType != null) {
                    machNumber = Accessor.Getter<Component, double> (vesselAeroType, "MachNumber");
                    reynoldsNumber = Accessor.Getter<Component, double> (vesselAeroType, "ReynoldsNumber");
                }
                if (machNumber == null || reynoldsNumber == null)
                    KRPC.Utils.Logger.WriteLine (
                        "FAR vessel Mach and Reynolds numbers not found; they are unavailable",
                        KRPC.Utils.Logger.Severity.Warning);
            }
        }

        public static bool IsAvailable { get; private set; }

        public static Func<Vessel, double> VesselDynPres { get; internal set; }

        public static Func<Vessel, double> VesselLiftCoeff { get; internal set; }

        public static Func<Vessel, double> VesselDragCoeff { get; internal set; }

        public static Func<Vessel, double> VesselRefArea { get; internal set; }

        public static Func<Vessel, double> VesselTermVelEst { get; internal set; }

        public static Func<Vessel, double> VesselBallisticCoeff { get; internal set; }

        public static Func<Vessel, double> VesselAoA { get; internal set; }

        public static Func<Vessel, double> VesselSideslip { get; internal set; }

        public static Func<Vessel, double> VesselTSFC { get; internal set; }

        public static Func<Vessel, double> VesselStallFrac { get; internal set; }

        /// <summary>
        /// The total aerodynamic force FAR applied to the vessel on the last physics frame,
        /// in world space, in kilonewtons. It is the sum of the per-part forces of FAR's own
        /// aerodynamics model, so it carries the direction those forces actually act in.
        /// Zero for a vessel FAR is not tracking.
        /// </summary>
        public static Func<Vessel, Vector3> VesselAerodynamicForce { get; internal set; }

        /// <summary>
        /// The total aerodynamic torque FAR applied to the vessel about its center of mass on
        /// the last physics frame, in world space, in kilonewton-meters. Zero for a vessel FAR
        /// is not tracking.
        /// </summary>
        public static Func<Vessel, Vector3> VesselAerodynamicTorque { get; internal set; }

        static Type vesselAeroType;
        static Func<Component, double> machNumber;
        static Func<Component, double> reynoldsNumber;

        public static double VesselMachNumber (Vessel vessel)
        {
            return machNumber (vessel.GetComponent (vesselAeroType));
        }

        public static double VesselReynoldsNumber (Vessel vessel)
        {
            return reynoldsNumber (vessel.GetComponent (vesselAeroType));
        }

        delegate void AeroForces (
            Vessel vessel, out Vector3 aeroForce, out Vector3 aeroTorque, Vector3 velocityWorldVector, double altitude);

        static AeroForces calculateVesselAeroForces;

        public static void CalculateVesselAeroForces(Vessel vessel, out Vector3 aeroForce, out Vector3 aeroTorque, Vector3 velocityWorldVector, double altitude) {
            calculateVesselAeroForces (vessel, out aeroForce, out aeroTorque, velocityWorldVector, altitude);
        }
    }
}
