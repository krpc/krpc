using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class ResourceFlowModeExtensions
    {
        public static KRPC.SpaceCenter.Services.ResourceFlowMode ToResourceFlowMode (this global::ResourceFlowMode mode)
        {
            switch (mode) {
            case global::ResourceFlowMode.ALL_VESSEL:
                return KRPC.SpaceCenter.Services.ResourceFlowMode.Vessel;
            case global::ResourceFlowMode.STAGE_PRIORITY_FLOW:
                return KRPC.SpaceCenter.Services.ResourceFlowMode.Stage;
            case global::ResourceFlowMode.STACK_PRIORITY_SEARCH:
                return KRPC.SpaceCenter.Services.ResourceFlowMode.Adjacent;
            case global::ResourceFlowMode.NO_FLOW:
                return KRPC.SpaceCenter.Services.ResourceFlowMode.None;
            default:
                throw new ArgumentException ("Unsupported resource flow mode");
            }
        }

        public static global::ResourceFlowMode FromResourceFlowMode (this KRPC.SpaceCenter.Services.ResourceFlowMode mode)
        {
            switch (mode) {
            case KRPC.SpaceCenter.Services.ResourceFlowMode.Vessel:
                return global::ResourceFlowMode.ALL_VESSEL;
            case KRPC.SpaceCenter.Services.ResourceFlowMode.Stage:
                return global::ResourceFlowMode.STAGE_PRIORITY_FLOW;
            case KRPC.SpaceCenter.Services.ResourceFlowMode.Adjacent:
                return global::ResourceFlowMode.STACK_PRIORITY_SEARCH;
            case KRPC.SpaceCenter.Services.ResourceFlowMode.None:
                return global::ResourceFlowMode.NO_FLOW;
            default:
                throw new ArgumentException ("Unsupported resource flow mode");
            }
        }
    }
}
