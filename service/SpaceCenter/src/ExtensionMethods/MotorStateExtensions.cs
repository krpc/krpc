using System;
using System.Diagnostics.CodeAnalysis;
using KRPC.SpaceCenter.Services.Parts;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class MotorStateExtensions
    {
        [SuppressMessage("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInMethodNameRule")]
        public static MotorState ToMotorState(this ModuleWheels.ModuleWheelMotor.MotorState state)
        {
            switch (state) {
                case ModuleWheels.ModuleWheelMotor.MotorState.Idle:
                    return MotorState.Idle;
                case ModuleWheels.ModuleWheelMotor.MotorState.Running:
                    return MotorState.Running;
                case ModuleWheels.ModuleWheelMotor.MotorState.Disabled:
                    return MotorState.Disabled;
                case ModuleWheels.ModuleWheelMotor.MotorState.Inoperable:
                    return MotorState.Inoperable;
                case ModuleWheels.ModuleWheelMotor.MotorState.NotEnoughResources:
                    return MotorState.NotEnoughResources;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }
    }
}
