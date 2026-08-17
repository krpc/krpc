using System;
using System.Collections.Generic;
using System.Linq;
using KRPC.Benchmarks;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.SpaceCenter;
using ServicePart = KRPC.SpaceCenter.Services.Parts.Part;
using ServiceVessel = KRPC.SpaceCenter.Services.Vessel;

namespace TestingTools
{
    /// <summary>
    /// The benchmarks that need the game: the ways of getting from an identifier back to a
    /// game object, which no remote procedure exposes on its own. They exist to be compared
    /// with each other within a single session, since absolute timings drift between sessions
    /// by more than most differences worth measuring.
    ///
    /// What a remote procedure costs is measured by the <c>Benchmark</c> service instead,
    /// which names no game object and so is the same measurement here and against TestServer.
    /// These use its timer, so their numbers are in the same units as its.
    ///
    /// Both entry points are typed on the object under test, so the proxy is decoded before
    /// timing starts, and return what one chunk of the loop cost. The client decides how many
    /// iterations a chunk is, so it can keep a single call short enough not to stall the game;
    /// it never times anything itself, so no measurement includes a network round trip.
    ///
    /// The scripts that drive these live in <c>tools/benchmarks/</c>.
    /// </summary>
    public static partial class TestingTools
    {
        /// <summary>
        /// Run a benchmark case against a part, and return the metrics for that chunk.
        /// </summary>
        /// <param name="part">The part to measure against.</param>
        /// <param name="caseName">Name of the case to run, from the part case registry.</param>
        /// <param name="iterations">Number of times to run the case.</param>
        [KRPCProcedure]
        public static IDictionary<string, double> BenchmarkPart (ServicePart part, string caseName, uint iterations)
        {
            if (part == null)
                throw new ArgumentNullException (nameof (part));
            return Timer.Measure (Lookup (PartCases, caseName) (part), iterations);
        }

        /// <summary>
        /// Run a benchmark case against a vessel, and return the metrics for that chunk.
        /// </summary>
        /// <param name="vessel">The vessel to measure against.</param>
        /// <param name="caseName">Name of the case to run, from the vessel case registry.</param>
        /// <param name="iterations">Number of times to run the case.</param>
        [KRPCProcedure]
        public static IDictionary<string, double> BenchmarkVessel (ServiceVessel vessel, string caseName, uint iterations)
        {
            if (vessel == null)
                throw new ArgumentNullException (nameof (vessel));
            return Timer.Measure (Lookup (VesselCases, caseName) (vessel), iterations);
        }

        /// <summary>
        /// The names of the cases that <see cref="BenchmarkPart"/> accepts.
        /// </summary>
        [KRPCProperty]
        public static IList<string> BenchmarkPartCases {
            get { return PartCases.Keys.OrderBy (x => x, StringComparer.Ordinal).ToList (); }
        }

        /// <summary>
        /// The names of the cases that <see cref="BenchmarkVessel"/> accepts.
        /// </summary>
        [KRPCProperty]
        public static IList<string> BenchmarkVesselCases {
            get { return VesselCases.Keys.OrderBy (x => x, StringComparer.Ordinal).ToList (); }
        }

        // A case is a factory: given the object under test it does whatever setup the case needs
        // (constructing a reference, filling an object store) and returns the operation to time.
        // Only the returned operation is inside the timed loop.

        static readonly IDictionary<string, Func<ServicePart, Action>> PartCases =
            new Dictionary<string, Func<ServicePart, Action>> {
                // The cost of the loop itself: one delegate call and one volatile store. Subtract
                // it from every other case in this registry to get the cost of the operation.
                { "empty", part => () => { Timer.IntSink = 0; } },

                // Part resolution. A proxy that holds a stable id has to turn it back into a KSP
                // part on every access; these are the ways of doing that, from the cheapest (what
                // a proxy that captures its part pays) to the linear scan over every loaded part.
                { "resolve.captured", part => {
                    var internalPart = part.InternalPart;
                    return () => { Timer.BoolSink = internalPart != null; };
                } },
                { "resolve.cached", part => {
                    var reference = new CachedPart (part.InternalPart.flightID);
                    return () => { Timer.ObjectSink = reference.Resolve (); };
                } },
                { "resolve.cached_bare", part => {
                    var reference = new BareCachedPart (part.InternalPart.flightID);
                    return () => { Timer.ObjectSink = reference.Resolve (); };
                } },
                { "resolve.find_part_by_id", part => {
                    var flightId = part.InternalPart.flightID;
                    return () => { Timer.ObjectSink = FlightGlobals.FindPartByID (flightId); };
                } },

                // Module re-derivation, on the part's last module, which is the worst case for a
                // scan.
                { "module.by_name_scan", part => {
                    var internalPart = part.InternalPart;
                    var name = LastModule (internalPart).moduleName;
                    return () => { Timer.ObjectSink = ScanForModule (internalPart, name); };
                } },
                { "module.indexed", part => {
                    var internalPart = part.InternalPart;
                    var index = internalPart.Modules.Count - 1;
                    var name = LastModule (internalPart).moduleName;
                    var count = internalPart.Modules.Count;
                    return () => { Timer.ObjectSink = IndexedModule (internalPart, index, name, count); };
                } },
                { "module.by_persistent_id", part => {
                    var internalPart = part.InternalPart;
                    var id = LastModule (internalPart).GetPersistentId ();
                    return () => { Timer.ObjectSink = internalPart.Modules [id]; };
                } },
                { "module.ref", part => {
                    // The re-derivation the service layer ships, which is the lookup at the
                    // remembered position plus the check that keeps it honest when the module
                    // list changes.
                    var internalPart = part.InternalPart;
                    var reference = ModuleRef.ForModule (LastModule (internalPart));
                    return () => { Timer.ObjectSink = reference.Get (internalPart); };
                } },
                { "module.of_type_to_list", part => {
                    var internalPart = part.InternalPart;
                    return () => { Timer.ObjectSink = internalPart.Modules.OfType<ModuleEngines> ().ToList (); };
                } },
            };

        static readonly IDictionary<string, Func<ServiceVessel, Action>> VesselCases =
            new Dictionary<string, Func<ServiceVessel, Action>> {
                { "empty", vessel => () => { Timer.IntSink = 0; } },

                // What returning a proxy costs once it has been constructed: the object store
                // hashes it and compares it against what it already holds, so this is the dedup
                // path, over a store holding one entry per part of the vessel. The store is a
                // private one rather than the server's, so the benchmark does not grow the store
                // the rest of the session is measured against.
                { "store.dedup", vessel => {
                    var store = new ObjectStore ();
                    var parts = vessel.Parts.All;
                    foreach (var part in parts)
                        store.AddInstance (part);
                    var index = 0;
                    return () => {
                        index = index + 1 < parts.Count ? index + 1 : 0;
                        Timer.IntSink = (int)store.AddInstance (parts [index]);
                    };
                } },
            };

        /// <summary>
        /// Re-derives a part from its flight id the way a part object does: through the shared
        /// cache the service layer uses, falling back to a scan over every loaded part when the
        /// cache has nothing to give. This is the shipped code path, not a copy of it, so what
        /// the case measures is what a part getter pays.
        /// </summary>
        sealed class CachedPart
        {
            readonly uint flightId;
            CachedObject<Part> cache;

            public CachedPart (uint id)
            {
                flightId = id;
            }

            public Part Resolve ()
            {
                var part = cache.Get ();
                if (part != null)
                    return part;
                part = FlightGlobals.FindPartByID (flightId);
                cache.Set (part);
                return part;
            }
        }

        /// <summary>
        /// The least a weak-reference cache can do: no game-state stamp, and typed on the part
        /// rather than shared between every kind of game object. Measured beside
        /// <see cref="CachedPart"/> to price what the shipped cache adds, which is what lets it
        /// tell a game object rebuilt under the same identifier from the one it last saw.
        /// </summary>
        sealed class BareCachedPart
        {
            readonly uint flightId;
            WeakReference reference;

            public BareCachedPart (uint id)
            {
                flightId = id;
            }

            public Part Resolve ()
            {
                if (reference != null) {
                    var cached = reference.Target as Part;
                    if (cached != null)
                        return cached;
                }
                var part = FlightGlobals.FindPartByID (flightId);
                reference = new WeakReference (part);
                return part;
            }
        }

        static PartModule LastModule (Part part)
        {
            var modules = part.Modules;
            if (modules.Count == 0)
                throw new ArgumentException ("Part has no part modules");
            return modules [modules.Count - 1];
        }

        // Find a module by name the way a proxy that stores only the module's name has to: walk the
        // part's module list until the name matches.
        static PartModule ScanForModule (Part part, string name)
        {
            var modules = part.Modules;
            for (var i = 0; i < modules.Count; i++) {
                var module = modules [i];
                if (module.moduleName == name)
                    return module;
            }
            return null;
        }

        // Find a module by the index it had when the proxy was constructed, validating that the
        // list is the same length and that the module at that index still has the expected name.
        // Anything that fails validation falls back to the scan.
        static PartModule IndexedModule (Part part, int index, string name, int count)
        {
            var modules = part.Modules;
            if (modules.Count == count && index < modules.Count) {
                var module = modules [index];
                if (module.moduleName == name)
                    return module;
            }
            return ScanForModule (part, name);
        }

        static Func<T, Action> Lookup<T> (IDictionary<string, Func<T, Action>> cases, string name)
        {
            Func<T, Action> factory;
            if (name != null && cases.TryGetValue (name, out factory))
                return factory;
            throw new ArgumentException (
                "Unknown benchmark case '" + name + "'. Known cases: " +
                string.Join (", ", cases.Keys.OrderBy (x => x, StringComparer.Ordinal).ToArray ()));
        }
    }
}
