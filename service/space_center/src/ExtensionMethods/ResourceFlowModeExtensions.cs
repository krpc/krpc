using System;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class ResourceFlowModeExtensions
    {
        public static Services.ResourceFlowMode ToResourceFlowMode (this ResourceFlowMode mode)
        {
            switch (mode) {
            case ResourceFlowMode.ALL_VESSEL:
                return Services.ResourceFlowMode.Vessel;
            case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                return Services.ResourceFlowMode.Stage;
            case ResourceFlowMode.STACK_PRIORITY_SEARCH:
                return Services.ResourceFlowMode.Adjacent;
            case ResourceFlowMode.NO_FLOW:
                return Services.ResourceFlowMode.None;
            default:
                throw new ArgumentOutOfRangeException (nameof (mode));
            }
        }
    }
}
