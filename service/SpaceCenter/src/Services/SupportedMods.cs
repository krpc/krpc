using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services
{
    // Whether each mod the service supports is installed
    public static partial class SpaceCenter
    {
        /// <summary>
        /// Whether Ferram Aerospace Research is installed.
        /// </summary>
        [KRPCProperty]
        public static bool FARAvailable {
            get { return ExternalAPI.FAR.IsAvailable; }
        }

        /// <summary>
        /// Whether Action Groups Extended is installed.
        /// </summary>
        [KRPCProperty]
        public static bool AGExtAvailable {
            get { return ExternalAPI.AGExt.IsAvailable; }
        }

        /// <summary>
        /// Whether RealFuels is installed.
        /// </summary>
        [KRPCProperty]
        public static bool RealFuelsAvailable {
            get { return ExternalAPI.RealFuels.IsAvailable; }
        }

        /// <summary>
        /// Whether RealChute is installed.
        /// </summary>
        [KRPCProperty]
        public static bool RealChuteAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.RealChute.Assembly); }
        }

        /// <summary>
        /// Whether Procedural Fairings is installed.
        /// </summary>
        [KRPCProperty]
        public static bool ProceduralFairingsAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.ProceduralFairings.Assembly); }
        }

        /// <summary>
        /// Whether Near Future Solar is installed.
        /// </summary>
        [KRPCProperty]
        public static bool NearFutureSolarAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.NearFutureSolar.Assembly); }
        }

        /// <summary>
        /// Whether DMagic's DMModuleScienceAnimateGeneric is installed.
        /// </summary>
        [KRPCProperty]
        public static bool DMagicAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.DMagic.Assembly); }
        }

        /// <summary>
        /// Whether Firespitter is installed.
        /// </summary>
        [KRPCProperty]
        public static bool FirespitterAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.Firespitter.Assembly); }
        }

        /// <summary>
        /// Whether KerbalActuators is installed.
        /// </summary>
        [KRPCProperty]
        public static bool KerbalActuatorsAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.KerbalActuators.Assembly); }
        }

        /// <summary>
        /// Whether KSPCommunityPartModules is installed.
        /// </summary>
        [KRPCProperty]
        public static bool KSPCommunityPartModulesAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.KSPCommunityPartModules.Assembly); }
        }

        /// <summary>
        /// Whether kOS is installed.
        /// </summary>
        [KRPCProperty]
        public static bool KOSAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.kOS.Assembly); }
        }

        /// <summary>
        /// Whether B9PartSwitch is installed.
        /// </summary>
        [KRPCProperty]
        public static bool B9PartSwitchAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.B9PartSwitch.Assembly); }
        }

        /// <summary>
        /// Whether TweakScale Rescaled is installed.
        /// </summary>
        [KRPCProperty]
        public static bool TweakScaleRescaledAvailable {
            get { return ModAssembly.IsLoaded (ServiceInfo.Mods.TweakScaleRescaled.Assembly); }
        }

        /// <summary>
        /// Whether RealAntennas is installed.
        /// </summary>
        [KRPCProperty]
        public static bool RealAntennasAvailable {
            get { return ExternalAPI.RealAntennas.IsAvailable; }
        }
    }
}
