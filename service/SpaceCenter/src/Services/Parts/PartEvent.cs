using System;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.SpaceCenter.Services.Parts
{
    /// <summary>
    /// An event of a part module. Events are the clickable buttons visible in the right-click menu
    /// of the part. Obtained by calling <see cref="Module.EventList"/>.
    /// </summary>
    [KRPCClass (Service = "SpaceCenter")]
    public class PartEvent : Equatable<PartEvent>
    {
        readonly BaseEvent partEvent;

        internal PartEvent (Module module, BaseEvent baseEvent)
        {
            Module = module;
            partEvent = baseEvent;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (PartEvent other)
        {
            return !ReferenceEquals (other, null) && Module == other.Module && ReferenceEquals (partEvent, other.partEvent);
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Module.GetHashCode () ^ partEvent.GetHashCode ();
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
            get { return partEvent.name; }
        }

        /// <summary>
        /// The name of the event, as displayed in the right-click menu of the part.
        /// </summary>
        [KRPCProperty]
        public string GuiName {
            get { return partEvent.guiName; }
        }

        /// <summary>
        /// Whether the event is visible in the right-click menu of the part, in the current scene
        /// (flight or editor).
        /// </summary>
        [KRPCProperty]
        public bool Visible {
            get { return HighLogic.LoadedSceneIsEditor ? partEvent.guiActiveEditor : partEvent.guiActive; }
        }

        /// <summary>
        /// Whether the event is currently active.
        /// </summary>
        [KRPCProperty]
        public bool Active {
            get { return partEvent.active; }
        }

        /// <summary>
        /// Trigger the event. Equivalent to clicking the button in the right-click menu of the part.
        /// </summary>
        [KRPCMethod]
        public void Trigger ()
        {
            partEvent.Invoke ();
        }
    }
}
