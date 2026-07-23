using System;
using System.Collections.Generic;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using UnityEngine;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A robotic controller. Obtained by calling <see cref="Part.RoboticController"/>.
    /// </summary>
    [KRPCClass(Service = "SpaceCenter")]
    public class RoboticController : Equatable<RoboticController>
    {
        readonly Expansions.Serenity.ModuleRoboticController controller;

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
            controller = internalPart.Module<Expansions.Serenity.ModuleRoboticController>();
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals(RoboticController other)
        {
            return !ReferenceEquals(other, null) && Part == other.Part && controller.Equals(other.controller);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Part.GetHashCode() ^ controller.GetHashCode();
        }

        /// <summary>
        /// The part object for this controller.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the controller is enabled.
        /// </summary>
        [KRPCProperty]
        public bool Enabled
        {
            get { return controller.controllerEnabled; }
            set { controller.controllerEnabled = value; }
        }

        /// <summary>
        /// Whether the controller's sequence is currently playing.
        /// </summary>
        [KRPCProperty]
        public bool Playing
        {
            get { return controller.SequenceIsPlaying; }
        }

        /// <summary>
        /// The current position along the sequence, in seconds.
        /// </summary>
        [KRPCProperty]
        public float Position
        {
            get { return controller.SequencePosition; }
            set { controller.SetSequencePosition(value); }
        }

        /// <summary>
        /// The length of the sequence, in seconds.
        /// </summary>
        [KRPCProperty]
        public float Length
        {
            get { return controller.SequenceLength; }
        }

        /// <summary>
        /// The speed at which the sequence is played back, as a multiple of normal speed.
        /// </summary>
        [KRPCProperty]
        public float PlaySpeed
        {
            get { return controller.SequencePlaySpeed; }
        }

        /// <summary>
        /// Start playing the controller's sequence.
        /// </summary>
        [KRPCMethod]
        public void Play()
        {
            controller.SequencePlay();
        }

        /// <summary>
        /// Stop playing the controller's sequence.
        /// </summary>
        [KRPCMethod]
        public void Stop()
        {
            controller.SequenceStop();
        }

        /// <summary>
        /// Whether the controller has a part.
        /// </summary>
        [KRPCMethod]
        public bool HasPart(Part part)
        {
            return controller.HasPart(part.InternalPart);
        }

        /// <summary>
        /// The axes for the controller.
        /// </summary>
        [KRPCMethod]
        public IList<IList<string>> Axes()
        {
            var output = new List<IList<string>>();
            foreach (var axis in controller.ControlledAxes)
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
            controller.AddPartAxis(internalPart, internalModule, axisField);
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

            foreach (var axis in controller.ControlledAxes)
            {
                if (internalModule == axis.Module && fieldName == axis.AxisField.name)
                {
                    Expansions.Serenity.ControlledAxis outAxis;
                    controller.TryGetPartAxisField(axis.Part, axis.AxisField, out outAxis);
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

            foreach (var axis in controller.ControlledAxes)
            {
                if (internalModule == axis.Module && fieldName == axis.AxisField.name)
                {
                    Expansions.Serenity.ControlledAxis outAxis;
                    controller.TryGetPartAxisField(axis.Part, axis.AxisField, out outAxis);
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
