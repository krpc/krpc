using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class ResourceFlowModeExtensions
    {
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

        public static ResourceFlowMode FromResourceFlowMode (this KRPC.SpaceCenter.Services.ResourceFlowMode mode)
        {
            switch (mode) {
            case KRPC.SpaceCenter.Services.ResourceFlowMode.Vessel:
                return ResourceFlowMode.ALL_VESSEL;
            case KRPC.SpaceCenter.Services.ResourceFlowMode.Stage:
                return ResourceFlowMode.STAGE_PRIORITY_FLOW;
            case KRPC.SpaceCenter.Services.ResourceFlowMode.Adjacent:
                return ResourceFlowMode.STACK_PRIORITY_SEARCH;
            case KRPC.SpaceCenter.Services.ResourceFlowMode.None:
                return ResourceFlowMode.NO_FLOW;
            default:
                throw new ArgumentOutOfRangeException ("mode");
            }
        }
    }
}
