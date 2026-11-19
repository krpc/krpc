using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A power consumer, which uses electrical power, such as a light. Obtained from
    /// <see cref="Part.PowerConsumers"/>.
    /// </summary>
    [KRPCClass (Service = "KSA")]
    public class PowerConsumer : Equatable<PowerConsumer>, IGameObjectState
    {
        readonly global::KSA.PowerConsumer module;

        internal PowerConsumer (Part part, global::KSA.PowerConsumer module)
        {
            Part = part;
            this.module = module;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (PowerConsumer other)
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
        /// The part the power consumer is in.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; }

        ref readonly PowerConsumerState State {
            get {
                if (!ModuleStateful<global::KSA.PowerConsumer, PowerConsumerState, EmptyStruct, EmptyStruct>.TryGetFrom (
                    Part.InternalVehicle.Parts.States, out var states))
                    throw new InvalidOperationException ("The vehicle has no power consumers");
                return ref states.GetState (module);
            }
        }

        /// <summary>
        /// Whether the part is using power.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return State.Active; }
        }

        /// <summary>
        /// The power the part uses, in watts.
        /// </summary>
        [KRPCProperty]
        public float Power {
            get { return State.Consumed.Value (); }
        }

        /// <summary>
        /// Whether the part has a power switch, as a probe core does.
        /// </summary>
        [KRPCProperty]
        public bool HasPowerSwitch {
            get { return module.PowerSwitch; }
        }

        global::KSA.PowerConsumer Switched {
            get {
                if (!module.PowerSwitch)
                    throw new InvalidOperationException ("The part has no power switch");
                return module;
            }
        }

        /// <summary>
        /// The setting of the power switch.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the part has no power switch.
        /// </remarks>
        [KRPCProperty]
        public PowerMode Mode {
            get { return ToMode (Switched.Mode); }
            set { Switched.SetMode (FromMode (value)); }
        }

        /// <summary>
        /// Whether the part goes on standby, drawing no power, while a radiation storm is under
        /// way at the vehicle.
        /// </summary>
        /// <remarks>
        /// The game takes a storm of level 1 or worse on the S scale, as
        /// <see cref="Flight.RadiationStormLevel"/> gives. Throws an exception if the part has
        /// no power switch.
        /// </remarks>
        [KRPCProperty]
        public bool StandbyInStorms {
            get { return Switched.StandbyInStorms; }
            set { Switched.SetStandbyInStorms (value); }
        }

        /// <summary>
        /// The power status of the part.
        /// </summary>
        /// <remarks>
        /// Throws an exception if the part has no power switch.
        /// </remarks>
        [KRPCProperty]
        public PowerStatus Status {
            get {
                switch (Switched.ComputeStatus (Part.InternalVehicle.Parts)) {
                case global::KSA.PowerStatus.Off:
                    return PowerStatus.Off;
                case global::KSA.PowerStatus.Standby:
                    return PowerStatus.Standby;
                case global::KSA.PowerStatus.Rebooting:
                    return PowerStatus.Rebooting;
                case global::KSA.PowerStatus.Low:
                    return PowerStatus.Low;
                case global::KSA.PowerStatus.Out:
                    return PowerStatus.Out;
                case global::KSA.PowerStatus.Failed:
                    return PowerStatus.Failed;
                default:
                    return PowerStatus.On;
                }
            }
        }

        static PowerMode ToMode (PowerSwitchMode mode)
        {
            switch (mode) {
            case PowerSwitchMode.Off:
                return PowerMode.Off;
            case PowerSwitchMode.On:
                return PowerMode.On;
            default:
                return PowerMode.Auto;
            }
        }

        static PowerSwitchMode FromMode (PowerMode mode)
        {
            switch (mode) {
            case PowerMode.Off:
                return PowerSwitchMode.Off;
            case PowerMode.Auto:
                return PowerSwitchMode.Auto;
            case PowerMode.On:
                return PowerSwitchMode.On;
            default:
                throw new ArgumentException ("Unknown power mode");
            }
        }
    }
}
