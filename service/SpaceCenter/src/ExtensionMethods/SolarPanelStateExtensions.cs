using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class SolarPanelStateExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static SolarPanelState ToSolarPanelState (this ModuleDeployableSolarPanel.panelStates state)
        {
            switch (state) {
            case ModuleDeployableSolarPanel.panelStates.EXTENDED:
                return SolarPanelState.Extended;
            case ModuleDeployableSolarPanel.panelStates.RETRACTED:
                return SolarPanelState.Retracted;
            case ModuleDeployableSolarPanel.panelStates.EXTENDING:
                return SolarPanelState.Extending;
            case ModuleDeployableSolarPanel.panelStates.RETRACTING:
                return SolarPanelState.Retracting;
            case ModuleDeployableSolarPanel.panelStates.BROKEN:
                return SolarPanelState.Broken;
            default:
                throw new ArgumentOutOfRangeException ("state");
            }
        }
    }
}
