using System;
using KRPCSpaceCenter;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class PartStateExtensions
    {
        public static KRPCSpaceCenter.Services.PartState ToPartState (this global::PartStates state)
        {
            switch (state) {
            case global::PartStates.IDLE:
                return KRPCSpaceCenter.Services.PartState.Idle;
            case global::PartStates.ACTIVE:
                return KRPCSpaceCenter.Services.PartState.Active;
            case global::PartStates.DEAD:
                return KRPCSpaceCenter.Services.PartState.Dead;
            default:
                throw new ArgumentException ("Unsupported part state");
            }
        }
    }
}
