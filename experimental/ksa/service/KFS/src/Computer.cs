using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using KRPC.Service;
using KRPC.Service.Attributes;
using KRPC.Utils;
using KSA.Kfs;
using KSA.Kfs.Compiler;
using KSA.Kfs.Host;
using KSA.Kfs.Host.Services;
using KSA.Kfs.Host.Views;
using KSA.Kfs.KAsm;
using KSA.Kfs.Vm;
using GameDiagnostic = KSA.Kfs.Compiler.Diagnostic;
using Vehicle = KRPC.KSA.Services.Vehicle;

namespace KRPC.KSA.FlightSoftware
{
    /// <summary>
    /// A vehicle's flight computer, which runs a kFS program. Obtained from
    /// <see cref="KFS.Computer"/>.
    /// </summary>
    /// <remarks>
    /// A program is written in kC, a subset of C, or in kASM. The calls that change the
    /// computer, and <see cref="WriteGlobals"/>, take effect before the computer's next tick, and
    /// return once they have. The computer ticks while the game is running and the vehicle is
    /// in the physics bubble, so until then they wait. The game does not save a program.
    /// </remarks>
    [KRPCClass (Service = "KFS", GameScene = GameScene.Flight)]
    public class Computer : Equatable<Computer>, IGameObjectState
    {
        readonly Vehicle vehicle;

        internal Computer (Vehicle vehicle)
        {
            this.vehicle = vehicle;
        }

        /// <summary>
        /// Returns true if the objects are equal.
        /// </summary>
        public override bool Equals (Computer other)
        {
            return !ReferenceEquals (other, null) && vehicle.Equals (other.vehicle);
        }

        /// <summary>
        /// The state of the game object.
        /// </summary>
        public GameObjectState GameObjectState {
            get { return vehicle.GameObjectState; }
        }

        /// <summary>
        /// Hash code for the object.
        /// </summary>
        public override int GetHashCode ()
        {
            return vehicle.GetHashCode ();
        }

        KfsVehicleHost Host {
            get { return KfsVehicleHost.For (vehicle.InternalVehicle); }
        }

        VehicleComputer InternalComputer {
            get { return Host.Computer; }
        }

        /// <summary>
        /// Build a program, without loading it.
        /// </summary>
        /// <returns>The errors and warnings.</returns>
        /// <param name="files">The source files, by name. A program with a <c>.c</c> file is
        /// kC, and one with only <c>.kasm</c> files is kASM.</param>
        [KRPCMethod]
        public IList<Diagnostic> Build (IDictionary<string, string> files)
        {
            KFS.CheckAvailable ();
            return Compile (Host, files).Diagnostics.Select (ToDiagnostic).ToList ();
        }

        /// <summary>
        /// Build a program and load it onto the computer. Throws an exception if the program has
        /// errors, or the computer rejects it.
        /// </summary>
        /// <remarks>
        /// The program is in standby once loaded. See <see cref="Start"/>.
        /// </remarks>
        /// <param name="files">The source files, by name, as for <see cref="Build"/>.</param>
        [KRPCMethod]
        public void Load (IDictionary<string, string> files)
        {
            KFS.CheckAvailable ();
            var host = Host;
            var (image, diagnostics) = Compile (host, files);
            if (image == null) {
                var errors = diagnostics.Where (d => d.Severity == Severity.Error).Select (d => d.ToString ());
                throw new InvalidOperationException ("The program has errors:\n" + string.Join ("\n", errors));
            }
            var layout = host.Layout;
            Run (host, c => {
                var result = c.Load (image, layout);
                if (!result.Ok)
                    throw new InvalidOperationException ("The computer rejected the program: " + result);
            });
        }

        static (byte[] Image, IReadOnlyList<GameDiagnostic> Diagnostics) Compile (
            KfsVehicleHost host, IDictionary<string, string> files)
        {
            if (files == null)
                throw new ArgumentNullException (nameof (files));
            var tables = KfsIntegration.Tables;
            var part = host.Computer.Part;
            var layout = host.Layout;
            if (files.Keys.Any (f => f.EndsWith (".c", StringComparison.OrdinalIgnoreCase))) {
                var result = global::KSA.Kfs.Compiler.Compiler.Compile (new CompileInput {
                    Files = new Dictionary<string, string> (files),
                    Part = part,
                    Game = tables,
                    Layout = layout,
                    ProjectName = "krpc"
                });
                return (result.Ok ? result.Image : null, result.Diagnostics);
            }
            var sources = files
                .Where (f => f.Key.EndsWith (".kasm", StringComparison.OrdinalIgnoreCase))
                .Select (f => new SourceFile (f.Key, f.Value))
                .ToList ();
            if (sources.Count == 0)
                throw new ArgumentException ("The program has no .c or .kasm file", nameof (files));
            var assembled = new Assembler (tables, part, layout) {
                ProjectFiles = name => files.TryGetValue (name, out var text) ? text : null
            }.Assemble (sources);
            return (assembled.Ok ? assembled.Image : null, assembled.Diagnostics);
        }

        static Diagnostic ToDiagnostic (GameDiagnostic diagnostic)
        {
            return new Diagnostic {
                Error = diagnostic.Severity == Severity.Error,
                Code = diagnostic.Code,
                File = diagnostic.File,
                Line = diagnostic.Line,
                Column = diagnostic.Col,
                Message = diagnostic.Message,
                Hint = diagnostic.Hint
            };
        }

        /// <summary>
        /// Start the loaded program. Throws an exception if no program is loaded.
        /// </summary>
        [KRPCMethod]
        public void Start ()
        {
            Run (Host, c => {
                if (!c.TryStart (c.DispatchTimeNs, ResetCause.PowerOn))
                    throw new InvalidOperationException ("No program is loaded");
            });
        }

        /// <summary>
        /// Restart the loaded program, as the reset button does. Throws an exception if no
        /// program is loaded.
        /// </summary>
        [KRPCMethod]
        public void Reset ()
        {
            Run (Host, c => {
                if (!c.TryStart (c.DispatchTimeNs, ResetCause.Player))
                    throw new InvalidOperationException ("No program is loaded");
            });
        }

        /// <summary>
        /// Stop the program, and give the vehicle back to the pilot. The program stays loaded.
        /// </summary>
        [KRPCMethod]
        public void Stop ()
        {
            Run (Host, c => c.Stop ());
        }

        /// <summary>
        /// Halt the program with the kill switch, and give the vehicle back to the pilot.
        /// </summary>
        /// <remarks>
        /// <see cref="Start"/> or <see cref="Reset"/> runs the program again.
        /// </remarks>
        [KRPCMethod]
        public void Kill ()
        {
            Run (Host, c => c.KillSwitch ());
        }

        // A change queued for the computer, made on the physics thread before its next tick
        sealed class Request
        {
            public volatile bool Done;
            public Exception Error;
        }

        void Run (KfsVehicleHost host, Action<VehicleComputer> action)
        {
            KFS.CheckAvailable ();
            var request = new Request ();
            host.Computer.Enqueue (c => {
                try {
                    action ((VehicleComputer)c);
                } catch (Exception e) {
                    request.Error = e;
                }
                request.Done = true;
            });
            Wait (request);
        }

        // Throws if the computer will never tick: the vehicle is gone, or kFS is off
        void CheckTicking ()
        {
            _ = vehicle.InternalVehicle;
            KFS.CheckAvailable ();
        }

        void Wait (Request request)
        {
            if (!request.Done) {
                CheckTicking ();
                throw new YieldException<Action> (() => Wait (request));
            }
            if (request.Error != null)
                ExceptionDispatchInfo.Capture (request.Error).Throw ();
        }

        /// <summary>
        /// The state of the computer.
        /// </summary>
        [KRPCProperty]
        public ComputerState State {
            get { return (ComputerState)InternalComputer.State; }
        }

        /// <summary>
        /// The control mode the loaded program claims.
        /// </summary>
        [KRPCProperty]
        public ControlMode Mode {
            get { return (ControlMode)InternalComputer.Mode; }
        }

        /// <summary>
        /// Whether the program controls the vehicle: it is running, or halted with its last
        /// commands held.
        /// </summary>
        [KRPCProperty]
        public bool UnderControl {
            get { return KfsPrototype.Enabled && InternalComputer.UnderKfsControl; }
        }

        /// <summary>
        /// The ticks per second of the loaded program, or zero if no program is loaded.
        /// </summary>
        [KRPCProperty]
        public int TickRate {
            get { return InternalComputer.TickHz; }
        }

        /// <summary>
        /// The processor cycles the latest tick used.
        /// </summary>
        [KRPCProperty]
        public long CyclesUsed {
            get { return InternalComputer.Snapshot.CyclesUsed; }
        }

        /// <summary>
        /// The processor cycles a tick may use.
        /// </summary>
        [KRPCProperty]
        public long CycleBudget {
            get { return InternalComputer.Snapshot.Budget; }
        }

        /// <summary>
        /// The number of ticks that ran past their budget since the program started.
        /// </summary>
        [KRPCProperty]
        public long Overruns {
            get { return InternalComputer.Snapshot.Overruns; }
        }

        /// <summary>
        /// The error that halted the program, or <c>null</c> if no error halted it.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public Trap? Trap {
            get {
                var trap = InternalComputer.Snapshot.Trap;
                if (trap == null)
                    return null;
                return new Trap {
                    Code = (TrapCode)trap.Code,
                    Function = trap.Function,
                    File = trap.File,
                    Line = trap.Line
                };
            }
        }

        /// <summary>
        /// The game's error that halted the computer, or <c>null</c> if there is none.
        /// </summary>
        [KRPCProperty (Nullable = true)]
        public string Fault {
            get { return InternalComputer.HostFault; }
        }

        /// <summary>
        /// The computer's console: the lines the program logs, and the computer's own messages.
        /// </summary>
        /// <remarks>
        /// The computer keeps the last 4,096 lines, and a program logs at most 10 lines a
        /// second.
        /// </remarks>
        [KRPCProperty]
        public IList<string> Log {
            get { return InternalComputer.Console.Snapshot ().Select (line => line.Text).ToList (); }
        }

        /// <summary>
        /// Clear the computer's console.
        /// </summary>
        [KRPCMethod]
        public void ClearLog ()
        {
            Run (Host, c => c.Console.Clear ());
        }

        /// <summary>
        /// The paths of the loaded program's global variables, one for each number in them,
        /// such as <c>command.args[2]</c>.
        /// </summary>
        [KRPCProperty]
        public IList<string> Globals {
            get { return InternalComputer.Leaves.Select (leaf => leaf.Path).ToList (); }
        }

        static GlobalLeaf Leaf (HostComputer computer, string path)
        {
            foreach (var leaf in computer.Leaves) {
                if (leaf.Path == path)
                    return leaf;
            }
            throw new ArgumentException ("The program has no global variable " + path);
        }

        /// <summary>
        /// The value of one of the program's global variables, as of its latest tick.
        /// </summary>
        /// <param name="path">The path of the variable, as in <see cref="Globals"/>.</param>
        [KRPCMethod]
        public double ReadGlobal (string path)
        {
            var computer = InternalComputer;
            var leaf = Leaf (computer, path);
            computer.WantRam ();
            var ram = computer.Snapshot.Ram;
            if (ram == null || ram.Length < leaf.Address + leaf.Size) {
                CheckTicking ();
                throw new YieldException<Func<double>> (() => ReadGlobal (path));
            }
            return leaf.Read (ram);
        }

        /// <summary>
        /// Write the program's global variables. The program sees all of them from the same
        /// tick.
        /// </summary>
        /// <remarks>
        /// A value is converted to the variable's type. Throws an exception if a value does not
        /// fit, such as a fraction for an integer.
        /// </remarks>
        /// <param name="values">The values, by the paths in <see cref="Globals"/>.</param>
        [KRPCMethod]
        public void WriteGlobals (IDictionary<string, double> values)
        {
            if (values == null)
                throw new ArgumentNullException (nameof (values));
            var computer = InternalComputer;
            foreach (var path in values.Keys)
                Leaf (computer, path);
            Run (Host, c => {
                var writes = values.Select (x => {
                    var leaf = Leaf (c, x.Key);
                    return (leaf.Address, Bytes: Encode (leaf, x.Value));
                }).ToList ();
                foreach (var (address, bytes) in writes)
                    c.Machine.WriteRam ((int)address, bytes);
                // The snapshot ReadGlobal reads from
                c.Republish ();
            });
        }

        static byte[] Encode (GlobalLeaf leaf, double value)
        {
            var bytes = new byte[leaf.Size];
            if (leaf.Kind == LeafKind.Float) {
                if (leaf.Size == 8)
                    BinaryPrimitives.WriteDoubleLittleEndian (bytes, value);
                else
                    BinaryPrimitives.WriteSingleLittleEndian (bytes, (float)value);
                return bytes;
            }
            if (leaf.Kind == LeafKind.Bool) {
                if (value != 0 && value != 1)
                    throw new ArgumentException (value + " does not fit in " + leaf.Path);
                bytes[0] = value != 0 ? (byte)1 : (byte)0;
                return bytes;
            }
            if (value != Math.Floor (value))
                throw new ArgumentException (leaf.Path + " is an integer, and " + value + " is not");
            // The low bytes of the 64-bit value, little endian
            var bits = leaf.Size * 8;
            var whole = new byte[8];
            if (leaf.Kind == LeafKind.Int) {
                var limit = Math.Pow (2, bits - 1);
                if (value < -limit || value >= limit)
                    throw new ArgumentException (value + " does not fit in " + leaf.Path);
                BinaryPrimitives.WriteInt64LittleEndian (whole, (long)value);
            } else {
                if (value < 0 || value >= Math.Pow (2, bits))
                    throw new ArgumentException (value + " does not fit in " + leaf.Path);
                BinaryPrimitives.WriteUInt64LittleEndian (whole, (ulong)value);
            }
            Array.Copy (whole, bytes, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// The names of the channels the program has traced to.
        /// </summary>
        [KRPCProperty]
        public IList<string> TraceChannels {
            get { return InternalComputer.Traces.Channels.ToList (); }
        }

        /// <summary>
        /// The latest value the program traced to a channel. Throws an exception if the program
        /// has not traced to it.
        /// </summary>
        /// <param name="channel">The name of the channel.</param>
        [KRPCMethod]
        public float Trace (string channel)
        {
            var samples = new List<TraceSample> (1);
            InternalComputer.Traces.CopyLast (channel, 1, samples);
            if (samples.Count == 0)
                throw new ArgumentException ("The program has not traced to " + channel);
            return samples[0].Value;
        }
    }
}
