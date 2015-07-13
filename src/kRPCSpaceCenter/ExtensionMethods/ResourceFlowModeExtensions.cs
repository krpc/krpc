using System;

namespace KRPCSpaceCenter.ExtensionMethods
{
    static class ResourceFlowModeExtensions
    {
        public static KRPCSpaceCenter.Services.ResourceFlowMode ToResourceFlowMode (this global::ResourceFlowMode mode)
        {
            switch (mode) {
            case global::ResourceFlowMode.ALL_VESSEL:
                return KRPCSpaceCenter.Services.ResourceFlowMode.Vessel;
            case global::ResourceFlowMode.STAGE_PRIORITY_FLOW:
                return KRPCSpaceCenter.Services.ResourceFlowMode.Stage;
            case global::ResourceFlowMode.STACK_PRIORITY_SEARCH:
                return KRPCSpaceCenter.Services.ResourceFlowMode.Adjacent;
            case global::ResourceFlowMode.NO_FLOW:
                return KRPCSpaceCenter.Services.ResourceFlowMode.None;
            default:
                throw new ArgumentException ("Unsupported resource flow mode");
            }
        }

        public static global::ResourceFlowMode FromResourceFlowMode (this KRPCSpaceCenter.Services.ResourceFlowMode mode)
        {
            switch (mode) {
            case KRPCSpaceCenter.Services.ResourceFlowMode.Vessel:
                return global::ResourceFlowMode.ALL_VESSEL;
            case KRPCSpaceCenter.Services.ResourceFlowMode.Stage:
                return global::ResourceFlowMode.STAGE_PRIORITY_FLOW;
            case KRPCSpaceCenter.Services.ResourceFlowMode.Adjacent:
                return global::ResourceFlowMode.STACK_PRIORITY_SEARCH;
            case KRPCSpaceCenter.Services.ResourceFlowMode.None:
                return global::ResourceFlowMode.NO_FLOW;
            default:
                throw new ArgumentException ("Unsupported resource flow mode");
            }
        }
    }
}
