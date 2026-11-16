using System;
using UnityEngine;
using VPWheelCollider = VehiclePhysics.VPWheelCollider;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// The physics state of a wheel or landing leg, shared by <see cref="Wheel"/> and
    /// <see cref="Leg"/>.
    /// </summary>
    static class WheelPhysics
    {
        static KSPWheelController Controller (ModuleWheelBase wheel)
        {
            var controller = wheel.Wheel;
            if (controller == null || controller.wheelCollider == null)
                throw new InvalidOperationException ("Wheel collider is not set up");
            return controller;
        }

        internal static VPWheelCollider Collider (ModuleWheelBase wheel)
        {
            return Controller (wheel).wheelCollider;
        }

        // The game keeps the last grounded value while the wheel is in the air
        internal static float? Spring (ModuleWheelBase wheel)
        {
            var controller = Controller (wheel);
            return controller.IsGrounded ? controller.wheelCollider.springRate : (float?)null;
        }

        internal static float? Damper (ModuleWheelBase wheel)
        {
            var controller = Controller (wheel);
            return controller.IsGrounded ? controller.wheelCollider.damperRate : (float?)null;
        }

        internal static float Load (ModuleWheelBase wheel)
        {
            var state = Controller (wheel).currentState;
            return state == null ? 0f : state.downforce;
        }

        internal static Vector3 ContactPosition (ModuleWheelBase wheel)
        {
            var collider = Collider (wheel);
            if (collider.visualGrounded)
                return collider.visualHit.point;
            // The end of the game's contact ray, the bottom of the wheel at full extension
            var transform = collider.cachedTransform;
            return transform.TransformPoint (collider.center) -
                   transform.up * (collider.suspensionDistance + collider.radius);
        }
    }
}
