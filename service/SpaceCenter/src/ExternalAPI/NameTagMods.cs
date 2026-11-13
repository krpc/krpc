using KRPC.Utils;

namespace KRPC.SpaceCenter.ExternalAPI
{
    // The mods that add the name tag part module. Part.Tag reads the module by name, so there
    // are no members to bind.

    sealed class KSPCommunityPartModules : ModAPI
    {
        public KSPCommunityPartModules () :
            base (ServiceInfo.Mods.KSPCommunityPartModules.Name, ServiceInfo.Mods.KSPCommunityPartModules.Assembly)
        {
        }

        protected override void Bind (ModBinder mod)
        {
        }
    }

    sealed class KOS : ModAPI
    {
        public KOS () : base (ServiceInfo.Mods.kOS.Name, ServiceInfo.Mods.kOS.Assembly)
        {
        }

        protected override void Bind (ModBinder mod)
        {
        }
    }
}
