using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A resource converter. Obtained by calling <see cref="Part.ResourceConverter"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class ResourceConverter: Equatable<ResourceConverter>
    {
        // Re-derivable references to the part's converter modules, looked up from the live
        // part by stored index on each access rather than captured. The set of converters is
        // fixed for the part's lifetime, so it is captured once at construction.
        readonly ModuleRef<ModuleResourceConverter>[] converterRefs;

        IList<ModuleResourceConverter> converters {
            get {
                var internalPart = Part.InternalPart;
                var result = new ModuleResourceConverter [converterRefs.Length];
                for (int i = 0; i < converterRefs.Length; i++)
                    result [i] = converterRefs [i].Resolve (internalPart);
                return result;
            }
        }

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleResourceConverter> ();
        }

        internal ResourceConverter (Part part)
        {
            Part = part;
            var internalPart = part.InternalPart;
            var modules = internalPart.Modules.OfType<ModuleResourceConverter> ().ToList ();
            if (modules.Count == 0)
                throw new ArgumentException ("Part is does not contain any resource converters");
            converterRefs = new ModuleRef<ModuleResourceConverter> [modules.Count];
            for (int i = 0; i < modules.Count; i++)
                converterRefs [i] = new ModuleRef<ModuleResourceConverter> (internalPart, modules [i]);
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ResourceConverter other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Part.GetHashCode ();
        }

        /// <summary>
        /// The part object for this converter.
        /// </summary>
        [KRPCProperty]
        public Part Part { get; private set; }

        /// <summary>
        /// The number of converters in the part.
        /// </summary>
        [KRPCProperty]
        public int Count {
            get { return converters.Count; }
        }

        void CheckConverterExists (int index)
        {
            if (index < 0 || Count <= index)
                throw new ArgumentException ("Converter not found with index " + index);
        }

        /// <summary>
        /// True if the specified converter is active.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public bool Active (int index)
        {
            CheckConverterExists (index);
            return converters [index].IsActivated;
        }

        /// <summary>
        /// The name of the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public string Name (int index)
        {
            CheckConverterExists (index);
            return converters [index].ConverterName;
        }

        /// <summary>
        /// Start the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public void Start (int index)
        {
            CheckConverterExists (index);
            converters [index].StartResourceConverter ();
        }

        /// <summary>
        /// Stop the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public void Stop (int index)
        {
            CheckConverterExists (index);
            converters [index].StopResourceConverter ();
        }

        /// <summary>
        /// The state of the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public ResourceConverterState State (int index)
        {
            CheckConverterExists (index);
            var converter = converters [index];
            // Use IsActivated for the active/idle distinction rather than matching
            // the localized "Inactive" status string. A running converter reports
            // either "<x>% load" (while thermally throttled) or "Operational" (at
            // full capacity), so anything active that isn't reporting a problem is
            // treated as running.
            if (!converter.IsActivated)
                return ResourceConverterState.Idle;
            var status = converter.status;
            if (status == null)
                return ResourceConverterState.Unknown;
            else if (status.Contains ("missing") || status.IndexOf("insufficient", StringComparison.OrdinalIgnoreCase) >= 0)
                return ResourceConverterState.MissingResource;
            else if (status.Contains ("full"))
                return ResourceConverterState.StorageFull;
            else if (status.Contains ("cap"))
                return ResourceConverterState.Capacity;
            else
                return ResourceConverterState.Running;
        }

        /// <summary>
        /// Status information for the specified converter.
        /// This is the full status message shown in the in-game UI.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public string StatusInfo (int index)
        {
            CheckConverterExists (index);
            return converters [index].status;
        }

        /// <summary>
        /// List of the names of resources consumed by the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public IList<string> Inputs (int index)
        {
            CheckConverterExists (index);
            return converters [index].inputList.Select (x => x.ResourceName).ToList ();
        }

        /// <summary>
        /// List of the names of resources produced by the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public IList<string> Outputs (int index)
        {
            CheckConverterExists (index);
            return converters [index].outputList.Select (x => x.ResourceName).ToList ();
        }

        /// <summary>
        /// The thermal efficiency of the converter, as a percentage of its maximum.
        /// </summary>
        [KRPCProperty]
        public float ThermalEfficiency {
            get {
                var core = converters[0];
                var temp = Convert.ToSingle(core.GetCoreTemperature());
                return core.ThermalEfficiency.Evaluate(temp);
            }
        }

        /// <summary>
        /// The core temperature of the converter, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float CoreTemperature {
            get { return (float)converters[0].GetCoreTemperature (); }
        }

        /// <summary>
        /// The core temperature at which the converter will operate with peak efficiency, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float OptimumCoreTemperature {
            get { return (float)converters[0].GetGoalTemperature (); }
        }
    }
}
