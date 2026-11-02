using System;
using Expansions.Serenity;
using KRPC.Utils;

namespace KRPC.SpaceCenter.ExtensionMethods
{
    static class BaseServoExtensions
    {
        static readonly Func<BaseServo, bool> isMoving =
            Accessor.Method<Func<BaseServo, bool>> (typeof (BaseServo), "IsMoving");
        static readonly Func<BaseServo, float> currentTransformAngle =
            Accessor.Method<Func<BaseServo, float>> (typeof (BaseServo), "currentTransformAngle");
        static readonly Func<BaseServo, float> transformRateOfMotion =
            Accessor.Getter<BaseServo, float> ("transformRateOfMotion");

        /// <summary>
        /// Whether the servo is moving.
        /// </summary>
        public static bool IsMoving (this BaseServo servo)
        {
            return isMoving (servo);
        }

        /// <summary>
        /// The live angle of the servo's moving transform, in degrees.
        /// </summary>
        public static float CurrentTransformAngle (this BaseServo servo)
        {
            return currentTransformAngle (servo);
        }

        /// <summary>
        /// The live rate of motion of the servo's moving transform.
        /// </summary>
        public static float TransformRateOfMotion (this BaseServo servo)
        {
            return transformRateOfMotion (servo);
        }
    }
}
