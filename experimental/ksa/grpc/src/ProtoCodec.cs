using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using KRPC.Service;
using KRPC.Service.Messages;
using KRPC.Service.Scanner;
using WireType = Google.Protobuf.WireFormat.WireType;

namespace KRPC.KSA.Grpc
{
    /// <summary>
    /// Encodes kRPC values as the fields of the typed messages in the generated .proto, which
    /// protogen.py writes by the same rules:
    ///
    /// The request message holds parameter i as field i + 1, and the response message holds
    /// the return value as field 1. A class value is a message holding its object id as field
    /// 1, with id 0 for null. A tuple is a message holding item i as field i + 1. A list or set
    /// is a repeated field, and a dictionary a map. A nullable scalar, or a scalar parameter
    /// with a default value, is an optional field.
    /// </summary>
    static class ProtoCodec
    {
        /// <summary>
        /// A field as read from the wire, before its type is known.
        /// </summary>
        readonly struct Raw
        {
            public Raw (WireType wireType, ulong number, ByteString bytes)
            {
                WireType = wireType;
                Number = number;
                Bytes = bytes;
            }

            public WireType WireType { get; }

            public ulong Number { get; }

            public ByteString Bytes { get; }
        }

        /// <summary>
        /// Whether the procedure's parameters and return value all have a typed form.
        /// </summary>
        public static bool IsSupported (ProcedureSignature procedure, out string reason)
        {
            foreach (var parameter in procedure.Parameters) {
                if (!IsSupported (parameter.Spec, true)) {
                    reason = "parameter " + parameter.Name + " has type " + parameter.Spec.Type;
                    return false;
                }
            }
            if (procedure.HasReturnType && !IsSupported (procedure.ReturnSpec, true)) {
                reason = "returns " + procedure.ReturnSpec.Type;
                return false;
            }
            reason = null;
            return true;
        }

        static bool IsSupported (TypeSpec spec, bool topLevel)
        {
            switch (spec.Kind) {
            case TypeKind.Double:
            case TypeKind.Single:
            case TypeKind.Int32:
            case TypeKind.Int64:
            case TypeKind.UInt32:
            case TypeKind.UInt64:
            case TypeKind.Boolean:
            case TypeKind.String:
            case TypeKind.Bytes:
            case TypeKind.Enum:
                // Only a field outside a container can be optional
                return topLevel || !spec.Nullable;
            case TypeKind.Class:
                return true;
            case TypeKind.Tuple:
                return !spec.Nullable && spec.Types.All (x => IsSupported (x, false));
            case TypeKind.List:
            case TypeKind.Set:
                return !spec.Nullable && IsElement (spec.Types [0]);
            case TypeKind.Dictionary:
                return !spec.Nullable && IsMapKey (spec.Types [0]) && IsElement (spec.Types [1]);
            default:
                return false;
            }
        }

        // Nested containers would need wrapper messages
        static bool IsElement (TypeSpec spec)
        {
            return !IsContainer (spec) && IsSupported (spec, false);
        }

        static bool IsContainer (TypeSpec spec)
        {
            return spec.Kind == TypeKind.List || spec.Kind == TypeKind.Set || spec.Kind == TypeKind.Dictionary;
        }

        static bool IsMapKey (TypeSpec spec)
        {
            switch (spec.Kind) {
            case TypeKind.String:
            case TypeKind.Int32:
            case TypeKind.Int64:
            case TypeKind.UInt32:
            case TypeKind.UInt64:
            case TypeKind.Boolean:
                return !spec.Nullable;
            default:
                return false;
            }
        }

        static bool IsPackable (TypeSpec spec)
        {
            switch (spec.Kind) {
            case TypeKind.Double:
            case TypeKind.Single:
            case TypeKind.Int32:
            case TypeKind.Int64:
            case TypeKind.UInt32:
            case TypeKind.UInt64:
            case TypeKind.Boolean:
            case TypeKind.Enum:
                return true;
            default:
                return false;
            }
        }

        /// <summary>
        /// Decode a request message into a call to the procedure.
        /// </summary>
        public static Request DecodeRequest (ProcedureSignature procedure, byte[] message)
        {
            var fields = ReadFields (ByteString.CopyFrom (message));
            var service = procedure.FullyQualifiedName.Substring (
                0, procedure.FullyQualifiedName.Length - procedure.Name.Length - 1);
            var call = new ProcedureCall (service, procedure.Name);
            call.CachedSignature = procedure;
            for (int i = 0; i < procedure.Parameters.Count; i++) {
                var parameter = procedure.Parameters [i];
                if (!fields.TryGetValue (i + 1, out var raws)) {
                    // The server core fills in the default value of an argument left out
                    if (parameter.HasDefaultValue)
                        continue;
                    raws = null;
                }
                call.Arguments.Add (new Argument ((uint)i, Decode (parameter.Spec, raws)));
            }
            var request = new Request ();
            request.Calls.Add (call);
            return request;
        }

        /// <summary>
        /// Encode the response message for a value returned by the procedure.
        /// </summary>
        public static byte[] EncodeResponse (ProcedureSignature procedure, object value)
        {
            if (!procedure.HasReturnType)
                return Array.Empty<byte> ();
            return Message (output => WriteField (output, 1, value, procedure.ReturnSpec));
        }

        static byte[] Message (Action<CodedOutputStream> write)
        {
            var buffer = new MemoryStream ();
            var output = new CodedOutputStream (buffer);
            write (output);
            output.Flush ();
            return buffer.ToArray ();
        }

        static void WriteMessageField (CodedOutputStream output, int field, Action<CodedOutputStream> write)
        {
            output.WriteTag (field, WireType.LengthDelimited);
            output.WriteBytes (ByteString.CopyFrom (Message (write)));
        }

        /// <summary>
        /// Write a field. A null leaves the field out.
        /// </summary>
        static void WriteField (CodedOutputStream output, int field, object value, TypeSpec spec)
        {
            if (value == null)
                return;
            switch (spec.Kind) {
            case TypeKind.List:
            case TypeKind.Set:
                WriteRepeated (output, field, (IEnumerable)value, spec.Types [0]);
                return;
            case TypeKind.Dictionary:
                foreach (DictionaryEntry entry in (IDictionary)value) {
                    WriteMessageField (output, field, entry_ => {
                        WriteField (entry_, 1, entry.Key, spec.Types [0]);
                        WriteElement (entry_, 2, entry.Value, spec.Types [1]);
                    });
                }
                return;
            }
            WriteElement (output, field, value, spec);
        }

        /// <summary>
        /// Write a value that is not a container. A null class is written as id 0, so that it
        /// keeps its place in a repeated field.
        /// </summary>
        static void WriteElement (CodedOutputStream output, int field, object value, TypeSpec spec)
        {
            switch (spec.Kind) {
            case TypeKind.Double:
                output.WriteTag (field, WireType.Fixed64);
                output.WriteDouble ((double)value);
                break;
            case TypeKind.Single:
                output.WriteTag (field, WireType.Fixed32);
                output.WriteFloat ((float)value);
                break;
            case TypeKind.String:
                output.WriteTag (field, WireType.LengthDelimited);
                output.WriteString ((string)value);
                break;
            case TypeKind.Bytes:
                output.WriteTag (field, WireType.LengthDelimited);
                output.WriteBytes (ByteString.CopyFrom ((byte[])value));
                break;
            case TypeKind.Class:
                var id = value == null ? 0 : ObjectStore.Instance.AddInstance (value);
                WriteMessageField (output, field, message => {
                    if (id != 0) {
                        message.WriteTag (1, WireType.Varint);
                        message.WriteUInt64 (id);
                    }
                });
                break;
            case TypeKind.Tuple:
                var tuple = (ITuple)value;
                WriteMessageField (output, field, message => {
                    for (int i = 0; i < spec.Types.Count; i++)
                        WriteField (message, i + 1, tuple [i], spec.Types [i]);
                });
                break;
            default:
                output.WriteTag (field, WireType.Varint);
                WriteVarint (output, value, spec);
                break;
            }
        }

        static void WriteVarint (CodedOutputStream output, object value, TypeSpec spec)
        {
            switch (spec.Kind) {
            case TypeKind.Int32:
                output.WriteSInt32 ((int)value);
                break;
            case TypeKind.Int64:
                output.WriteSInt64 ((long)value);
                break;
            case TypeKind.UInt32:
                output.WriteUInt32 ((uint)value);
                break;
            case TypeKind.UInt64:
                output.WriteUInt64 ((ulong)value);
                break;
            case TypeKind.Boolean:
                output.WriteBool ((bool)value);
                break;
            case TypeKind.Enum:
                output.WriteEnum (Convert.ToInt32 (value, System.Globalization.CultureInfo.InvariantCulture));
                break;
            default:
                throw new ArgumentException (spec.Type + " has no typed form");
            }
        }

        static void WriteRepeated (CodedOutputStream output, int field, IEnumerable items, TypeSpec spec)
        {
            if (!IsPackable (spec)) {
                foreach (var item in items)
                    WriteElement (output, field, item, spec);
                return;
            }
            WriteMessageField (output, field, packed => {
                foreach (var item in items) {
                    switch (spec.Kind) {
                    case TypeKind.Double:
                        packed.WriteDouble ((double)item);
                        break;
                    case TypeKind.Single:
                        packed.WriteFloat ((float)item);
                        break;
                    default:
                        WriteVarint (packed, item, spec);
                        break;
                    }
                }
            });
        }

        static Dictionary<int, List<Raw>> ReadFields (ByteString message)
        {
            var fields = new Dictionary<int, List<Raw>> ();
            var input = message.CreateCodedInput ();
            uint tag;
            while ((tag = input.ReadTag ()) != 0) {
                var wireType = WireFormat.GetTagWireType (tag);
                Raw raw;
                switch (wireType) {
                case WireType.Varint:
                    raw = new Raw (wireType, input.ReadUInt64 (), null);
                    break;
                case WireType.Fixed64:
                    raw = new Raw (wireType, input.ReadFixed64 (), null);
                    break;
                case WireType.Fixed32:
                    raw = new Raw (wireType, input.ReadFixed32 (), null);
                    break;
                case WireType.LengthDelimited:
                    raw = new Raw (wireType, 0, input.ReadBytes ());
                    break;
                default:
                    input.SkipLastField ();
                    continue;
                }
                var field = WireFormat.GetTagFieldNumber (tag);
                if (!fields.TryGetValue (field, out var raws))
                    fields [field] = raws = new List<Raw> ();
                raws.Add (raw);
            }
            return fields;
        }

        static List<Raw> Field (Dictionary<int, List<Raw>> fields, int field)
        {
            return fields.TryGetValue (field, out var raws) ? raws : null;
        }

        /// <summary>
        /// Decode a field from its occurrences, or null when it is absent. An absent field takes
        /// the proto3 default, except that it is null for a nullable scalar.
        /// </summary>
        static object Decode (TypeSpec spec, List<Raw> raws)
        {
            switch (spec.Kind) {
            case TypeKind.List:
                return DecodeList (spec, raws, typeof (List<>));
            case TypeKind.Set:
                return DecodeList (spec, raws, typeof (HashSet<>));
            case TypeKind.Dictionary:
                return DecodeDictionary (spec, raws);
            }
            if (raws == null || raws.Count == 0) {
                if (spec.Nullable && spec.Kind != TypeKind.Tuple)
                    return null;
                return DecodeElement (spec, Default (spec));
            }
            // The last occurrence of a singular field wins
            return DecodeElement (spec, raws [raws.Count - 1]);
        }

        static Raw Default (TypeSpec spec)
        {
            switch (spec.Kind) {
            case TypeKind.Double:
                return new Raw (WireType.Fixed64, 0, null);
            case TypeKind.Single:
                return new Raw (WireType.Fixed32, 0, null);
            case TypeKind.String:
            case TypeKind.Bytes:
            case TypeKind.Class:
            case TypeKind.Tuple:
                return new Raw (WireType.LengthDelimited, 0, ByteString.Empty);
            default:
                return new Raw (WireType.Varint, 0, null);
            }
        }

        static object DecodeElement (TypeSpec spec, Raw raw)
        {
            switch (spec.Kind) {
            case TypeKind.Double:
                Expect (raw, WireType.Fixed64, spec);
                return BitConverter.Int64BitsToDouble ((long)raw.Number);
            case TypeKind.Single:
                Expect (raw, WireType.Fixed32, spec);
                return BitConverter.Int32BitsToSingle ((int)raw.Number);
            case TypeKind.Int32:
                Expect (raw, WireType.Varint, spec);
                return (int)((uint)raw.Number >> 1) ^ -(int)(raw.Number & 1);
            case TypeKind.Int64:
                Expect (raw, WireType.Varint, spec);
                return (long)(raw.Number >> 1) ^ -(long)(raw.Number & 1);
            case TypeKind.UInt32:
                Expect (raw, WireType.Varint, spec);
                return (uint)raw.Number;
            case TypeKind.UInt64:
                Expect (raw, WireType.Varint, spec);
                return raw.Number;
            case TypeKind.Boolean:
                Expect (raw, WireType.Varint, spec);
                return raw.Number != 0;
            case TypeKind.Enum:
                Expect (raw, WireType.Varint, spec);
                return Enum.ToObject (spec.Type, (int)raw.Number);
            case TypeKind.String:
                Expect (raw, WireType.LengthDelimited, spec);
                return raw.Bytes.ToStringUtf8 ();
            case TypeKind.Bytes:
                Expect (raw, WireType.LengthDelimited, spec);
                return raw.Bytes.ToByteArray ();
            case TypeKind.Class:
                Expect (raw, WireType.LengthDelimited, spec);
                var idField = Field (ReadFields (raw.Bytes), 1);
                var id = idField == null ? 0 : idField [idField.Count - 1].Number;
                return id == 0 ? null : ObjectStore.Instance.GetInstance (id);
            case TypeKind.Tuple:
                Expect (raw, WireType.LengthDelimited, spec);
                var fields = ReadFields (raw.Bytes);
                var types = spec.Types.Select (x => x.DeclaredType).ToArray ();
                var values = new object [types.Length];
                for (int i = 0; i < types.Length; i++)
                    values [i] = Decode (spec.Types [i], Field (fields, i + 1));
                return Type.GetType ("System.Tuple`" + types.Length)
                    .MakeGenericType (types).GetConstructor (types).Invoke (values);
            default:
                throw new ArgumentException (spec.Type + " has no typed form");
            }
        }

        static void Expect (Raw raw, WireType wireType, TypeSpec spec)
        {
            if (raw.WireType != wireType)
                throw new ArgumentException ("Wire type " + raw.WireType + " is not a " + spec.Type);
        }

        static object DecodeList (TypeSpec spec, List<Raw> raws, Type collectionType)
        {
            var itemSpec = spec.Types [0];
            var collection = Activator.CreateInstance (collectionType.MakeGenericType (itemSpec.DeclaredType));
            var add = collection.GetType ().GetMethod ("Add");
            if (raws == null)
                return collection;
            foreach (var raw in raws) {
                if (IsPackable (itemSpec) && raw.WireType == WireType.LengthDelimited) {
                    var input = raw.Bytes.CreateCodedInput ();
                    while (!input.IsAtEnd) {
                        Raw item;
                        if (itemSpec.Kind == TypeKind.Double)
                            item = new Raw (WireType.Fixed64, input.ReadFixed64 (), null);
                        else if (itemSpec.Kind == TypeKind.Single)
                            item = new Raw (WireType.Fixed32, input.ReadFixed32 (), null);
                        else
                            item = new Raw (WireType.Varint, input.ReadUInt64 (), null);
                        add.Invoke (collection, new [] { DecodeElement (itemSpec, item) });
                    }
                } else {
                    add.Invoke (collection, new [] { DecodeElement (itemSpec, raw) });
                }
            }
            return collection;
        }

        static object DecodeDictionary (TypeSpec spec, List<Raw> raws)
        {
            var keySpec = spec.Types [0];
            var valueSpec = spec.Types [1];
            var dictionary = (IDictionary)Activator.CreateInstance (
                typeof (Dictionary<,>).MakeGenericType (keySpec.DeclaredType, valueSpec.DeclaredType));
            if (raws == null)
                return dictionary;
            foreach (var raw in raws) {
                Expect (raw, WireType.LengthDelimited, spec);
                var entry = ReadFields (raw.Bytes);
                dictionary [Decode (keySpec, Field (entry, 1))] = Decode (valueSpec, Field (entry, 2));
            }
            return dictionary;
        }
    }
}
