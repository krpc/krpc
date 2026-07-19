using System;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class DeployableStateExtensions
    {
        public static DeployableState ToDeployableState (this ModuleDeployablePart.DeployState state)
        {
            switch (state) {
            case ModuleDeployablePart.DeployState.EXTENDED:
                return DeployableState.Deployed;
            case ModuleDeployablePart.DeployState.RETRACTED:
                return DeployableState.Retracted;
            case ModuleDeployablePart.DeployState.EXTENDING:
                return DeployableState.Deploying;
            case ModuleDeployablePart.DeployState.RETRACTING:
                return DeployableState.Retracting;
            case ModuleDeployablePart.DeployState.BROKEN:
                return DeployableState.Broken;
            default:
                throw new ArgumentOutOfRangeException (nameof (state));
            }
        }
    }
}
