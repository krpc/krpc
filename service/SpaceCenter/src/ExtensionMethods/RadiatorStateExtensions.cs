using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class RadiatorStateExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static RadiatorState ToRadiatorState (this ModuleDeployablePart.DeployState state)
        {
            switch (state) {
            case ModuleDeployablePart.DeployState.EXTENDED:
                return RadiatorState.Extended;
            case ModuleDeployablePart.DeployState.RETRACTED:
                return RadiatorState.Retracted;
            case ModuleDeployablePart.DeployState.EXTENDING:
                return RadiatorState.Extending;
            case ModuleDeployablePart.DeployState.RETRACTING:
                return RadiatorState.Retracting;
            case ModuleDeployablePart.DeployState.BROKEN:
                return RadiatorState.Broken;
            default:
                throw new ArgumentOutOfRangeException (nameof (state));
            }
        }
    }
}
