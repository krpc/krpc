using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// The avionics of a probe core, which radiation wears out and upsets. Obtained from
    /// <see cref="Part.Avionics"/> or <see cref="Vehicle.Avionics"/>.
    /// </summary>
    /// <remarks>
    /// The avionics take a dose only while <see cref="KSA.RadiationEnabled"/> is true. Switched
    /// off or on standby, they take half the dose and no upsets.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Avionics : Equatable<Avionics>, IGameObjectState
    {
        readonly global::KSA.Avionics module;

        internal Avionics (Part part, global::KSA.Avionics module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Avionics other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (module, other.module);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Part.GameObjectState; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return module.GetHashCode ();
        }

        /// <summary>
        /// The part the avionics are in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// The health of the avionics.
        /// </summary>
        [KRPCProperty]
        public AvionicsHealth Health {
            get {
                switch (module.ComputeHealth ()) {
                case global::KSA.AvionicsHealth.Failed:
                    return AvionicsHealth.Failed;
                case global::KSA.AvionicsHealth.Worn:
                    return AvionicsHealth.Worn;
                default:
                    return AvionicsHealth.Ok;
                }
            }
        }

        /// <summary>
        /// The radiation dose the avionics have taken, in grays of silicon.
        /// </summary>
        [KRPCProperty]
        public float Dose {
            get { return (float)module.DoseGy; }
        }

        /// <summary>
        /// The dose at which the avionics fail, in grays of silicon.
        /// </summary>
        [KRPCProperty]
        public float Tolerance {
            get { return (float)module.ToleranceGy; }
        }

        /// <summary>
        /// The rate at which the avionics take a dose, in grays of silicon per second,
        /// averaged over about an hour. Not a number until the avionics have taken a dose.
        /// </summary>
        [KRPCProperty]
        public float DoseRate {
            get { return (float)module.DoseRate; }
        }

        /// <summary>
        /// The time until the avionics fail at <see cref="DoseRate"/>, in seconds. Infinite
        /// while they take no dose, and zero once they have failed.
        /// </summary>
        [KRPCProperty]
        public float TimeLeft {
            get { return (float)module.ComputeSecondsLeft (); }
        }

        /// <summary>
        /// Whether the avionics are rebooting after a radiation upset.
        /// </summary>
        [KRPCProperty]
        public bool Rebooting {
            get { return module.IsRebooting (Universe.GetElapsedSeconds ()); }
        }
    }
}
