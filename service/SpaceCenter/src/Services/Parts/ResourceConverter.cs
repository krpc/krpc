using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// Obtained by calling <see cref="Part.ResourceConverter"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class ResourceConverter: Equatable<ResourceConverter>
    {
        readonly Part part;
        readonly IList<ModuleResourceConverter> converters;

        internal static bool Is (Part part)
        {
            return part.InternalPart.HasModule<ModuleResourceConverter> ();
        }

        internal ResourceConverter (Part part)
        {
            this.part = part;
            converters = part.InternalPart.Modules.OfType<ModuleResourceConverter> ().ToList ();
            if (converters.Count == 0)
                throw new ArgumentException ("Part is does not contain any resource converters");
        }

        /// <summary>
        /// Check if resource converters are equal.
        /// </summary>
        public override bool Equals (ResourceConverter obj)
        {
            return part == obj.part && converters.SequenceEqual (obj.converters);
        }

        /// <summary>
        /// Hash the resource converter.
        /// </summary>
        public override int GetHashCode ()
        {
            int hash = part.GetHashCode ();
            foreach (var converter in converters)
                hash ^= converter.GetHashCode ();
            return hash;
        }

        /// <summary>
        /// The part object for this converter.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

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
            var status = converters [index].status;
            if (status.Contains ("load"))
                return ResourceConverterState.Running;
            else if (status.Contains ("Inactive"))
                return ResourceConverterState.Idle;
            else if (status.Contains ("missing"))
                return ResourceConverterState.MissingResource;
            else if (status.Contains ("full"))
                return ResourceConverterState.StorageFull;
            else if (status.Contains ("cap"))
                return ResourceConverterState.Capacity;
            else
                return ResourceConverterState.Unknown;
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
    }
}
