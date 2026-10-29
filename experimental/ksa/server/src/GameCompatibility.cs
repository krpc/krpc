using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using KSA;
using Logger = KRPC.Utils.Logger;

namespace KRPC.KSA
{
    /// <summary>
    /// Checks the mod against the running game, for a game version other than the one the
    /// mod was built against.
    /// </summary>
    /// <remarks>
    /// The mod refers to each game member by name and signature. A member missing from the
    /// running game throws when the method using it is first compiled, so only the RPCs that
    /// reach that method fail. We find them by resolving every member token in the IL of the
    /// methods each RPC can reach, without running them. A member that keeps its signature
    /// but changes its behavior is not found.
    /// </remarks>
    public static class GameCompatibility
    {
        /// <summary>
        /// The KSA version the mod was built against.
        /// </summary>
        public static Version BuiltFor {
            get { return typeof (GameCompatibility).Assembly.GetReferencedAssemblies ().First (x => x.Name == "KSA").Version; }
        }

        /// <summary>
        /// The version of the running game.
        /// </summary>
        public static Version Running {
            get { return typeof (Universe).Assembly.GetName ().Version; }
        }

        /// <summary>
        /// The RPCs that use game members missing from the running game, as service, class and
        /// member names. Logs the missing member of each, in one entry.
        /// </summary>
        public static IList<string> FindBreakages ()
        {
            var checker = new Checker ();
            var breakages = new SortedSet<string> (StringComparer.Ordinal);
            var details = new List<string> ();
            foreach (var service in KRPC.Service.Services.Instance.Signatures.Values) {
                foreach (var procedure in service.Procedures.Values) {
                    var error = checker.Check (procedure.Handler.Method);
                    if (error == null)
                        continue;
                    details.Add (procedure.FullyQualifiedName + ": " + error);
                    breakages.Add (DisplayName (service.Name, procedure.Name));
                }
            }
            if (details.Count > 0) {
                details.Sort (StringComparer.Ordinal);
                Logger.WriteLine ("RPCs broken by KSA " + Running + ":\n  " + string.Join ("\n  ", details));
            }
            return breakages.ToList ();
        }

        static readonly SortedSet<string> patchBreakages = new SortedSet<string> (StringComparer.Ordinal);

        /// <summary>
        /// The RPCs that need a game patch that failed to apply, as service, class and member
        /// names.
        /// </summary>
        public static IList<string> PatchBreakages {
            get { return patchBreakages.ToList (); }
        }

        /// <summary>
        /// Record a game patch that failed to apply, and log the RPCs that need it.
        /// </summary>
        public static void PatchFailed (string patch, string error, params string [] rpcs)
        {
            Logger.WriteLine ("Failed to patch " + patch + ", needed by " + string.Join (", ", rpcs) + ": " + error, Logger.Severity.Error);
            foreach (var rpc in rpcs)
                patchBreakages.Add (rpc);
        }

        // A procedure name without its get_, set_ and static_ parts, e.g. Part_get_Mass is Part.Mass
        static string DisplayName (string service, string procedure)
        {
            var names = procedure.Split ('_').Where (x => x != "get" && x != "set" && x != "static");
            return service + "." + string.Join (".", names);
        }

        sealed class Checker
        {
            // The mod's assemblies, whose methods we follow
            readonly HashSet<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies ()
                .Where (x => x.GetName ().Name.StartsWith ("KRPC.KSA", StringComparison.Ordinal)).ToHashSet ();
            // The error of each method checked, or null for none
            readonly Dictionary<MethodBase, string> errors = new Dictionary<MethodBase, string> ();

            /// <summary>
            /// The first missing member a method can reach, or null for none.
            /// </summary>
            public string Check (MethodBase method)
            {
                if (!assemblies.Contains (method.Module.Assembly))
                    return null;
                // The definition, for a method of a generic type or a generic method
                if (method.IsGenericMethod || method.DeclaringType is { IsGenericType: true })
                    method = method.Module.ResolveMethod (method.MetadataToken);
                if (errors.TryGetValue (method, out var error))
                    return error;
                // A method in a cycle counts as working until its scan finishes
                errors [method] = null;
                error = Scan (method);
                errors [method] = error;
                return error;
            }

            string Scan (MethodBase method)
            {
                // The type's static constructor runs before any of its members
                var initializer = method.DeclaringType?.TypeInitializer;
                if (initializer != null && initializer != method && Check (initializer) is string initializerError)
                    return initializerError;
                byte [] il;
                try {
                    var body = method.GetMethodBody ();
                    if (body == null)
                        return null;
                    _ = body.LocalVariables;
                    il = body.GetILAsByteArray ();
                } catch (Exception e) when (IsMissing (e)) {
                    return Describe (method, e);
                }
                var module = method.Module;
                var typeArguments = method.DeclaringType is { IsGenericType: true } type ? type.GetGenericArguments () : null;
                var methodArguments = method.IsGenericMethod ? method.GetGenericArguments () : null;
                for (var i = 0; i < il.Length;) {
                    var opCode = il [i] == 0xfe ? twoByteOpCodes [il [i + 1]] : oneByteOpCodes [il [i]];
                    i += opCode.Size;
                    var operandType = opCode.OperandType;
                    if (operandType == OperandType.InlineField || operandType == OperandType.InlineMethod ||
                        operandType == OperandType.InlineTok || operandType == OperandType.InlineType) {
                        MemberInfo member;
                        try {
                            member = Resolve (module, operandType, BitConverter.ToInt32 (il, i), typeArguments, methodArguments);
                        } catch (Exception e) when (IsMissing (e)) {
                            return Describe (method, e);
                        }
                        if (member is MethodBase callee && Check (callee) is string calleeError)
                            return calleeError;
                        if (member is ConstructorInfo constructor && StateMachine (constructor.DeclaringType) is MethodInfo moveNext &&
                            Check (moveNext) is string stateMachineError)
                            return stateMachineError;
                    }
                    i += OperandSize (operandType, il, i);
                }
                return null;
            }

            static MemberInfo Resolve (Module module, OperandType type, int token, Type [] typeArguments, Type [] methodArguments)
            {
                switch (type) {
                case OperandType.InlineField:
                    return module.ResolveField (token, typeArguments, methodArguments);
                case OperandType.InlineMethod:
                    return module.ResolveMethod (token, typeArguments, methodArguments);
                case OperandType.InlineType:
                    return module.ResolveType (token, typeArguments, methodArguments);
                default:
                    return module.ResolveMember (token, typeArguments, methodArguments);
                }
            }

            // The MoveNext of an iterator or async method, whose body runs from the state machine
            static MethodInfo StateMachine (Type type)
            {
                if (type.GetCustomAttribute<CompilerGeneratedAttribute> () == null)
                    return null;
                if (!typeof (IEnumerator).IsAssignableFrom (type) && !typeof (IAsyncStateMachine).IsAssignableFrom (type))
                    return null;
                return type.GetMethod ("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            // A field reference that matches no field of the game type, e.g. one that became a
            // property, resolves as a token not valid in the module's scope
            static bool IsMissing (Exception e)
            {
                return e is MissingMemberException || e is TypeLoadException || e is FileNotFoundException ||
                    e is FileLoadException || e is ArgumentOutOfRangeException;
            }

            static string Describe (MethodBase method, Exception e)
            {
                var message = e is ArgumentOutOfRangeException ? "Game member not found" : e.Message;
                return message + " (in " + method.DeclaringType + "." + method.Name + ")";
            }

            static int OperandSize (OperandType type, byte [] il, int offset)
            {
                switch (type) {
                case OperandType.InlineNone:
                    return 0;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    return 1;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineSwitch:
                    return 4 + 4 * BitConverter.ToInt32 (il, offset);
                default:
                    return 4;
                }
            }

            static readonly OpCode [] oneByteOpCodes = new OpCode [0x100];
            static readonly OpCode [] twoByteOpCodes = new OpCode [0x100];

            static Checker ()
            {
                foreach (var field in typeof (OpCodes).GetFields (BindingFlags.Public | BindingFlags.Static)) {
                    var opCode = (OpCode)field.GetValue (null);
                    var value = (ushort)opCode.Value;
                    if (opCode.Size == 1)
                        oneByteOpCodes [value] = opCode;
                    else
                        twoByteOpCodes [value & 0xff] = opCode;
                }
            }
        }
    }
}
