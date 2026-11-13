using System;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    /// <summary>
    /// Reflective access to RealAntennas. Its part module, antenna and link classes derive
    /// from the stock ones, so the stock members keep working on them. The members here are
    /// the link budget and targeting that RealAntennas adds.
    /// </summary>
    sealed class RealAntennas : ModAPI
    {
        public RealAntennas () : base (ServiceInfo.Mods.RealAntennas.Name, ServiceInfo.Mods.RealAntennas.Assembly)
        {
        }

        // ModuleRealAntenna
        static Type moduleType;
        static Func<PartModule, object> moduleAntenna;
        static Func<PartModule, float> maxTxPower;
        static Func<PartModule, int> maxTechLevel;
        static Func<PartModule, int> condition;
        static Action<PartModule, KSPActionParam> permanentShutdown;
        // RealAntenna
        static Func<object, float> gain;
        static Func<object, float> txPower;
        static Func<object, double> dataRate;
        static Func<object, float> beamwidth;
        static Func<object, float> idlePowerDraw;
        static Func<object, float> powerDrawLinear;
        static Func<object, object> techLevelInfo;
        static Func<object, object> rfBand;
        static Func<object, int> shape;
        static Func<object, UnityEngine.Vector3> toTarget;
        static Func<object, object> target;
        static Action<object, object> setTarget;
        static Func<object, PartModule> parent;
        // TechLevelInfo and BandInfo
        static Func<object, int> level;
        static Func<int> maxDefinedTechLevel;
        static Func<object, string> bandName;
        static Func<object, float> frequency;
        // AntennaTarget
        static Func<ConfigNode, object, object> loadTarget;
        static Action<object, ConfigNode> saveTarget;
        // RACommLink
        static Type linkType;
        static Func<CommNet.CommLink, double> forwardDataRate;
        static Func<CommNet.CommLink, double> reverseDataRate;
        static Func<CommNet.CommLink, object> forwardTx;
        static Func<CommNet.CommLink, object> forwardRx;

        protected override void Bind (ModBinder mod)
        {
            var module = mod.Type ("RealAntennas.ModuleRealAntenna");
            var antenna = mod.Type ("RealAntennas.RealAntenna");
            var techLevel = mod.Type ("RealAntennas.TechLevelInfo");
            var band = mod.Type ("RealAntennas.Antenna.BandInfo");
            var targetBinder = mod.Type ("RealAntennas.Targeting.AntennaTarget");
            var link = mod.Type ("RealAntennas.RACommLink");
            moduleType = module.Type;
            linkType = link.Type;
            moduleAntenna = module.Getter<PartModule, object> ("RAAntenna");
            maxTxPower = module.Getter<PartModule, float> ("MaxTxPower");
            maxTechLevel = module.Getter<PartModule, int> ("maxTechLevel");
            condition = module.Getter<PartModule, int> ("Condition");
            permanentShutdown = module.Method<Action<PartModule, KSPActionParam>> ("PermanentShutdownAction");
            gain = antenna.Getter<object, float> ("Gain");
            txPower = antenna.Getter<object, float> ("TxPower");
            dataRate = antenna.Getter<object, double> ("DataRate");
            beamwidth = antenna.Getter<object, float> ("Beamwidth");
            idlePowerDraw = antenna.Getter<object, float> ("IdlePowerDraw");
            powerDrawLinear = antenna.Getter<object, float> ("PowerDrawLinear");
            techLevelInfo = antenna.Getter<object, object> ("TechLevelInfo");
            rfBand = antenna.Getter<object, object> ("RFBand");
            shape = antenna.Getter<object, int> ("Shape");
            toTarget = antenna.Getter<object, UnityEngine.Vector3> ("ToTarget");
            target = antenna.Getter<object, object> ("Target");
            setTarget = antenna.Setter<object, object> ("Target");
            parent = antenna.Getter<object, PartModule> ("Parent");
            level = techLevel.Getter<object, int> ("Level");
            maxDefinedTechLevel = techLevel.StaticGetter<int> ("MaxTL");
            bandName = band.Getter<object, string> ("name");
            frequency = band.Getter<object, float> ("Frequency");
            loadTarget = targetBinder.Method<Func<ConfigNode, object, object>> ("LoadFromConfig");
            saveTarget = targetBinder.Method<Action<object, ConfigNode>> ("Save");
            forwardDataRate = link.Getter<CommNet.CommLink, double> ("FwdDataRate");
            reverseDataRate = link.Getter<CommNet.CommLink, double> ("RevDataRate");
            forwardTx = link.Getter<CommNet.CommLink, object> ("FwdAntennaTx");
            forwardRx = link.Getter<CommNet.CommLink, object> ("FwdAntennaRx");
        }

        public static bool IsModule (PartModule module)
        {
            return moduleType != null && moduleType.IsInstanceOfType (module);
        }

        public static bool IsLink (CommNet.CommLink link)
        {
            return linkType != null && linkType.IsInstanceOfType (link);
        }

        // ModuleRealAntenna

        public static object Antenna (PartModule module)
        {
            return moduleAntenna (module);
        }

        public static float MaxTxPower (PartModule module)
        {
            return maxTxPower (module);
        }

        /// <summary>
        /// The highest tech level the module allows that RealAntennas defines. RealAntennas
        /// clamps a higher setting to the highest it defines.
        /// </summary>
        public static int MaxTechLevel (PartModule module)
        {
            return Math.Min (maxTechLevel (module), maxDefinedTechLevel ());
        }

        /// <summary>
        /// Whether the module's AntennaCondition is Enabled, its lowest flag.
        /// </summary>
        public static bool Enabled (PartModule module)
        {
            return condition (module) == 1;
        }

        public static void PermanentShutdown (PartModule module)
        {
            permanentShutdown (module, null);
        }

        // RealAntenna

        public static float Gain (object antenna)
        {
            return gain (antenna);
        }

        public static float TxPower (object antenna)
        {
            return txPower (antenna);
        }

        public static double DataRate (object antenna)
        {
            return dataRate (antenna);
        }

        public static float Beamwidth (object antenna)
        {
            return beamwidth (antenna);
        }

        /// <summary>
        /// The idle power draw in kilowatts, which is electric charge per second.
        /// </summary>
        public static float IdlePowerDraw (object antenna)
        {
            return idlePowerDraw (antenna);
        }

        /// <summary>
        /// The power the transmitter draws to transmit, on top of the idle draw, in milliwatts.
        /// </summary>
        public static float TransmitPowerDraw (object antenna)
        {
            return powerDrawLinear (antenna);
        }

        public static int TechLevel (object antenna)
        {
            return level (techLevelInfo (antenna));
        }

        public static string Band (object antenna)
        {
            return bandName (rfBand (antenna));
        }

        public static float Frequency (object antenna)
        {
            return frequency (rfBand (antenna));
        }

        /// <summary>
        /// Whether the antenna is a dish, which RealAntennas points at a target. Its own
        /// CanTarget also requires a target to be set already.
        /// </summary>
        public static bool IsDish (object antenna)
        {
            // AntennaShape.Omni
            return shape (antenna) != 1;
        }

        /// <summary>
        /// The world space vector from the antenna to its target.
        /// </summary>
        public static UnityEngine.Vector3 ToTarget (object antenna)
        {
            return toTarget (antenna);
        }

        /// <summary>
        /// The antenna's target in the TARGET node format RealAntennas saves it in, or null if
        /// it has none.
        /// </summary>
        public static ConfigNode Target (object antenna)
        {
            var t = target (antenna);
            if (t == null)
                return null;
            var node = new ConfigNode ();
            saveTarget (t, node);
            return node.GetNode ("TARGET");
        }

        /// <summary>
        /// Point the antenna at the target in a TARGET node, as the targeting window does.
        /// </summary>
        public static void SetTarget (object antenna, ConfigNode node)
        {
            setTarget (antenna, loadTarget (node, antenna));
        }

        /// <summary>
        /// The part module of an antenna, or null for the antenna of a ground station or of
        /// an unloaded vessel.
        /// </summary>
        public static PartModule Module (object antenna)
        {
            return antenna == null ? null : parent (antenna);
        }

        // RACommLink

        public static double ForwardDataRate (CommNet.CommLink link)
        {
            return forwardDataRate (link);
        }

        public static double ReverseDataRate (CommNet.CommLink link)
        {
            return reverseDataRate (link);
        }

        public static object ForwardTransmitter (CommNet.CommLink link)
        {
            return forwardTx (link);
        }

        public static object ForwardReceiver (CommNet.CommLink link)
        {
            return forwardRx (link);
        }
    }
}
