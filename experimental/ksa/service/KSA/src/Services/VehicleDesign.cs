using System;
using System.Collections.Generic;
using System.Reflection;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA;

namespace KRPC.KSA.Services
{
    /// <summary>
    /// A vehicle design that can be launched: one of the game's default vehicles, or a vehicle
    /// saved by the player in the editor. Obtained from <see cref="KSA.VehicleDesigns"/>.
    /// </summary>
    /// <remarks>
    /// The mass and delta-v are those the editor shows for the design.
    /// </remarks>
    [KRPCClass (Service = "KSA")]
    public class VehicleDesign : Equatable<VehicleDesign>, IGameObjectState
    {
        internal VehicleDesign (string name)
        {
            Name = name ?? throw new ArgumentNullException (nameof (name));
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (VehicleDesign other)
        {
            return !ReferenceEquals (other, null) && Name == other.Name;
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return Find (Name) != null ? GameObjectState.Live : GameObjectState.Destroyed; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return Name.GetHashCode (StringComparison.Ordinal);
        }

        /// <summary>
        /// The save of the design with the given name, or <c>null</c> if there is none.
        /// Kittens are saved as designs too, and are left out.
        /// </summary>
        internal static VehicleSave Find (string name)
        {
            var save = DefaultVehicleSaves.FindSave (name);
            if (save == null) {
                foreach (var playerSave in VehicleSaves.AsSpan ()) {
                    if (playerSave.Id == name)
                        save = playerSave;
                }
            }
            return save == null || !string.IsNullOrEmpty (save.VehicleSaveData.Character) ? null : save;
        }

        // The game's default vehicles, with no public accessor
        static readonly PropertyInfo defaultSaves = typeof (DefaultVehicleSaves).GetProperty (
            "Saves", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>
        /// The designs, the game's default vehicles first.
        /// </summary>
        internal static List<VehicleDesign> All {
            get {
                var result = new List<VehicleDesign> ();
                foreach (var save in ((LookupCollection<VehicleSave>)defaultSaves.GetValue (null)).AsSpan ()) {
                    if (string.IsNullOrEmpty (save.VehicleSaveData.Character))
                        result.Add (new VehicleDesign (save.Id));
                }
                foreach (var save in VehicleSaves.AsSpan ()) {
                    if (DefaultVehicleSaves.FindSave (save.Id) == null && string.IsNullOrEmpty (save.VehicleSaveData.Character))
                        result.Add (new VehicleDesign (save.Id));
                }
                return result;
            }
        }

        VehicleSave Save {
            get { return Find (Name) ?? throw new Service.KRPC.ObjectDestroyedException ("The vehicle design no longer exists"); }
        }

        /// <summary>
        /// The name of the design, as passed to <see cref="KSA.LaunchVehicle"/>.
        /// </summary>
        [KRPCProperty]
        public string Name { get; }

        /// <summary>
        /// Whether the design is one of the game's default vehicles.
        /// </summary>
        [KRPCProperty]
        public bool IsDefault {
            get {
                _ = Save;
                return DefaultVehicleSaves.FindSave (Name) != null;
            }
        }

        T WithParts<T> (Func<PartTree, T> read)
        {
            // Loading builds the design's parts, as the editor and the launch menu do
            var tree = Save.Load (Program.MainViewport) ?? throw new InvalidOperationException ("Failed to load the vehicle design");
            try {
                tree.ApplyTankAssignments ();
                return read (tree);
            } finally {
                tree.Dispose ();
            }
        }

        /// <summary>
        /// The number of parts in the design.
        /// </summary>
        [KRPCProperty]
        public int PartCount {
            get { return WithParts (tree => tree.Count); }
        }

        /// <summary>
        /// The total mass of the design, including propellant, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float Mass {
            get {
                return WithParts (tree =>
                    tree.ComputeInertMassPropertiesAsmb ().Props.Mass + tree.ComputePropellantMassPropertiesAsmb ().Props.Mass);
            }
        }

        /// <summary>
        /// The mass of the design without propellant, in kilograms.
        /// </summary>
        [KRPCProperty]
        public float DryMass {
            get { return WithParts (tree => tree.ComputeInertMassPropertiesAsmb ().Props.Mass); }
        }

        /// <summary>
        /// The change in velocity of all the design's sequences, in meters per second.
        /// </summary>
        /// <remarks>
        /// Each sequence is computed in the environment chosen for it in the editor, in vacuum
        /// or at sea level.
        /// </remarks>
        [KRPCProperty]
        public float DeltaV {
            get {
                return WithParts (tree => {
                    tree.PerformanceSequences.RecomputeIfDirty ();
                    return tree.PerformanceSequences.TotalDeltaV;
                });
            }
        }
    }
}
