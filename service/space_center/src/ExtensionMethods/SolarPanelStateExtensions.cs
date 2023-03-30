using System;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class SolarPanelStateExtensions
    {
        public static SolarPanelState ToSolarPanelState (this ModuleDeployablePart.DeployState state)
        {
            switch (state) {
            case ModuleDeployablePart.DeployState.EXTENDED:
                return SolarPanelState.Extended;
            case ModuleDeployablePart.DeployState.RETRACTED:
                return SolarPanelState.Retracted;
            case ModuleDeployablePart.DeployState.EXTENDING:
                return SolarPanelState.Extending;
            case ModuleDeployablePart.DeployState.RETRACTING:
                return SolarPanelState.Retracting;
            case ModuleDeployablePart.DeployState.BROKEN:
                return SolarPanelState.Broken;
            default:
                throw new ArgumentOutOfRangeException (nameof (state));
            }
        }
    }
}
