using System;
using KRPC.Service;
using KRPC.Service.Attributes;
using KSA.Kfs;
using Vehicle = KRPC.KSA.Services.Vehicle;

namespace KRPC.KSA.FlightSoftware
{
    /// <summary>
    /// Provides access to kFS, the game's flight software: programs that run on a vehicle's
    /// flight computer.
    /// </summary>
    /// <remarks>
    /// kFS is a prototype in the game, and is off until it is turned on, by setting
    /// <see cref="Available"/> or with the game's <c>kfs on</c> console command. This service
    /// follows the prototype, and changes with it.
    /// </remarks>
    [KRPCService (GameScene = GameScene.Flight)]
    public static class KFS
    {
        /// <summary>
        /// Returns whether kFS is on, and turns it on or off.
        /// </summary>
        /// <remarks>
        /// Turning it off stops every program, and gives each vehicle back to the pilot, as the
        /// game's <c>kfs off</c> console command does.
        /// </remarks>
        [KRPCProperty]
        public static bool Available {
            get { return KfsPrototype.Enabled; }
            set {
                var enabled = KfsPrototype.Enabled;
                KfsPrototype.Enabled = value;
                if (enabled && !value)
                    KfsVehicleHost.HandBackAtFlagOff ();
            }
        }

        internal static void CheckAvailable ()
        {
            if (!KfsPrototype.Enabled)
                throw new InvalidOperationException ("kFS is off");
        }

        /// <summary>
        /// The flight computer of a vehicle. Throws an exception if kFS is off.
        /// </summary>
        /// <param name="vehicle">The vehicle.</param>
        [KRPCProcedure]
        public static Computer Computer (Vehicle vehicle)
        {
            if (ReferenceEquals (vehicle, null))
                throw new ArgumentNullException (nameof (vehicle));
            CheckAvailable ();
            return new Computer (vehicle);
        }
    }
}
