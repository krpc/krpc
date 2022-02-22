using System;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
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
        /// The part object for this antenna.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// Whether the controller any axisfield from the part
        /// </summary>
        [KRPCMethod]
        public bool HasPart(Part part)
        {
            return controller.HasPart(part.InternalPart);
        }

        /// <summary>
        /// List the axes for the controller.
        /// </summary>
        [KRPCMethod]
        public IList<IList<string>> ListAxes()
        {
            IList<IList<string>> output = new List<IList<string>>();
            foreach (var axis in controller.ControlledAxes)
            {
                IList<string> data = new List<string>() { axis.Part.name, axis.AxisField.name };
                output.Add(data);
            }
            return output;
        }

        /// <summary>
        /// Add key frame value for controller axis.
        /// </summary>
        [KRPCMethod]
        public bool AddKey(string part, string field, float time, float value)
        {
            foreach (var axis in controller.ControlledAxes)
            {
                if (part == axis.Part.name && field == axis.AxisField.name)
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
        [KRPCMethod]
        public bool ClearAxis(string part, string field)
        {
            foreach (var axis in controller.ControlledAxes)
            {
                if (part == axis.Part.name && field == axis.AxisField.name)
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
