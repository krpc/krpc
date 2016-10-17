using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class RadiatorStateExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static RadiatorState ToRadiatorState (this ModuleDeployableRadiator.DeployState state)
        {
            switch (state) {
            case ModuleDeployableRadiator.DeployState.EXTENDED:
                return RadiatorState.Extended;
            case ModuleDeployableRadiator.DeployState.RETRACTED:
                return RadiatorState.Retracted;
            case ModuleDeployableRadiator.DeployState.EXTENDING:
                return RadiatorState.Extending;
            case ModuleDeployableRadiator.DeployState.RETRACTING:
                return RadiatorState.Retracting;
            case ModuleDeployableRadiator.DeployState.BROKEN:
                return RadiatorState.Broken;
            default:
                throw new ArgumentOutOfRangeException ("state");
            }
        }
    }
}
