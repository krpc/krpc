using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class SolarPanelStateExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static SolarPanelState ToSolarPanelState (this ModuleDeployableSolarPanel.DeployState state)
        {
            switch (state) {
            case ModuleDeployableSolarPanel.DeployState.EXTENDED:
                return SolarPanelState.Extended;
            case ModuleDeployableSolarPanel.DeployState.RETRACTED:
                return SolarPanelState.Retracted;
            case ModuleDeployableSolarPanel.DeployState.EXTENDING:
                return SolarPanelState.Extending;
            case ModuleDeployableSolarPanel.DeployState.RETRACTING:
                return SolarPanelState.Retracting;
            case ModuleDeployableSolarPanel.DeployState.BROKEN:
                return SolarPanelState.Broken;
            default:
                throw new ArgumentOutOfRangeException ("state");
            }
        }
    }
}
