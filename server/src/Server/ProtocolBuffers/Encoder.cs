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
    //FIXME: a lot of this code is very similar to the C# client. Should only implement this stuff once.
    //FIXME: this is not tested
    static class Encoder
    {
        public static Request DecodeRequest (byte[] buffer, int offset, int length)
        {
            // Attempt to deserialize a request from the buffered data
            var codedStream = new CodedInputStream (buffer, offset, length);
            try {
                //TODO: do something with the size that is read?
                codedStream.ReadUInt32 ();
                return Schema.KRPC.Request.Parser.ParseFrom (codedStream).ToRequest ();
            } catch (InvalidProtocolBufferException) {
                // Failed to deserialize a request
                if (length >= buffer.Length) {
                    // And the buffer is full
                    throw new RequestBufferOverflowException ();
                }
                // And the buffer not yet full
                // TODO: can we detect if the partial data received is a subset of a valid request?
                // And we read to the end, so we have a valid part of a request
                throw new NoRequestException ();
            }

            // Partial request is not complete, so some required fields weren't set
            //if (!bufferedRequest.IsInitialized) {
            //    throw new MalformedRequestException ();
            //}
        }

        public static byte[] EncodeResponse (Response response)
        {
            var message = response.ToProtobufResponse ();
            var bufferStream = new MemoryStream ();
            message.WriteDelimitedTo (bufferStream);
            return bufferStream.ToArray ();
        }

        public static byte[] EncodeStreamMessage (StreamMessage streamMessage)
        {
            var message = streamMessage.ToProtobufStreamMessage ();
            var bufferStream = new MemoryStream ();
            message.WriteDelimitedTo (bufferStream);
            return bufferStream.ToArray ();
        }

        /// <summary>
        /// Encode a value of the given type.
        /// </summary>
        public static ByteString Encode (object value)
        {
            var stream = new MemoryStream ();
            var encoder = new CodedOutputStream (stream);
            if (value == null) {
                encoder.WriteUInt64 (ObjectStore.Instance.AddInstance (value));
                encoder.Flush ();
                return ByteString.CopyFrom (stream.ToArray ());
            }
            Type type = value.GetType ();
            if (type == typeof(Double))
                encoder.WriteDouble ((Double)value);
            else if (type == typeof(Single))
                encoder.WriteFloat ((Single)value);
            else if (type == typeof(Int32))
                encoder.WriteInt32 ((Int32)value);
            else if (type == typeof(Int64))
                encoder.WriteInt64 ((Int64)value);
            else if (type == typeof(UInt32))
                encoder.WriteUInt32 ((UInt32)value);
            else if (type == typeof(UInt64))
                encoder.WriteUInt64 ((UInt64)value);
            else if (type == typeof(Boolean))
                encoder.WriteBool ((Boolean)value);
            else if (type == typeof(String))
                encoder.WriteString ((String)value);
            else if (type == typeof(byte[]))
                encoder.WriteBytes (ByteString.CopyFrom ((byte[])value));
            else if (value is Enum)
                encoder.WriteInt32 ((int)value);
            else if (TypeUtils.IsAClassType (type))
                encoder.WriteUInt64 (ObjectStore.Instance.AddInstance (value));
            else if (TypeUtils.IsAMessageType (type)) {
                Google.Protobuf.IMessage message;
                if (type == typeof(Status))
                    message = ((Status)value).ToProtobufStatus ();
                else if (type == typeof(Request))
                    message = ((Request)value).ToProtobufRequest ();
                else if (type == typeof(Response))
                    message = ((Response)value).ToProtobufResponse ();
                else if (type == typeof(Service.Messages.Services))
                    message = ((Service.Messages.Services)value).ToProtobufServices ();
                else
                    throw new NotImplementedException (type + " is not supported");
                message.WriteTo (stream);
                return ByteString.CopyFrom (stream.ToArray ());
            } else if (TypeUtils.IsAListCollectionType (type))
                return EncodeList (value, type);
            else if (TypeUtils.IsADictionaryCollectionType (type))
                return EncodeDictionary (value, type);
            else if (TypeUtils.IsASetCollectionType (type))
                return EncodeSet (value, type);
            else if (TypeUtils.IsATupleCollectionType (type))
                return EncodeTuple (value, type);
            else
                throw new ArgumentException (type + " is not a serializable type");
            encoder.Flush ();
            return ByteString.CopyFrom (stream.ToArray ());
        }

        static ByteString EncodeList (object value, Type type)
        {
            var encodedList = new KRPC.Schema.KRPC.List ();
            var list = (IList)value;
            foreach (var item in list)
                encodedList.Items.Add (Encode (item));
            return encodedList.ToByteString ();
        }

        static ByteString EncodeDictionary (object value, Type type)
        {
            var encodedDictionary = new KRPC.Schema.KRPC.Dictionary ();
            foreach (DictionaryEntry entry in (IDictionary) value) {
                var encodedEntry = new KRPC.Schema.KRPC.DictionaryEntry ();
                encodedEntry.Key = Encode (entry.Key);
                encodedEntry.Value = Encode (entry.Value);
                encodedDictionary.Entries.Add (encodedEntry);
            }
            return encodedDictionary.ToByteString ();
        }

        static ByteString EncodeSet (object value, Type type)
        {
            var encodedSet = new KRPC.Schema.KRPC.Set ();
            var set = (IEnumerable)value;
            foreach (var item in set)
                encodedSet.Items.Add (Encode (item));
            return encodedSet.ToByteString ();
        }

        static ByteString EncodeTuple (object value, Type type)
        {
            var encodedTuple = new KRPC.Schema.KRPC.Tuple ();
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("KRPC.Utils.Tuple`" + valueTypes.Length);
            var tupleType = genericType.MakeGenericType (valueTypes);
            for (int i = 0; i < valueTypes.Length; i++) {
                var property = tupleType.GetProperty ("Item" + (i + 1));
                var item = property.GetGetMethod ().Invoke (value, null);
                encodedTuple.Items.Add (Encode (item));
            }
            return encodedTuple.ToByteString ();
        }

        /// <summary>
        /// Decode a value of the given type.
        /// Should not be called directly. This interface is used by service client stubs.
        /// </summary>
        public static object Decode (ByteString value, Type type)
        {
            var stream = new CodedInputStream (value.ToByteArray ());
            if (type == typeof(double))
                return stream.ReadDouble ();
            else if (type == typeof(float))
                return stream.ReadFloat ();
            else if (type == typeof(int))
                return stream.ReadInt32 ();
            else if (type == typeof(long))
                return stream.ReadInt64 ();
            else if (type == typeof(uint))
                return stream.ReadUInt32 ();
            else if (type == typeof(ulong))
                return stream.ReadUInt64 ();
            else if (type == typeof(bool))
                return stream.ReadBool ();
            else if (type == typeof(string))
                return stream.ReadString ();
            else if (type == typeof(byte[]))
                return stream.ReadBytes ().ToByteArray ();
            else if (TypeUtils.IsAnEnumType (type))
                return Enum.ToObject (type, stream.ReadInt32 ());
            else if (TypeUtils.IsAClassType (type))
                return ObjectStore.Instance.GetInstance (stream.ReadUInt64 ());
            else if (TypeUtils.IsAMessageType (type)) {
                if (type == typeof(Request)) {
                    var message = new Schema.KRPC.Request ();
                    message.MergeFrom (stream);
                    return message.ToRequest ();
                } else if (type == typeof(Response)) {
                    var message = new Schema.KRPC.Response ();
                    message.MergeFrom (stream);
                    return message.ToResponse ();
                } else
                    throw new NotImplementedException ("Cannot decode protobuf message for " + type);

            } else if (TypeUtils.IsAListCollectionType (type))
                return DecodeList (value, type);
            else if (TypeUtils.IsADictionaryCollectionType (type))
                return DecodeDictionary (value, type);
            else if (TypeUtils.IsASetCollectionType (type))
                return DecodeSet (value, type);
            // TODO: ugly handing of tuple types
            else if (TypeUtils.IsATupleCollectionType (type))
                return DecodeTuple (value, type);
            throw new ArgumentException (type + " is not a serializable type");
        }

        static object DecodeList (ByteString value, Type type)
        {
            var encodedList = KRPC.Schema.KRPC.List.Parser.ParseFrom (value);
            var list = (IList)(typeof(System.Collections.Generic.List<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var item in encodedList.Items)
                list.Add (Decode (item, type.GetGenericArguments ().Single ()));
            return list;
        }

        static object DecodeDictionary (ByteString value, Type type)
        {
            var encodedDictionary = KRPC.Schema.KRPC.Dictionary.Parser.ParseFrom (value);
            var dictionary = (IDictionary)(typeof(System.Collections.Generic.Dictionary<,>)
                .MakeGenericType (type.GetGenericArguments () [0], type.GetGenericArguments () [1])
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            foreach (var entry in encodedDictionary.Entries) {
                var k = Decode (entry.Key, type.GetGenericArguments () [0]);
                var v = Decode (entry.Value, type.GetGenericArguments () [1]);
                dictionary [k] = v;
            }
            return dictionary;
        }

        static object DecodeSet (ByteString value, Type type)
        {
            var encodedSet = KRPC.Schema.KRPC.Set.Parser.ParseFrom (value);
            var set = (IEnumerable)(typeof(System.Collections.Generic.HashSet<>)
                .MakeGenericType (type.GetGenericArguments ().Single ())
                .GetConstructor (Type.EmptyTypes)
                .Invoke (null));
            MethodInfo methodInfo = type.GetMethod ("Add");
            foreach (var item in encodedSet.Items) {
                var decodedItem = Decode (item, type.GetGenericArguments ().Single ());
                methodInfo.Invoke (set, new [] { decodedItem });
            }
            return set;
        }

        static object DecodeTuple (ByteString value, Type type)
        {
            var encodedTuple = KRPC.Schema.KRPC.Tuple.Parser.ParseFrom (value);
            var valueTypes = type.GetGenericArguments ().ToArray ();
            var genericType = Type.GetType ("KRPC.Utils.Tuple`" + valueTypes.Length);
            var values = new Object[valueTypes.Length];
            for (int j = 0; j < valueTypes.Length; j++) {
                var item = encodedTuple.Items [j];
                values [j] = Decode (item, valueTypes [j]);
            }
            var tuple = genericType
                .MakeGenericType (valueTypes)
                .GetConstructor (valueTypes)
                .Invoke (values);
            return tuple;
        }
    }
}
