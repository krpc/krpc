using System;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    /// <summary>
    /// Reflective access to RealAntennas. Its part module, antenna and link classes derive
    /// from the stock ones, so the stock members keep working on them. The members here are
    /// the link budget and targeting that RealAntennas adds.
    /// </summary>
    static class RealAntennas
    {
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

        public static void Load ()
        {
            IsAvailable = false;
            moduleType = APILoader.Load (typeof(RealAntennas), "RealAntennas", "RealAntennas.ModuleRealAntenna");
            if (moduleType == null)
                return;
            var assembly = moduleType.Assembly;
            var antennaType = assembly.GetType ("RealAntennas.RealAntenna");
            var techLevelType = assembly.GetType ("RealAntennas.TechLevelInfo");
            var bandType = assembly.GetType ("RealAntennas.Antenna.BandInfo");
            var targetType = assembly.GetType ("RealAntennas.Targeting.AntennaTarget");
            linkType = assembly.GetType ("RealAntennas.RACommLink");
            if (antennaType == null || techLevelType == null || bandType == null || targetType == null || linkType == null) {
                Logger.WriteLine ("Load API: RealAntennas types not found; skipping", Logger.Severity.Error);
                return;
            }
            moduleAntenna = Accessor.Getter<PartModule, object> (moduleType, "RAAntenna");
            maxTxPower = Accessor.Getter<PartModule, float> (moduleType, "MaxTxPower");
            maxTechLevel = Accessor.Getter<PartModule, int> (moduleType, "maxTechLevel");
            condition = Accessor.Getter<PartModule, int> (moduleType, "Condition");
            permanentShutdown = Accessor.Method<Action<PartModule, KSPActionParam>> (moduleType, "PermanentShutdownAction");
            gain = Accessor.Getter<object, float> (antennaType, "Gain");
            txPower = Accessor.Getter<object, float> (antennaType, "TxPower");
            dataRate = Accessor.Getter<object, double> (antennaType, "DataRate");
            beamwidth = Accessor.Getter<object, float> (antennaType, "Beamwidth");
            idlePowerDraw = Accessor.Getter<object, float> (antennaType, "IdlePowerDraw");
            powerDrawLinear = Accessor.Getter<object, float> (antennaType, "PowerDrawLinear");
            techLevelInfo = Accessor.Getter<object, object> (antennaType, "TechLevelInfo");
            rfBand = Accessor.Getter<object, object> (antennaType, "RFBand");
            shape = Accessor.Getter<object, int> (antennaType, "Shape");
            toTarget = Accessor.Getter<object, UnityEngine.Vector3> (antennaType, "ToTarget");
            target = Accessor.Getter<object, object> (antennaType, "Target");
            setTarget = Accessor.Setter<object, object> (antennaType, "Target");
            parent = Accessor.Getter<object, PartModule> (antennaType, "Parent");
            level = Accessor.Getter<object, int> (techLevelType, "Level");
            maxDefinedTechLevel = Accessor.StaticGetter<int> (techLevelType, "MaxTL");
            bandName = Accessor.Getter<object, string> (bandType, "name");
            frequency = Accessor.Getter<object, float> (bandType, "Frequency");
            loadTarget = Accessor.Method<Func<ConfigNode, object, object>> (targetType, "LoadFromConfig");
            saveTarget = Accessor.Method<Action<object, ConfigNode>> (targetType, "Save");
            forwardDataRate = Accessor.Getter<CommNet.CommLink, double> (linkType, "FwdDataRate");
            reverseDataRate = Accessor.Getter<CommNet.CommLink, double> (linkType, "RevDataRate");
            forwardTx = Accessor.Getter<CommNet.CommLink, object> (linkType, "FwdAntennaTx");
            forwardRx = Accessor.Getter<CommNet.CommLink, object> (linkType, "FwdAntennaRx");
            IsAvailable =
                moduleAntenna != null && maxTxPower != null && maxTechLevel != null &&
                condition != null && permanentShutdown != null && gain != null &&
                txPower != null && dataRate != null && beamwidth != null &&
                idlePowerDraw != null && powerDrawLinear != null && techLevelInfo != null &&
                rfBand != null && maxDefinedTechLevel != null && shape != null && toTarget != null && target != null &&
                setTarget != null && parent != null && level != null && bandName != null &&
                frequency != null && loadTarget != null && saveTarget != null &&
                forwardDataRate != null && reverseDataRate != null && forwardTx != null &&
                forwardRx != null;
            if (!IsAvailable)
                Logger.WriteLine ("Load API: RealAntennas is installed but does not provide the expected members; skipping", Logger.Severity.Error);
        }

        public static bool IsAvailable { get; private set; }

        public static bool IsModule (PartModule module)
        {
            return IsAvailable && moduleType.IsInstanceOfType (module);
        }

        public static bool IsLink (CommNet.CommLink link)
        {
            return IsAvailable && linkType.IsInstanceOfType (link);
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
