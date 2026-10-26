using System;
using KRPC.KSA.Utils;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// An engine. Obtained from <see cref="Part.Engine"/>.
    /// </summary>
    /// <remarks>
    /// An active engine fires when the vehicle's engines are on, at the vehicle's throttle.
    /// See <see cref="Control.EngineOn"/> and <see cref="Control.Throttle"/>.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class Engine : Equatable<Engine>
    {
        readonly EngineController engine;

        internal Engine (Part part, EngineController engine)
        {
            Part = part;
            this.engine = engine;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Engine other)
        {
            return !ReferenceEquals (other, null) && ReferenceEquals (engine, other.engine);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return engine.GetHashCode ();
        }

        /// <summary>
        /// The part the engine is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// Whether the engine is active, as when its sequence is activated. Setting it returns
        /// once the change has applied.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get {
                _ = Part.InternalPart;
                return engine.IsActive;
            }
            set {
                engine.SetIsActive (Part.InternalVehicle, value);
                Waits.Until (() => engine.IsActive == value);
            }
        }

        /// <summary>
        /// Whether the engine has propellant to burn.
        /// </summary>
        [KRPCProperty]
        public bool HasPropellant {
            get { return Engines.State (Part.InternalVehicle, engine).IsPropellantAvailable; }
        }

        /// <summary>
        /// The thrust of the engine at its current throttle and the current atmospheric
        /// pressure, in Newtons. Zero when it is not firing.
        /// </summary>
        [KRPCProperty]
        public double Thrust {
            get { return Engines.Performance (Part.InternalVehicle, engine, fullThrottle: false).thrust; }
        }

        /// <summary>
        /// The thrust of the engine at full throttle and the current atmospheric pressure, in
        /// Newtons. Zero when it is inactive or has no propellant.
        /// </summary>
        [KRPCProperty]
        public double AvailableThrust {
            get { return Engines.Performance (Part.InternalVehicle, engine, fullThrottle: true).thrust; }
        }

        /// <summary>
        /// The specific impulse of the engine at the current atmospheric pressure, in seconds.
        /// Zero when it is inactive or has no propellant.
        /// </summary>
        [KRPCProperty]
        public double SpecificImpulse {
            get {
                var (thrust, massFlowRate) = Engines.Performance (Part.InternalVehicle, engine, fullThrottle: true);
                return massFlowRate > 0 ? thrust / (massFlowRate * Engines.G0) : 0;
            }
        }
    }
}
