using System;
using KRPC.Service.Attributes;

namespace KRPC.MechJeb
{
    /// <summary>
    /// The Translatron window, which holds a speed.
    /// </summary>
    [KRPCClass (Service = "MechJeb")]
    public sealed class Translatron : DisplayModule
    {
        // MechJeb's value of the DIRECT mode, which comes after KeepVertical
        const int direct = 4;

        static Value<double> transSpd;
        static Action<object, int> setMode;
        static Action<object> panicSwitch;

        internal static new void Load ()
        {
            var type = Reflection.Type ("MuMech.MechJebModuleTranslatron");
            transSpd = Reflection.EditableDouble (type, "trans_spd");
            setMode = Reflection.Method<Action<object, int>> (type, "SetMode");
            panicSwitch = Reflection.Method<Action<object>> (type, "PanicSwitch");
        }

        internal Translatron () : base ("MechJebModuleTranslatron")
        {
        }

        static object Thrust {
            get { return MechJeb.ThrustController.Instance; }
        }

        /// <summary>
        /// The speed to hold, in <unit>m/s</unit>.
        /// </summary>
        [KRPCProperty]
        public double TranslationSpeed {
            get { return transSpd.Get (Instance); }
            set {
                transSpd.Set (Instance, value);
                ThrustController.TransSpdAct.Set (Thrust, (float)value);
            }
        }

        /// <summary>
        /// Whether to cancel the horizontal speed.
        /// </summary>
        [KRPCProperty]
        public bool KillHorizontalSpeed {
            get { return ThrustController.TransKillH.Get (Thrust); }
            set { ThrustController.TransKillH.Set (Thrust, value); }
        }

        /// <summary>
        /// The mode. Throws an exception if set to <see cref="TranslatronMode.KeepRelative"/>
        /// or <see cref="TranslatronMode.Direct"/>.
        /// </summary>
        [KRPCProperty]
        public TranslatronMode Mode {
            get {
                var mode = ThrustController.TMode.Get (Thrust);
                return mode == direct ? TranslatronMode.Direct : (TranslatronMode)mode;
            }
            set {
                if (value == TranslatronMode.KeepRelative || value == TranslatronMode.Direct)
                    throw new MJServiceException ("Cannot set TranslatronMode to internal values");
                setMode (Instance, (int)value);
            }
        }

        /// <summary>
        /// Abort: separate all but the last stage and engage the landing autopilot. A call
        /// during an abort cancels it.
        /// </summary>
        [KRPCMethod]
        public void PanicSwitch ()
        {
            panicSwitch (Instance);
        }
    }

    /// <summary>
    /// The mode of Translatron. See <see cref="Translatron.Mode"/>.
    /// </summary>
    [KRPCEnum (Service = "MechJeb")]
    public enum TranslatronMode
    {
        /// <summary>
        /// Off.
        /// </summary>
        Off,

        /// <summary>
        /// Hold the orbital speed.
        /// </summary>
        KeepOrbital,

        /// <summary>
        /// Hold the surface speed.
        /// </summary>
        KeepSurface,

        /// <summary>
        /// Hold the vertical speed.
        /// </summary>
        KeepVertical,

        /// <summary>
        /// Unused. MechJeb 2.15 has no such mode.
        /// </summary>
        KeepRelative,

        /// <summary>
        /// Set by MechJeb's autopilots. Cannot be set.
        /// </summary>
        Direct
    }
}
