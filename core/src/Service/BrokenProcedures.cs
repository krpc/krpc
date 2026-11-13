using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using KRPC.Service.Scanner;
using KRPC.Utils;

namespace KRPC.Service
{
    /// <summary>
    /// Finds the procedures whose code uses a missing member, such as after a game or mod
    /// update.
    /// </summary>
    /// <remarks>
    /// We resolve every member token in the IL of the methods each procedure can reach, in the
    /// assemblies that reference this one, without running them. A member that keeps its
    /// signature but changes its behavior is not found.
    /// </remarks>
    public static class BrokenProcedures
    {
        /// <summary>
        /// The procedures that reach a missing member, each as its service, class and member
        /// names, e.g. <c>SpaceCenter.Vessel.Name</c>, with the missing member.
        /// </summary>
        public static IList<KeyValuePair<string, string>> Find ()
        {
            return Find (Services.Instance.Signatures.Values);
        }

        internal static IList<KeyValuePair<string, string>> Find (IEnumerable<ServiceSignature> services)
        {
            var core = typeof (BrokenProcedures).Assembly;
            var procedures = services.SelectMany (service => service.Procedures.Values.Select (
                procedure => new KeyValuePair<string, MethodBase> (DisplayName (service.Name, procedure.Name), procedure.Handler.Method)));
            return Find (procedures, AppDomain.CurrentDomain.GetAssemblies ().Where (x => x != core && !x.IsDynamic && Reflection.CanDeclare (x, core)));
        }

        /// <summary>
        /// The procedures, given by name and method, that reach a missing member, following the
        /// methods of the given assemblies.
        /// </summary>
        internal static IList<KeyValuePair<string, string>> Find (IEnumerable<KeyValuePair<string, MethodBase>> procedures, IEnumerable<Assembly> follow)
        {
            var checker = new Checker (follow);
            var result = new SortedDictionary<string, string> (StringComparer.Ordinal);
            foreach (var procedure in procedures) {
                var error = checker.Check (procedure.Value);
                if (error != null)
                    result [procedure.Key] = error;
            }
            return result.ToList ();
        }

        // A procedure name without its get_, set_ and static_ parts, e.g. Part_get_Mass is Part.Mass
        static string DisplayName (string service, string procedure)
        {
            var names = procedure.Split ('_').Where (x => x != "get" && x != "set" && x != "static");
            return service + "." + string.Join (".", names.ToArray ());
        }

        sealed class Checker
        {
            // The assemblies whose methods we follow
            readonly HashSet<Assembly> assemblies;
            // The error of each method checked, or null for none
            readonly Dictionary<MethodBase, string> errors = new Dictionary<MethodBase, string> ();

            public Checker (IEnumerable<Assembly> assemblies)
            {
                this.assemblies = new HashSet<Assembly> (assemblies);
            }

            /// <summary>
            /// The first missing member a method can reach, or null for none.
            /// </summary>
            public string Check (MethodBase method)
            {
                if (!assemblies.Contains (method.Module.Assembly))
                    return null;
                // The definition, for a method of a generic type or a generic method
                if (method.IsGenericMethod || (method.DeclaringType != null && method.DeclaringType.IsGenericType))
                    method = method.Module.ResolveMethod (method.MetadataToken);
                string error;
                if (errors.TryGetValue (method, out error))
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
                var initializer = method.DeclaringType == null ? null : method.DeclaringType.TypeInitializer;
                if (initializer != null && initializer != method) {
                    var initializerError = Check (initializer);
                    if (initializerError != null)
                        return initializerError;
                }
                byte [] il;
                try {
                    var body = method.GetMethodBody ();
                    if (body == null)
                        return null;
                    // Reading the locals resolves their types
                    if (body.LocalVariables == null)
                        return null;
                    il = body.GetILAsByteArray ();
                } catch (Exception e) {
                    if (!IsMissing (e))
                        throw;
                    return Describe (method, e);
                }
                var module = method.Module;
                var type = method.DeclaringType;
                var typeArguments = type != null && type.IsGenericType ? type.GetGenericArguments () : null;
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
                        } catch (Exception e) {
                            if (!IsMissing (e))
                                throw;
                            return Describe (method, e);
                        }
                        var callee = member as MethodBase;
                        if (callee != null) {
                            var calleeError = Check (callee);
                            if (calleeError != null)
                                return calleeError;
                        }
                        var constructor = member as ConstructorInfo;
                        if (constructor != null) {
                            var moveNext = StateMachine (constructor.DeclaringType);
                            var stateMachineError = moveNext == null ? null : Check (moveNext);
                            if (stateMachineError != null)
                                return stateMachineError;
                        }
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
                if (!type.IsDefined (typeof (CompilerGeneratedAttribute), false))
                    return null;
                if (!typeof (IEnumerator).IsAssignableFrom (type) && !typeof (IAsyncStateMachine).IsAssignableFrom (type))
                    return null;
                return type.GetMethod ("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }

            // A field reference that matches no field of the type, e.g. one that became a
            // property, resolves as a token not valid in the module's scope
            static bool IsMissing (Exception e)
            {
                return e is MissingMemberException || e is TypeLoadException || e is FileNotFoundException ||
                    e is FileLoadException || e is ArgumentOutOfRangeException;
            }

            static string Describe (MethodBase method, Exception e)
            {
                var message = e is ArgumentOutOfRangeException ? "Member not found" : e.Message;
                // Mono gives only the signature of a missing method or field
                if (e is MissingMemberException && message.IndexOf ("not found", StringComparison.OrdinalIgnoreCase) < 0)
                    message = "Not found: " + message;
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
