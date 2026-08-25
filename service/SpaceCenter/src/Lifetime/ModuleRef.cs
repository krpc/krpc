using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter
{
    /// <summary>
    /// Names one of a part's modules, so that an object standing for that module can find it
    /// again on every access instead of holding on to it.
    /// </summary>
    /// <remarks>
    /// What a reference is, is the module's class and which of the part's modules of that
    /// class it is. The game rebuilds a part's modules in the order its configuration lists
    /// them, so that outlives any one of them, which is why it is all that equality and
    /// hashing use.
    ///
    /// Finding the module is the game's own name for it, its persistent id, and where in the
    /// part's module list it was last seen. An access reads that position and takes what is
    /// there if it carries the id, which no other module of the part can. Anything else looks
    /// the id up along the list, and corrects the position.
    ///
    /// The game gives a module an id only when something asks for one, and writes out only
    /// ids it has already given, so a game state saved before this named the module holds
    /// nothing to look it up by. Counting the part's modules of the class is what answers
    /// then, and it settles the id and the position again for next time.
    ///
    /// Nothing here allocates, and nothing is generic on the module type, which in a shared
    /// generic costs more than the lookup itself.
    /// </remarks>
    struct ModuleRef
    {
        // The module's class name, and which of the part's modules of that class this is.
        // A null name stands for no module at all.
        readonly string name;
        readonly int occurrence;
        // The game's own name for the module, and where the module was in the part's module
        // list, or 0 and -1 while it has not been found. Both are ways of finding the module
        uint id;
        int index;

        ModuleRef (string moduleName, int moduleOccurrence)
        {
            name = moduleName;
            occurrence = moduleOccurrence;
            id = 0;
            index = -1;
        }

        /// <summary>
        /// A reference to a module the caller already has.
        /// </summary>
        public static ModuleRef ForModule (PartModule module)
        {
            var part = module == null ? null : module.part;
            if (part == null)
                return None;
            var modules = part.Modules;
            var moduleCount = modules.Count;
            var matches = 0;
            for (var i = 0; i < moduleCount; i++) {
                var candidate = modules [i];
                if (candidate == null || candidate.moduleName != module.moduleName)
                    continue;
                if (ReferenceEquals (candidate, module)) {
                    var reference = new ModuleRef (module.moduleName, matches);
                    reference.Bind (module, i);
                    return reference;
                }
                matches++;
            }
            return None;
        }

        /// <summary>
        /// A reference to the first module of the part that is a <typeparamref name="T"/>,
        /// or to no module at all if the part has none. The type is only used to find the
        /// module; what the reference stands for afterwards is the class of what it found.
        /// </summary>
        public static ModuleRef ForType<T> (global::Part part) where T : PartModule
        {
            var modules = part.Modules;
            for (var i = 0; i < modules.Count; i++) {
                var module = modules [i] as T;
                if (module != null)
                    return ForModule (module);
            }
            return None;
        }

        /// <summary>
        /// A reference to no module at all, for an object that has one only sometimes.
        /// </summary>
        public static ModuleRef None {
            get { return new ModuleRef (null, -1); }
        }

        /// <summary>
        /// The module on the given part, or null if the part does not have it.
        /// </summary>
        public PartModule Find (global::Part part)
        {
            var modules = part.Modules;
            if (index >= 0 && index < modules.Count) {
                var module = modules [index];
                // The id is unique among the modules of a part from the moment the game
                // hands it out, so the module is the one carrying it that is of the
                // reference's class. The index is only where the module was: a change that
                // leaves the list length alone moves the modules under it. An id loaded from
                // a game state had no uniqueness enforced on it, so the class has to agree
                // as well
                if (module != null && module.PersistentId == id && module.moduleName == name)
                    return module;
            }
            return Search (part);
        }

        /// <summary>
        /// The module on the given part. Throws if the part does not have it.
        /// </summary>
        public PartModule Get (global::Part part)
        {
            var module = Find (part);
            if (module == null)
                throw new ObjectDestroyedException (
                    "The part module no longer exists, as the part it was on no longer has it.");
            return module;
        }

        /// <summary>
        /// The state of the module, for an object that identifies one.
        /// </summary>
        /// <remarks>
        /// A module belongs to its part's game object, so it is exactly as live, dormant or
        /// destroyed as the part until the part is there to look in. A module of a part the
        /// game has merely unloaded is no more gone than the part, and cannot be checked
        /// either, so it answers with the part's own state.
        /// </remarks>
        public GameObjectState StateOn (Services.Parts.Part part)
        {
            var state = part.GameObjectState;
            if (state != GameObjectState.Live)
                return state;
            return Find (part.InternalPart) != null ? GameObjectState.Live : GameObjectState.Destroyed;
        }

        /// <summary>
        /// Look for the module along the part's module list, by the id the game knows it by
        /// where that answers, and by counting the part's modules of the class where it does
        /// not, recording where it was found so that the next access is a look at one module.
        /// </summary>
        PartModule Search (global::Part part)
        {
            index = -1;
            if (name == null)
                return null;
            var modules = part.Modules;
            var moduleCount = modules.Count;
            if (id != 0) {
                for (var i = 0; i < moduleCount; i++) {
                    var module = modules [i];
                    // An id is unique among the modules of a part only from the moment the
                    // game hands it out, so one loaded from a game state has to be of the
                    // reference's class as well
                    if (module == null || module.PersistentId != id || module.moduleName != name)
                        continue;
                    index = i;
                    return module;
                }
            }
            return Count (modules, moduleCount);
        }

        /// <summary>
        /// The module of the reference's class and position among the part's modules of that
        /// class, which is what finds it when the game knows it by no id.
        /// </summary>
        PartModule Count (PartModuleList modules, int moduleCount)
        {
            var matches = 0;
            for (var i = 0; i < moduleCount; i++) {
                var module = modules [i];
                if (module == null || module.moduleName != name)
                    continue;
                if (matches == occurrence) {
                    Bind (module, i);
                    return module;
                }
                matches++;
            }
            return null;
        }

        /// <summary>
        /// Record where a module just found was, and take the game's own name for it, asking
        /// the game to give it one if it has not already.
        /// </summary>
        void Bind (PartModule module, int moduleIndex)
        {
            id = module.GetPersistentId ();
            index = moduleIndex;
        }

        /// <summary>
        /// Whether two references name the same module of a part: the same class, and
        /// the same one of the part's modules of it. Which part that is belongs to the
        /// object holding the reference, so two references from different parts compare
        /// equal and the objects holding them are told apart by their parts.
        /// </summary>
        public bool Equals (ModuleRef other)
        {
            return name == other.name && occurrence == other.occurrence;
        }

        /// <summary>
        /// Whether two references name the same module of a part.
        /// </summary>
        public override bool Equals (object obj)
        {
            return obj is ModuleRef && Equals ((ModuleRef)obj);
        }

        /// <summary>
        /// Hash code for the reference, from what it stands for rather than where it last
        /// found it, so that it does not change over the lifetime of the object holding it.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (name).And (occurrence);
        }

        /// <summary>
        /// Whether two references name the same module of a part.
        /// </summary>
        public static bool operator == (ModuleRef lhs, ModuleRef rhs)
        {
            return lhs.Equals (rhs);
        }

        /// <summary>
        /// Whether two references stand for different modules.
        /// </summary>
        public static bool operator != (ModuleRef lhs, ModuleRef rhs)
        {
            return !lhs.Equals (rhs);
        }
    }
}
