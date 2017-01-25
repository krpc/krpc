using System;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A parachute. Obtained by calling <see cref="Part.Parachute"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class Parachute : Equatable<Parachute>
    {
        readonly ModuleParachute parachute;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleParachute> ();
        }

        internal Parachute (Part part)
        {
            Part = part;
            parachute = part.InternalPart.Module<ModuleParachute> ();
            if (parachute == null)
                throw new ArgumentException ("Part is not a parachute");
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Parachute other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part && parachute.Equals (other.parachute);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode () ^ parachute.GetHashCode ();
        }

        /// <summary>
        /// The part object for this parachute.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Deploys the parachute. This has no effect if the parachute has already
        /// been deployed.
        /// </summary>
        [KRPCMethod]
        public void Deploy ()
        {
            parachute.Deploy ();
        }

        /// <summary>
        /// Whether the parachute has been deployed.
        /// </summary>
        [KRPCProperty]
        public bool Deployed {
            get { return parachute.deploymentState != ModuleParachute.deploymentStates.STOWED; }
        }

        /// <summary>
        /// The current state of the parachute.
        /// </summary>
        [KRPCProperty]
        public ParachuteState State {
            get { return parachute.deploymentState.ToParachuteState (); }
        }

        /// <summary>
        /// The altitude at which the parachute will full deploy, in meters.
        /// </summary>
        [KRPCProperty]
        public float DeployAltitude {
            get { return parachute.deployAltitude; }
            set { parachute.deployAltitude = value; }
        }

        /// <summary>
        /// The minimum pressure at which the parachute will semi-deploy, in atmospheres.
        /// </summary>
        [KRPCProperty]
        public float DeployMinPressure {
            get { return parachute.minAirPressureToOpen; }
            set { parachute.minAirPressureToOpen = value; }
        }

        // TODO: add safe deployment information?
    }
}
