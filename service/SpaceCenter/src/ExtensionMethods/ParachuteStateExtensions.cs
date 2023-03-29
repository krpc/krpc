using System;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class ParachuteStateExtensions
    {
        public static ParachuteState ToParachuteState (this ModuleParachute.deploymentStates state)
        {
            switch (state) {
            case ModuleParachute.deploymentStates.ACTIVE:
                return ParachuteState.Armed;
            case ModuleParachute.deploymentStates.CUT:
                return ParachuteState.Cut;
            case ModuleParachute.deploymentStates.DEPLOYED:
                return ParachuteState.Deployed;
            case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                return ParachuteState.SemiDeployed;
            case ModuleParachute.deploymentStates.STOWED:
                return ParachuteState.Stowed;
            default:
                throw new ArgumentOutOfRangeException (nameof (state));
            }
        }
    }
}
