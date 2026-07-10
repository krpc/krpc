using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// A stable, re-derivable reference to a single part module of type
    /// <typeparamref name="T"/> on a part. Stores the module's position in the part's
    /// module list (and its occurrence among same-type modules as a fallback) rather
    /// than capturing the module, so the module is looked up from the live part on each
    /// access and never pins a destroyed part. This mirrors the re-derivation used by
    /// <see cref="Module"/>, but keyed by module type rather than name, and is shared by
    /// the part-module proxies (such as <see cref="Engine"/> or <see cref="Sensor"/>) so
    /// each avoids a module-list scan (or an allocating <c>OfType&lt;T&gt;().ToList()</c>)
    /// on every access.
    /// </summary>
    struct ModuleRef<T> where T : PartModule
    {
        readonly int rawIndex;
        readonly int occurrence;

        public ModuleRef (global::Part part, T partModule)
        {
            rawIndex = -1;
            occurrence = 0;
            var modules = part.Modules;
            int seen = 0;
            for (int i = 0; i < modules.Count; i++) {
                if (ReferenceEquals (modules [i], partModule)) {
                    rawIndex = i;
                    occurrence = seen;
                    break;
                }
                if (modules [i] is T)
                    seen++;
            }
        }

        // Construct directly from a known index and occurrence (used by For).
        ModuleRef (int rawIndex, int occurrence)
        {
            this.rawIndex = rawIndex;
            this.occurrence = occurrence;
        }

        /// <summary>
        /// The module's occurrence among the same-type modules on the part (its index in
        /// the part's filtered list of <typeparamref name="T"/> modules). Stable for the
        /// part's lifetime, so suitable for proxy equality.
        /// </summary>
        public int Occurrence {
            get { return occurrence; }
        }

        /// <summary>
        /// Re-derive the module from the live part. The module keeps its position for the
        /// part's lifetime, so this indexes the list directly (validated by type) with an
        /// occurrence scan as a fallback. Throws <see cref="ObjectDestroyedException"/> if
        /// the module no longer exists.
        /// </summary>
        public T Resolve (global::Part part)
        {
            var modules = part.Modules;
            // Fast path: the module keeps its position for the part's lifetime, so index
            // directly and validate the type rather than scanning the list.
            if (rawIndex >= 0 && rawIndex < modules.Count) {
                var candidate = modules [rawIndex];
                // 'candidate != null' is the Unity destroyed check (PartModule derives
                // from UnityEngine.Object); 'as T' validates the type without re-scanning.
                if (candidate != null) {
                    var typed = candidate as T;
                    if (typed != null)
                        return typed;
                }
            }
            // Slow path: the list changed; re-derive by the occurrence among same-type modules.
            int seen = 0;
            for (int i = 0; i < modules.Count; i++) {
                var typed = modules [i] as T;
                if (typed != null) {
                    if (seen == occurrence)
                        return typed;
                    seen++;
                }
            }
            throw new ObjectDestroyedException (
                "The part module no longer exists. The part may have been destroyed.");
        }

        // A reference to the first module of type T on the part, or null if the part has no
        // such module. Used by the proxies that wrap a single module of a given type, in
        // place of a Module&lt;T&gt;() scan on every access.
        public static ModuleRef<T>? For (global::Part part)
        {
            var modules = part.Modules;
            for (int i = 0; i < modules.Count; i++)
                if (modules [i] is T)
                    return new ModuleRef<T> (i, 0);
            return null;
        }

        // Resolve a possibly-absent reference: the module, re-derived from the live part, or
        // null if the reference is absent (the part had no such module at construction).
        public static T ResolveOrNull (ModuleRef<T>? reference, global::Part part)
        {
            return reference.HasValue ? reference.Value.Resolve (part) : null;
        }
    }
}
