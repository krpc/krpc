using System;
using System.Collections.Generic;
using System.Linq;
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
    public class Engine : Equatable<Engine>, IGameObjectState
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
            return engine.GetHashCode ();
        }

        /// <summary>
        /// The part the engine is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        /// <summary>
        /// The gimbals that steer the engine's nozzles. Empty if the engine has none.
        /// </summary>
        [KRPCProperty]
        public IList<Gimbal> Gimbals {
            get {
                _ = Part.InternalPart;
                return Part.Gimbals.Where (gimbal => gimbal.Steers (engine)).ToList ();
            }
        }

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
        /// The lowest throttle the engine runs at, between 0 and 1.
        /// </summary>
        [KRPCProperty]
        public float MinimumThrottle {
            get {
                _ = Part.InternalPart;
                return engine.MinimumThrottle;
            }
        }

        /// <summary>
        /// The names of the substances the engine burns. See <see cref="Resource.Name"/>.
        /// </summary>
        [KRPCProperty]
        public IList<string> Propellants {
            get {
                _ = Part.InternalPart;
                return engine.Cores.SelectMany (core => core.Reaction.ReactantPhases)
                    .Select (phase => phase.Name).Distinct ().ToList ();
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
        public float Thrust {
            get { return (float)Engines.Performance (Part.InternalVehicle, engine, fullThrottle: false).thrust; }
        }

        /// <summary>
        /// The thrust of the engine at full throttle and the current atmospheric pressure, in
        /// Newtons. Zero when it is inactive or has no propellant.
        /// </summary>
        [KRPCProperty]
        public float AvailableThrust {
            get { return (float)Engines.Performance (Part.InternalVehicle, engine, fullThrottle: true).thrust; }
        }

        /// <summary>
        /// The specific impulse of the engine at the current atmospheric pressure, in seconds.
        /// Zero when it is inactive or has no propellant.
        /// </summary>
        [KRPCProperty]
        public float SpecificImpulse {
            get {
                var (thrust, massFlowRate) = Engines.Performance (Part.InternalVehicle, engine, fullThrottle: true);
                return (float)(massFlowRate > 0 ? thrust / (massFlowRate * Engines.G0) : 0);
            }
        }
    }
}
