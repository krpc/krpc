using System;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class ResourceFlowModeExtensions
    {
        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static KRPC.SpaceCenter.Services.ResourceFlowMode ToResourceFlowMode (this ResourceFlowMode mode)
        {
            switch (mode) {
            case ResourceFlowMode.ALL_VESSEL:
                return KRPC.SpaceCenter.Services.ResourceFlowMode.Vessel;
            case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                return KRPC.SpaceCenter.Services.ResourceFlowMode.Stage;
            case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                return KRPC.SpaceCenter.Services.ResourceFlowMode.Adjacent;
            case ResourceFlowMode.NO_FLOW:
                return KRPC.SpaceCenter.Services.ResourceFlowMode.None;
            default:
                throw new ArgumentOutOfRangeException ("mode");
            }
        }
    }
}
