using System;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.ExternalAPI
{
    sealed class FAR : ModAPI
    {
        public FAR () : base (ServiceInfo.Mods.FAR.Name, ServiceInfo.Mods.FAR.Assembly)
        {
        }

        /// <summary>
        /// Whether FAR is installed, so that it replaces the stock aerodynamics. Throws an
        /// exception if FAR is installed and not available.
        /// </summary>
        public static bool Installed {
            get { return Get<FAR> ().CheckInstalled (); }
        }

        protected override void Bind (ModBinder mod)
        {
            mod.Statics (typeof (FAR), "FerramAerospaceResearch.FARAPI");
            calculateVesselAeroForces = mod.Type ("FerramAerospaceResearch.FARAPI").Method<AeroForces> ("CalculateVesselAeroForces");
            var vesselAero = mod.Type ("FerramAerospaceResearch.FARAeroComponents.FARVesselAero");
            vesselAeroType = vesselAero.Type;
            machNumber = vesselAero.Getter<Component, double> ("MachNumber");
            reynoldsNumber = vesselAero.Getter<Component, double> ("ReynoldsNumber");
        }

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

        static Component VesselAero (Vessel vessel)
        {
            return vesselAeroType == null ? null : vessel.GetComponent (vesselAeroType);
        }

        public static double VesselMachNumber (Vessel vessel)
        {
            return machNumber (VesselAero (vessel));
        }

        public static double VesselReynoldsNumber (Vessel vessel)
        {
            return reynoldsNumber (VesselAero (vessel));
        }

        delegate void AeroForces (
            Vessel vessel, out Vector3 aeroForce, out Vector3 aeroTorque, Vector3 velocityWorldVector, double altitude);

        static AeroForces calculateVesselAeroForces;

        public static void CalculateVesselAeroForces(Vessel vessel, out Vector3 aeroForce, out Vector3 aeroTorque, Vector3 velocityWorldVector, double altitude) {
            calculateVesselAeroForces (vessel, out aeroForce, out aeroTorque, velocityWorldVector, altitude);
        }
    }
}
