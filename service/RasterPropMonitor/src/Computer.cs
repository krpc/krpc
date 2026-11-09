using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using ObjectDestroyedException = KRPC.Service.KRPC.ObjectDestroyedException;

namespace KRPC.RasterPropMonitor
{
    /// <summary>
    /// The Raster Prop Monitor computer of a part, which holds the variables its props show.
    /// Obtained by calling <see cref="RasterPropMonitor.Computer"/> or
    /// <see cref="RasterPropMonitor.Computers"/>.
    /// </summary>
    [KRPCClass (Service = "RasterPropMonitor", GameScene = GameScene.Flight)]
    public class Computer : Equatable<Computer>, IGameObjectState
    {
        readonly SpaceCenter.Services.Parts.Part part;

        internal Computer (SpaceCenter.Services.Parts.Part innerPart)
        {
            part = innerPart;
            if (API.Computer (part.InternalPart) == null)
                throw new ArgumentException ("The part has no Raster Prop Monitor computer", nameof (innerPart));
        }

        /// <summary>
        /// Check that the computers are the same.
        /// </summary>
        public override bool Equals (Computer other)
        {
            return !ReferenceEquals (other, null) && part == other.part;
        }

        /// <summary>
        /// Hash the computer.
        /// </summary>
        public override int GetHashCode ()
        {
            return part.GetHashCode ();
        }

        /// <summary>
        /// The state of the computer. It takes the state of its part, and is destroyed once
        /// a live part no longer has the computer module.
        /// </summary>
        public GameObjectState GameObjectState {
            get {
                var state = part.GameObjectState;
                if (state != GameObjectState.Live)
                    return state;
                return API.Computer (part.InternalPart) != null ? GameObjectState.Live : GameObjectState.Destroyed;
            }
        }

        // The computer module, checked to still exist. Every member that reaches the mod goes
        // through this.
        PartModule InternalComputer {
            get {
                var computer = API.Computer (part.InternalPart);
                if (computer == null)
                    throw new ObjectDestroyedException ("The computer no longer exists, as its part no longer has one.");
                return computer;
            }
        }

        /// <summary>
        /// The part the computer is on.
        /// </summary>
        [KRPCProperty]
        public SpaceCenter.Services.Parts.Part Part {
            get { return part; }
        }

        /// <summary>
        /// The value of a persistent variable of the part, or else of the vessel, or
        /// <c>null</c> if neither is set. Props read it as <c>PERSISTENT_name</c>.
        /// </summary>
        /// <param name="name">The name of the variable, without the <c>PERSISTENT_</c> prefix.</param>
        [KRPCMethod]
        public double? PersistentVariable (string name)
        {
            return API.PersistentVariable (InternalComputer, name, false);
        }

        /// <summary>
        /// Set a persistent variable of the part. The props that show it update.
        /// </summary>
        /// <param name="name">The name of the variable, without the <c>PERSISTENT_</c> prefix.</param>
        /// <param name="value">The value.</param>
        [KRPCMethod]
        public void SetPersistentVariable (string name, double value)
        {
            API.SetPersistentVariable (InternalComputer, name, value, false);
        }

        /// <summary>
        /// Remove a persistent variable of the part. The props that show it update.
        /// </summary>
        /// <param name="name">The name of the variable, without the <c>PERSISTENT_</c> prefix.</param>
        [KRPCMethod]
        public void RemovePersistentVariable (string name)
        {
            API.RemovePersistentVariable (InternalComputer, name, false);
        }

        /// <summary>
        /// An event that fires when <see cref="PersistentVariable"/> changes, such as when a
        /// switch in the IVA sets it. The event lasts until it is removed.
        /// </summary>
        /// <param name="name">The name of the variable, without the <c>PERSISTENT_</c> prefix.</param>
        [KRPCMethod]
        public KRPC.Service.Messages.Event PersistentVariableChanged (string name)
        {
            return RasterPropMonitor.ChangedEvent (() => PersistentVariable (name));
        }

        /// <summary>
        /// The type of a variable's current value.
        /// Throws an exception if Raster Prop Monitor does not recognize the variable.
        /// </summary>
        /// <param name="name">The name of the variable, e.g. <c>ALTITUDE</c>.</param>
        [KRPCMethod]
        public VariableType VariableType (string name)
        {
            return API.Variable (InternalComputer, name) is string ? global::KRPC.RasterPropMonitor.VariableType.String : global::KRPC.RasterPropMonitor.VariableType.Number;
        }

        /// <summary>
        /// The value of a number variable. Raster Prop Monitor gives a boolean as 0 or 1.
        /// Throws an exception if Raster Prop Monitor does not recognize the variable, or
        /// its value is a string.
        /// </summary>
        /// <param name="name">The name of the variable, e.g. <c>ALTITUDE</c>.</param>
        [KRPCMethod]
        public double NumberVariable (string name)
        {
            var value = API.Variable (InternalComputer, name);
            if (value is string)
                throw new InvalidOperationException ("The variable " + name + " is a string");
            return (double)value;
        }

        /// <summary>
        /// The value of a variable as a string. A number gives its invariant culture string.
        /// Throws an exception if Raster Prop Monitor does not recognize the variable.
        /// </summary>
        /// <param name="name">The name of the variable, e.g. <c>ALTITUDE</c>.</param>
        [KRPCMethod]
        public string StringVariable (string name)
        {
            var value = API.Variable (InternalComputer, name);
            return value as string ?? ((double)value).ToString (System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Text with its variables formatted as a monitor page formats them, e.g.
        /// <c>{0:SIP6.1}m $&amp;$ ALTITUDE</c>. Each line takes its own variables.
        /// Throws an exception if Raster Prop Monitor does not recognize a variable, or a
        /// format is invalid.
        /// </summary>
        /// <param name="text">The text, in the format of a monitor page.</param>
        [KRPCMethod]
        public string FormatVariables (string text)
        {
            return API.FormatVariables (InternalComputer, text);
        }
    }
}
