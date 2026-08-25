using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter.ExtensionMethods;
using KRPC.Utils;
using KSP.Localization;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A resource converter. Obtained by calling <see cref="Part.ResourceConverter"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter", GameScene = GameScene.Flight)]
    public class ResourceConverter: Equatable<ResourceConverter>, IGameObjectState
    {
        // One reference per converter the part has, in the order the part lists them, which
        // is the order the index arguments of this class count in.
        readonly ModuleRef[] converterRefs;

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
            converterRefs = new ModuleRef [modules.Count];
            for (var i = 0; i < modules.Count; i++)
                converterRefs [i] = ModuleRef.ForModule (modules [i]);
        }

        ModuleResourceConverter Converter (int index)
        {
            return (ModuleResourceConverter)converterRefs [index].Get (Part.InternalPart);
        }

        /// <summary>
        /// The state of the part carrying the converters, or destroyed once that part loses
        /// them.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return converterRefs [0].StateOn (Part); }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (ResourceConverter other)
        {
            return !ReferenceEquals (other, null) && Part == other.Part &&
            converterRefs.SequenceEqual (other.converterRefs);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            var hash = Hash.Of (Part);
            foreach (var converter in converterRefs)
                hash = hash.And (converter);
            return hash;
        }

        /// <summary>
        /// The part object for this converter.
        /// </summary>
        [KRPCProperty (GameScene = GameScene.Flight | GameScene.Editor)]
        public Part Part { get; private set; }

        /// <summary>
        /// The number of converters in the part.
        /// </summary>
        [KRPCProperty]
        public int Count {
            get { return converterRefs.Length; }
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
            return Converter (index).IsActivated;
        }

        /// <summary>
        /// The name of the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public string Name (int index)
        {
            CheckConverterExists (index);
            return Converter (index).ConverterName;
        }

        /// <summary>
        /// Start the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public void Start (int index)
        {
            CheckConverterExists (index);
            Converter (index).StartResourceConverter ();
        }

        /// <summary>
        /// Stop the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public void Stop (int index)
        {
            CheckConverterExists (index);
            Converter (index).StopResourceConverter ();
        }

        /// <summary>
        /// The state of the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public ResourceConverterState State (int index)
        {
            CheckConverterExists (index);
            var converter = Converter (index);
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
            if (StatusMatches (status, MissingResourceMessages))
                return ResourceConverterState.MissingResource;
            if (StatusMatches (status, StorageFullMessages))
                return ResourceConverterState.StorageFull;
            if (StatusMatches (status, CapacityMessages))
                return ResourceConverterState.Capacity;
            return ResourceConverterState.Running;
        }

        // The messages the game builds a converter's status out of. It reports the status
        // already translated, so these are the tags for the same messages rather than the
        // English text, which only matches when the game is running in English.
        static readonly string[] MissingResourceMessages = {
            "#autoLOC_261263", "#autoLOC_261304", // missing <resource>
            "#autoLOC_258451", "#autoLOC_259933"  // insufficient power
        };
        static readonly string[] StorageFullMessages = { "#autoLOC_261334" };
        static readonly string[] CapacityMessages = { "#autoLOC_261332" };

        /// <summary>
        /// Whether a converter's status is one of the given messages.
        /// </summary>
        /// <remarks>
        /// The messages carry a resource name or amount in a placeholder, so only their
        /// fixed part can be matched. Comparison follows the language the game is running
        /// in, which is the one case where that is what is wanted: both sides are text the
        /// game has translated.
        /// </remarks>
        static bool StatusMatches (string status, string[] tags)
        {
            foreach (var tag in tags) {
                var message = Localizer.Format (tag, new [] { string.Empty }).Trim ();
                if (message.Length > 0 &&
                    status.IndexOf (message, StringComparison.CurrentCultureIgnoreCase) >= 0)
                    return true;
            }
            return false;
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
            return Converter (index).status;
        }

        /// <summary>
        /// List of the names of resources consumed by the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public IList<string> Inputs (int index)
        {
            CheckConverterExists (index);
            return Converter (index).inputList.Select (x => x.ResourceName).ToList ();
        }

        /// <summary>
        /// List of the names of resources produced by the specified converter.
        /// </summary>
        /// <param name="index">Index of the converter.</param>
        [KRPCMethod]
        public IList<string> Outputs (int index)
        {
            CheckConverterExists (index);
            return Converter (index).outputList.Select (x => x.ResourceName).ToList ();
        }

        /// <summary>
        /// The thermal efficiency of the converter, as a percentage of its maximum.
        /// </summary>
        [KRPCProperty]
        public float ThermalEfficiency {
            get {
                var core = Converter (0);
                var temp = Convert.ToSingle(core.GetCoreTemperature());
                return core.ThermalEfficiency.Evaluate(temp);
            }
        }

        /// <summary>
        /// The core temperature of the converter, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float CoreTemperature {
            get { return (float)Converter (0).GetCoreTemperature (); }
        }

        /// <summary>
        /// The core temperature at which the converter will operate with peak efficiency, in Kelvin.
        /// </summary>
        [KRPCProperty]
        public float OptimumCoreTemperature {
            get { return (float)Converter (0).GetGoalTemperature (); }
        }
    }
}
