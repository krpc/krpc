using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// See <see cref="ResourceConverter.Status"/>.
    /// </summary>
    [KRPCEnum (Service = "SpaceCenter")]
    public enum ResourceConverterState
    {
        /// <summary>
        /// Converter is running.
        /// </summary>
        Running,
        /// <summary>
        /// Converter is idle.
        /// </summary>
        Idle,
        /// <summary>
        /// Converter is missing a required resource.
        /// </summary>
        MissingResource,
        /// <summary>
        /// No available storage for output resource.
        /// </summary>
        StorageFull,
        /// <summary>
        /// At preset resource capacity.
        /// </summary>
        Capacity,
        /// <summary>
        /// Unknown state. Possible with modified resource converters.
        /// In this case, check <see cref="ResourceConverter.StatusString"/> for more information.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Obtained by calling <see cref="Part.ResourceConverter"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public sealed class ResourceConverter: Equatable<ResourceConverter>
    {
        readonly Part part;
        readonly IList<ModuleResourceConverter> converters;

        internal ResourceConverter (Part part)
        {
            this.part = part;
            converters = part.Modules.OfType<ModuleResourceConverter> ().ToList ();
        }

        public override bool Equals (ResourceConverter obj)
        {
            return part == obj.part;
        }

        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The part object for this converter.
        /// </summary>
        [KRPCProperty]
        public Part Part {
            get { return part; }
        }

        /// <summary>
        /// The number of converter modules in the part.
        /// </summary>
        [KRPCProperty]
        public int Count {
            get { return converters.Count; }
        }

        void CheckConverterExists (int index)
        {
            if (index >= Count)
                throw new ArgumentException ("No resource converter found with index " + index);
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
        /// The status of specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public ResourceConverterState Status (int index)
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
        /// Status string for the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public string StatusString (int index)
        {
            CheckConverterExists (index);
            return converters [index].status;
        }

        /// <summary>
        /// List of input resources for the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public IList<string> Inputs (int index)
        {
            CheckConverterExists (index);
            return converters [index].inputList.Select (x => x.ResourceName).ToList ();
        }

        /// <summary>
        /// List of output resources for the specified converter.
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
