using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class RadiatorStateExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static RadiatorState ToRadiatorState (this ModuleDeployableRadiator.panelStates state)
        {
            switch (state) {
            case ModuleDeployableRadiator.panelStates.EXTENDED:
                return RadiatorState.Extended;
            case ModuleDeployableRadiator.panelStates.RETRACTED:
                return RadiatorState.Retracted;
            case ModuleDeployableRadiator.panelStates.EXTENDING:
                return RadiatorState.Extending;
            case ModuleDeployableRadiator.panelStates.RETRACTING:
                return RadiatorState.Retracting;
            case ModuleDeployableRadiator.panelStates.BROKEN:
                return RadiatorState.Broken;
            default:
                throw new ArgumentOutOfRangeException ("state");
            }
        }
    }
}
