using System;
using KRPCSpaceCenter.Services.Parts;

namespace KRPCSpaceCenter.ExtensionMethods
{
    public static class PartStateExtensions
    {
        public static PartState ToPartState (this PartStates state)
        {
            switch (state) {
            case PartStates.IDLE:
                return PartState.Idle;
            case PartStates.ACTIVE:
                return PartState.Active;
            case PartStates.DEAD:
                return PartState.Dead;
            default:
                throw new ArgumentException ("Unsupported part state");
            }
        }
    }
}
