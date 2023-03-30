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
            return !ReferenceEquals(other, null) && Part == other.Part && controller == other.controller;
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
        /// Whether the controller has a part.
        /// </summary>
        [KRPCMethod]
        public bool HasPart(Part part)
        {
            if (ReferenceEquals (part, null))
                throw new ArgumentNullException (nameof (part));
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
        /// <returns>Returns <c>true</c> if the axis is added successfully.</returns>
        [KRPCMethod]
        public bool AddAxis(Module module, string fieldName)
        {
            if (module == null)
                throw new ArgumentNullException (nameof (module));
            var internalPart = module.Part.InternalPart;
            var internalModule = internalPart.Modules[module.Name];
            foreach (var field in internalModule.Fields)
            {
                if (field.guiName == fieldName)
                {
                    var axisField = (BaseAxisField)internalModule.Fields[field.name];
                    if (axisField != null)
                    {
                        controller.AddPartAxis(internalPart, internalModule, axisField);
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Add key frame value for controller axis.
        /// </summary>
        /// <returns>Returns <c>true</c> if the key frame is added successfully.</returns>
        [KRPCMethod]
        public bool AddKeyFrame(Module module, string fieldName, float time, float value)
        {
            if (module == null)
                throw new ArgumentNullException (nameof (module));
            var internalPart = module.Part.InternalPart;
            var internalModule = internalPart.Modules[module.Name];

            foreach (var axis in controller.ControlledAxes)
            {
                if (internalModule == axis.Module && fieldName == axis.AxisField.guiName)
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
        /// <returns>Returns <c>true</c> if the axis is cleared successfully.</returns>
        [KRPCMethod]
        public bool ClearAxis(Module module, string fieldName)
        {
            if (module == null)
                throw new ArgumentNullException (nameof (module));
            var internalPart = module.Part.InternalPart;
            var internalModule = internalPart.Modules[module.Name];

            foreach (var axis in controller.ControlledAxes)
            {
                if (internalModule == axis.Module && fieldName == axis.AxisField.guiName)
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
