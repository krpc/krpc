using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Google.Protobuf;
using KRPC.Service;
using KRPC.Service.Messages;

namespace KRPC.Server.ProtocolBuffers
{
    static class Encoder
    {
        static MemoryStream cachedBuffer = new MemoryStream ();
        static CodedOutputStream cachedStream = new CodedOutputStream (cachedBuffer);

        /// <summary>
        /// Encode an object using the protocol buffer encoding scheme. The spec names the type
        /// the value is encoded as, and which of the positions inside it can hold null.
        /// </summary>
        public static ByteString Encode (object value, TypeSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException (nameof (spec));
            return EncodeObject (value, spec, cachedBuffer, cachedStream);
        }

        static ByteString EncodeObject (object value, TypeSpec spec, MemoryStream buffer, CodedOutputStream stream)
        {
            if (value == null) {
                throw new ArgumentNullException (nameof (value));
            }
            buffer.SetLength (0);
            WriteObject (value, spec, stream);
            stream.Flush ();
            return ByteString.CopyFrom (buffer.GetBuffer (), 0, (int)buffer.Length);
        }

        /// <summary>
        /// Encode a value at a position that allows a null: a presence bool, followed by the
        /// value itself when it is present.
        /// </summary>
        static ByteString EncodeNullableObject (object value, TypeSpec spec, MemoryStream buffer, CodedOutputStream stream)
        {
            buffer.SetLength (0);
            stream.WriteBool (value != null);
            if (value != null)
                WriteObject (value, spec, stream);
            stream.Flush ();
            return ByteString.CopyFrom (buffer.GetBuffer (), 0, (int)buffer.Length);
        }

        /// <summary>
        /// Encode one of the values a collection holds, at the position the given spec
        /// describes.
        /// </summary>
        static ByteString EncodeItem (object value, TypeSpec spec, MemoryStream buffer, CodedOutputStream stream)
        {
            if (spec.Nullable)
                return EncodeNullableObject (value, spec, buffer, stream);
            return EncodeObject (value, spec, buffer, stream);
        }

        /// <summary>
        /// The error for a null at a position that does not allow one. A null carries nothing
        /// about where it came from, so the message names the position and the type holding it.
        /// </summary>
        static ServiceException NullAt (string position, Type type, string reason)
        {
            return new ServiceException (position + " of " + type + " is null; " + reason);
        }

        static void WriteObject (object value, TypeSpec spec, CodedOutputStream stream)
        {
            switch (spec.Kind) {
            case TypeKind.Enum:
            case TypeKind.UndeclaredEnum:
                stream.WriteSInt32 ((int)value);
                break;
            case TypeKind.Double:
                stream.WriteDouble ((double)value);
                break;
            case TypeKind.Single:
                stream.WriteFloat ((float)value);
                break;
            case TypeKind.Int32:
                stream.WriteSInt32 ((int)value);
                break;
            case TypeKind.Int64:
                stream.WriteSInt64 ((long)value);
                break;
            case TypeKind.UInt32:
                stream.WriteUInt32 ((uint)value);
                break;
            case TypeKind.UInt64:
                stream.WriteUInt64 ((ulong)value);
                break;
            case TypeKind.Boolean:
                stream.WriteBool ((bool)value);
                break;
            case TypeKind.String:
                stream.WriteString ((string)value);
                break;
            case TypeKind.Bytes:
                stream.WriteBytes (ByteString.CopyFrom ((byte[])value));
                break;
            case TypeKind.Class:
                stream.WriteUInt64 (ObjectStore.Instance.AddInstance (value));
                break;
            case TypeKind.Struct:
                WriteStruct (value, spec, stream);
                break;
            case TypeKind.Tuple:
                WriteTuple (value, spec, stream);
                break;
            case TypeKind.List:
                WriteList (value, spec, stream);
                break;
            case TypeKind.Set:
                WriteSet (value, spec, stream);
                break;
            case TypeKind.Dictionary:
                WriteDictionary (value, spec, stream);
                break;
            case TypeKind.Message:
                WriteMessage (value, stream);
                break;
            default:
                throw new ArgumentException (spec.Type + " is not a serializable type");
            }
        }

        static void WriteTuple (object value, TypeSpec spec, CodedOutputStream stream)
        {
            var encodedTuple = new Schema.KRPC.Tuple ();
            var tupleType = spec.Type;
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                for (int i = 0; i < spec.Types.Count; i++) {
                    var property = tupleType.GetProperty ("Item" + (i + 1));
                    var item = property.GetGetMethod ().Invoke (value, null);
                    var itemSpec = spec.Types [i];
                    if (item == null && !itemSpec.Nullable)
                        throw NullAt (
                            "Item " + (i + 1), tupleType, "the item is not nullable");
                    encodedTuple.Items.Add (
                        EncodeItem (item, itemSpec, internalBuffer, internalStream));
                }
            }
            encodedTuple.WriteTo (stream);
        }

        /// <summary>
        /// Write a structure value as the values of its fields, in the order the structure
        /// declares them, which is the same encoding as a tuple of those values.
        /// </summary>
        static void WriteStruct (object value, TypeSpec spec, CodedOutputStream stream)
        {
            var encodedStruct = new Schema.KRPC.Tuple ();
            var type = spec.Type;
            System.Collections.Generic.IList<PropertyInfo> fields;
            System.Collections.Generic.IList<TypeSpec> specs;
            TypeUtils.GetStructFieldsAndSpecs (type, out fields, out specs);
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                for (int i = 0; i < fields.Count; i++) {
                    var field = fields [i];
                    var fieldSpec = specs [i];
                    var item = field.GetGetMethod ().Invoke (value, null);
                    if (item == null && !fieldSpec.Nullable)
                        throw NullAt (
                            "Field " + field.Name, type, "the field is not nullable");
                    encodedStruct.Items.Add (
                        EncodeItem (item, fieldSpec, internalBuffer, internalStream));
                }
            }
            encodedStruct.WriteTo (stream);
        }

        static void WriteList (object value, TypeSpec spec, CodedOutputStream stream)
        {
            var encodedList = new Schema.KRPC.List ();
            var list = (IList)value;
            var type = spec.Type;
            var itemSpec = spec.Types [0];
            var nullable = itemSpec.Nullable;
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (var item in list) {
                    if (item == null && !nullable)
                        throw NullAt ("An element", type, "the element is not nullable");
                    encodedList.Items.Add (
                        EncodeItem (item, itemSpec, internalBuffer, internalStream));
                }
            }
            encodedList.WriteTo (stream);
        }

        static void WriteSet (object value, TypeSpec spec, CodedOutputStream stream)
        {
            var encodedSet = new Schema.KRPC.Set ();
            var set = (IEnumerable)value;
            var type = spec.Type;
            var itemSpec = spec.Types [0];
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (var item in set) {
                    if (item == null)
                        throw NullAt ("An element", type, "a set element cannot be null");
                    encodedSet.Items.Add (
                        EncodeObject (item, itemSpec, internalBuffer, internalStream));
                }
            }
            encodedSet.WriteTo (stream);
        }

        static void WriteDictionary (object value, TypeSpec spec, CodedOutputStream stream)
        {
            var encodedDictionary = new Schema.KRPC.Dictionary ();
            var type = spec.Type;
            var keySpec = spec.Types [0];
            var valueSpec = spec.Types [1];
            var nullable = valueSpec.Nullable;
            using (var internalBuffer = new MemoryStream ()) {
                var internalStream = new CodedOutputStream (internalBuffer);
                foreach (DictionaryEntry entry in (IDictionary) value) {
                    if (entry.Key == null)
                        throw NullAt ("A key", type, "a dictionary key cannot be null");
                    if (entry.Value == null && !nullable)
                        throw NullAt ("A value", type, "the value is not nullable");
                    var encodedEntry = new Schema.KRPC.DictionaryEntry ();
                    encodedEntry.Key = EncodeObject (entry.Key, keySpec, internalBuffer, internalStream);
                    encodedEntry.Value = EncodeItem (entry.Value, valueSpec, internalBuffer, internalStream);
                    encodedDictionary.Entries.Add (encodedEntry);
                }
            }
            encodedDictionary.WriteTo (stream);
        }

        static void WriteMessage (object value, CodedOutputStream stream)
        {
            var savedCachedBuffer = cachedBuffer;
            var savedCachedStream = cachedStream;
            cachedBuffer = new MemoryStream ();
            cachedStream = new CodedOutputStream (cachedBuffer);
            Google.Protobuf.IMessage message = ((Service.Messages.IMessage)value).ToProtobufMessage ();
            cachedBuffer = savedCachedBuffer;
            cachedStream = savedCachedStream;
            message.WriteTo (stream);
        }

        /// <summary>
        /// Decode a value of the type the given spec describes, which says which of the
        /// positions inside it can hold null.
        /// </summary>
        public static object Decode (ByteString value, TypeSpec spec)
        {
            return DecodeValue (value.CreateCodedInput (), spec);
        }

        /// <summary>
        /// Decode a value at a position that allows a null, which EncodeNullableObject
        /// writes.
        /// </summary>
        static object DecodeNullable (ByteString value, TypeSpec spec)
        {
            var stream = value.CreateCodedInput ();
            return stream.ReadBool () ? DecodeValue (stream, spec) : null;
        }

        /// <summary>
        /// Decode one of the values a collection holds, at the position the given spec
        /// describes.
        /// </summary>
        static object DecodeItem (ByteString value, TypeSpec spec)
        {
            if (spec.Nullable)
                return DecodeNullable (value, spec);
            return DecodeValue (value.CreateCodedInput (), spec);
        }

        static object DecodeValue (CodedInputStream stream, TypeSpec spec)
        {
            // The spec names the type a value decodes as, with Nullable<T> already unwrapped;
            // the null itself is read by the caller, from the presence bool or a flag beside
            // the value
            var type = spec.Type;
            switch (spec.Kind) {
            case TypeKind.Enum:
                return Enum.ToObject (type, stream.ReadSInt32 ());
            case TypeKind.Double:
                return stream.ReadDouble ();
            case TypeKind.Single:
                return stream.ReadFloat ();
            case TypeKind.Int32:
                return stream.ReadSInt32 ();
            case TypeKind.Int64:
                return stream.ReadSInt64 ();
            case TypeKind.UInt32:
                return stream.ReadUInt32 ();
            case TypeKind.UInt64:
                return stream.ReadUInt64 ();
            case TypeKind.Boolean:
                return stream.ReadBool ();
            case TypeKind.String:
                return stream.ReadString ();
            case TypeKind.Bytes:
                return stream.ReadBytes ().ToByteArray ();
            case TypeKind.Class:
                return ObjectStore.Instance.GetInstance (stream.ReadUInt64 ());
            case TypeKind.Struct:
                return DecodeStruct (stream, type);
            case TypeKind.Tuple:
                return DecodeTuple (stream, spec);
            case TypeKind.List:
                return DecodeList (stream, spec);
            case TypeKind.Set:
                return DecodeSet (stream, spec);
            case TypeKind.Dictionary:
                return DecodeDictionary (stream, spec);
            case TypeKind.Message:
                return DecodeMessage (stream, type);
            }
            throw new ArgumentException (type + " is not a serializable type");
        }

        static object DecodeTuple (CodedInputStream stream, TypeSpec spec)
        {
            var encodedTuple = Schema.KRPC.Tuple.Parser.ParseFrom (stream);
            var valueTypes = spec.Types.Select (x => x.DeclaredType).ToArray ();
            var genericType = Type.GetType ("System.Tuple`" + valueTypes.Length);
            var values = new object[valueTypes.Length];
            for (int i = 0; i < valueTypes.Length; i++)
                values [i] = DecodeItem (encodedTuple.Items [i], spec.Types [i]);
            var tuple = genericType
                .MakeGenericType (valueTypes)
                .GetConstructor (valueTypes)
                .Invoke (values);
            return tuple;
        }

        /// <summary>
        /// Read a structure value from the values of its fields, in the order the structure
        /// declares them. Fields may only ever be appended to a structure, so items beyond the
        /// ones the structure declares come from a newer definition and are ignored. A null is
        /// only accepted for a field declared nullable.
        /// </summary>
        static object DecodeStruct (CodedInputStream stream, Type type)
        {
            var encodedStruct = Schema.KRPC.Tuple.Parser.ParseFrom (stream);
            System.Collections.Generic.IList<PropertyInfo> fields;
            System.Collections.Generic.IList<TypeSpec> specs;
            TypeUtils.GetStructFieldsAndSpecs (type, out fields, out specs);
            if (encodedStruct.Items.Count < fields.Count)
                throw new ArgumentException (
                    "Value for " + type.Name + " has " + encodedStruct.Items.Count +
                    " fields; expected at least " + fields.Count);
            var value = Activator.CreateInstance (type);
            for (int i = 0; i < fields.Count; i++) {
                var field = fields [i];
                var spec = specs [i];
                var item = DecodeItem (encodedStruct.Items [i], spec);
                field.GetSetMethod ().Invoke (value, new [] { item });
            }
            return value;
        }

        static object DecodeList (CodedInputStream stream, TypeSpec spec)
        {
            var encodedList = Schema.KRPC.List.Parser.ParseFrom (stream);
            var itemSpec = spec.Types [0];
            var list = (IList)(typeof(System.Collections.Generic.List<>)
                .MakeGenericType (itemSpec.DeclaredType)
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var item in encodedList.Items)
                list.Add (DecodeItem (item, itemSpec));
            return list;
        }

        static object DecodeSet (CodedInputStream stream, TypeSpec spec)
        {
            var encodedSet = Schema.KRPC.Set.Parser.ParseFrom (stream);
            var itemSpec = spec.Types [0];
            var set = (IEnumerable)(typeof(System.Collections.Generic.HashSet<>)
                .MakeGenericType (itemSpec.DeclaredType)
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            MethodInfo methodInfo = spec.Type.GetMethod ("Add");
            foreach (var item in encodedSet.Items) {
                var decodedItem = DecodeValue (item.CreateCodedInput (), itemSpec);
                methodInfo.Invoke (set, new [] { decodedItem });
            }
            return set;
        }

        static object DecodeDictionary (CodedInputStream stream, TypeSpec spec)
        {
            var encodedDictionary = Schema.KRPC.Dictionary.Parser.ParseFrom (stream);
            var keySpec = spec.Types [0];
            var valueSpec = spec.Types [1];
            var dictionary = (IDictionary)(typeof(System.Collections.Generic.Dictionary<,>)
                .MakeGenericType (keySpec.DeclaredType, valueSpec.DeclaredType)
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var entry in encodedDictionary.Entries) {
                var key = DecodeValue (entry.Key.CreateCodedInput (), keySpec);
                dictionary [key] = DecodeItem (entry.Value, valueSpec);
            }
            return dictionary;
        }

        static object DecodeMessage (CodedInputStream stream, Type type)
        {
            if (type == typeof(ProcedureCall)) {
                var message = new Schema.KRPC.ProcedureCall ();
                message.MergeFrom (stream);
                return message.ToMessage ();
            }
            throw new ArgumentException ("Cannot decode protocol buffer messages of type " + type);
        }
    }
}
