using System;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class ParachuteSafeStateExtensions
    {
        public static ParachuteSafeState ToParachuteSafeState (this ModuleParachute.deploymentSafeStates state)
        {
            switch (state) {
            case ModuleParachute.deploymentSafeStates.SAFE:
                return ParachuteSafeState.Safe;
            case ModuleParachute.deploymentSafeStates.RISKY:
                return ParachuteSafeState.Risky;
            case ModuleParachute.deploymentSafeStates.UNSAFE:
                return ParachuteSafeState.Unsafe;
            case ModuleParachute.deploymentSafeStates.NONE:
                return ParachuteSafeState.None;
            default:
                throw new ArgumentOutOfRangeException (nameof (state));
            }
        }
    }
}
