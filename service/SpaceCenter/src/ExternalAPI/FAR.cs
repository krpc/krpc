using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
                calculateVesselAeroForces = type.GetMethod(
                    "CalculateVesselAeroForces",
                    BindingFlags.Public | BindingFlags.Static, null,
                    new Type[] {
                        typeof(Vessel),
                        typeof(Vector3).MakeByRefType(),
                        typeof(Vector3).MakeByRefType(),
                        typeof(Vector3),
                        typeof(double)
                    }, null);
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

        public static double VesselMachNumber (Vessel vessel)
        {
            var aero = vessel.GetComponent ("FARVesselAero");
            return (double)aero.GetType ().GetProperty ("MachNumber").GetValue (aero, null);
        }

        public static double VesselReynoldsNumber (Vessel vessel)
        {
            var aero = vessel.GetComponent ("FARVesselAero");
            return (double)aero.GetType ().GetProperty ("ReynoldsNumber").GetValue (aero, null);
        }

        static MethodInfo calculateVesselAeroForces;

        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public static void CalculateVesselAeroForces(Vessel vessel, out Vector3 aeroForce, out Vector3 aeroTorque, Vector3 velocityWorldVector, double altitude) {
            var parameters = new object[] { vessel, Vector3.zero, Vector3.zero, velocityWorldVector, altitude };
            calculateVesselAeroForces.Invoke(null, parameters);
            aeroForce = (Vector3)parameters[1];
            aeroTorque = (Vector3)parameters[2];
        }
    }
}
