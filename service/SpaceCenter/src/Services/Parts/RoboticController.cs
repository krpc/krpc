using System;
using System.Collections.Generic;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A robotic controller. Obtained by calling <see cref="Part.RoboticController"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class RoboticController : Equatable<RoboticController>, IGameObjectState
    {
        ModuleRef controllerRef;

        internal static bool Is(Part part)
        {
            return part.InternalPart.HasModule<Expansions.Serenity.ModuleRoboticController>();
        }

        internal RoboticController(Part part)
        {
            if (!Is(part))
                throw new ArgumentException("Part is not a robotics controller");
            Part = part;
            var internalPart = part.InternalPart;
            controllerRef = ModuleRef.ForType<Expansions.Serenity.ModuleRoboticController> (internalPart);
        }

        Expansions.Serenity.ModuleRoboticController InternalController {
            get { return (Expansions.Serenity.ModuleRoboticController)controllerRef.Get (Part.InternalPart); }
        }

        /// <summary>
        /// What the game holds for the robotic controller: the state of the part
        /// carrying it, or destroyed once that part no longer has the module.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return controllerRef.StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticController other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && controllerRef == other.controllerRef;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode() ^ controllerRef.GetHashCode();
        }

        /// <summary>
        /// The part object for this controller.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the controller is enabled.
        /// </summary>
        [KRPCProperty]
        public bool Enabled
        {
            get { return InternalController.controllerEnabled; }
            set { InternalController.controllerEnabled = value; }
        }

        /// <summary>
        /// Whether the controller's sequence is currently playing.
        /// </summary>
        [KRPCProperty]
        public bool Playing
        {
            get { return InternalController.SequenceIsPlaying; }
        }

        /// <summary>
        /// The current position along the sequence, in seconds.
        /// </summary>
        [KRPCProperty]
        public float Position
        {
            get { return InternalController.SequencePosition; }
            set { InternalController.SetSequencePosition(value); }
        }

        /// <summary>
        /// The length of the sequence, in seconds.
        /// </summary>
        [KRPCProperty]
        public float Length
        {
            get { return InternalController.SequenceLength; }
        }

        /// <summary>
        /// The speed at which the sequence is played back, as a multiple of normal speed.
        /// </summary>
        [KRPCProperty]
        public float PlaySpeed
        {
            get { return InternalController.SequencePlaySpeed; }
        }

        /// <summary>
        /// Start playing the controller's sequence.
        /// </summary>
        [KRPCMethod]
        public void Play()
        {
            InternalController.SequencePlay();
        }

        /// <summary>
        /// Stop playing the controller's sequence.
        /// </summary>
        [KRPCMethod]
        public void Stop()
        {
            InternalController.SequenceStop();
        }

        /// <summary>
        /// Whether the controller has a part.
        /// </summary>
        [KRPCMethod]
        public bool HasPart(Part part)
        {
            return InternalController.HasPart(part.InternalPart);
        }

        /// <summary>
        /// The axes for the controller.
        /// </summary>
        [KRPCMethod]
        public IList<IList<string>> Axes()
        {
            var output = new List<IList<string>>();
            foreach (var axis in InternalController.ControlledAxes)
            {
                output.Add(new List<string>() { axis.Part.name, axis.AxisField.name });
            }
            return output;
        }

        /// <summary>
        /// Add an axis to the controller.
        /// </summary>
        /// <param name="module">The part module that the axis belongs to.</param>
        /// <param name="fieldName">The name of the axis field, as returned by
        /// <see cref="Axes"/>.</param>
        /// <returns>Returns <c>true</c> if the axis is added successfully.</returns>
        [KRPCMethod]
        public bool AddAxis(Module module, string fieldName)
        {
            var internalPart = module.Part.InternalPart;
            var internalModule = internalPart.Modules[module.Name];
            var axisField = internalModule.Fields[fieldName] as BaseAxisField;
            if (axisField == null)
                return false;
            InternalController.AddPartAxis(internalPart, internalModule, axisField);
            return true;
        }

        /// <summary>
        /// Add key frame value for controller axis.
        /// </summary>
        /// <param name="module">The part module that the axis belongs to.</param>
        /// <param name="fieldName">The name of the axis field, as returned by
        /// <see cref="Axes"/>.</param>
        /// <param name="time">The time of the key frame.</param>
        /// <param name="value">The value of the key frame.</param>
        /// <returns>Returns <c>true</c> if the key frame is added successfully.</returns>
        [KRPCMethod]
        public bool AddKeyFrame(Module module, string fieldName, float time, float value)
        {
            var internalPart = module.Part.InternalPart;
            var internalModule = internalPart.Modules[module.Name];

            foreach (var axis in InternalController.ControlledAxes)
            {
                if (internalModule == axis.Module && fieldName == axis.AxisField.name)
                {
                    Expansions.Serenity.ControlledAxis outAxis;
                    InternalController.TryGetPartAxisField(axis.Part, axis.AxisField, out outAxis);
                    if (outAxis != null)
                    {
                        outAxis.timeValue.Curve.AddKey(time, value);
                        return true;
                    }
                    break;
                }
            }
            return false;
        }

        /// <summary>
        /// Clear axis.
        /// </summary>
        /// <param name="module">The part module that the axis belongs to.</param>
        /// <param name="fieldName">The name of the axis field, as returned by
        /// <see cref="Axes"/>.</param>
        /// <returns>Returns <c>true</c> if the axis is cleared successfully.</returns>
        [KRPCMethod]
        public bool ClearAxis(Module module, string fieldName)
        {
            var internalPart = module.Part.InternalPart;
            var internalModule = internalPart.Modules[module.Name];

            foreach (var axis in InternalController.ControlledAxes)
            {
                if (internalModule == axis.Module && fieldName == axis.AxisField.name)
                {
                    Expansions.Serenity.ControlledAxis outAxis;
                    InternalController.TryGetPartAxisField(axis.Part, axis.AxisField, out outAxis);
                    if (outAxis != null)
                    {
                        while (outAxis.timeValue.Curve.keys.Length > 0)
                        {
                            outAxis.timeValue.Curve.RemoveKey(0);
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
