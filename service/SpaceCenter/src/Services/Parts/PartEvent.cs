using System;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An event of a part module. Events are the clickable buttons visible in the right-click menu
    /// of the part. Obtained by calling <see cref="Module.EventList"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class PartEvent : Equatable<PartEvent>, IGameObjectState
    {
        readonly string name;

        internal PartEvent (Module module, BaseEvent baseEvent)
        {
            Module = module;
            name = baseEvent.name;
        }

        /// <summary>
        /// The underlying KSP event, found on the module by its identifier.
        /// </summary>
        BaseEvent InternalEvent {
            get {
                var partEvent = Module.InternalModule.Events [name];
                if (partEvent == null)
                    throw new ObjectDestroyedException (
                        "The part module no longer has an event called " + name + ".");
                return partEvent;
            }
        }

        /// <summary>
        /// What the game holds for the module this belongs to.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Module.GameObjectState; }
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (PartEvent other)
        {
            return !ReferenceEquals (other, null) && Module == other.Module && name == other.name;
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Hash.Of (Module).And (name);
        }

        /// <summary>
        /// The part module that contains this event.
        /// </summary>
        [KRPCProperty]
        public Module Module { get; private set; }

        /// <summary>
        /// The identifier of the event. This is stable and does not change between game versions,
        /// unlike <see cref="GuiName"/>.
        /// </summary>
        [KRPCProperty]
        public string Name {
            get { return name; }
        }

        /// <summary>
        /// The name of the event, as displayed in the right-click menu of the part.
        /// </summary>
        [KRPCProperty]
        public string GuiName {
            get { return InternalEvent.guiName; }
        }

        /// <summary>
        /// Whether the event is visible in the right-click menu of the part, in the current scene
        /// (flight or editor).
        /// </summary>
        [KRPCProperty]
        public bool Visible {
            get { return HighLogic.LoadedSceneIsEditor ? InternalEvent.guiActiveEditor : InternalEvent.guiActive; }
        }

        /// <summary>
        /// Whether the event is currently active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return InternalEvent.active; }
        }

        /// <summary>
        /// Trigger the event. Equivalent to clicking the button in the right-click menu of the part.
        /// </summary>
        [KRPCMethod]
        public void Trigger ()
        {
            InternalEvent.Invoke ();
        }
    }
}
