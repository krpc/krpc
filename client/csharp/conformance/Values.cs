using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using KRPC.Conformance;

namespace KRPC.Client.Conformance
{
    /// <summary>
    /// The objects and streams the testee holds, by handle. Each object a call returns gets a
    /// new handle, so two handles can refer to one server object.
    /// </summary>
    static class Handles
    {
        static readonly object handlesLock = new object ();
        static readonly Dictionary<ulong, object> handles = new Dictionary<ulong, object> ();
        static ulong nextHandle = 1;

        public static void Remove (ulong handle)
        {
            lock (handlesLock)
                handles.Remove (handle);
        }

        public static ulong Add (object value)
        {
            lock (handlesLock) {
                var handle = nextHandle++;
                handles [handle] = value;
                return handle;
            }
        }

        public static object Get (ulong handle)
        {
            lock (handlesLock) {
                return handles [handle];
            }
        }

        public static void Clear ()
        {
            lock (handlesLock) {
                handles.Clear ();
            }
        }
    }

    /// <summary>
    /// Arguments from conformance Values, converted without the client's encoder.
    /// </summary>
    static class Arg
    {
        public static double Double (Value value) { return NotNull (value).Double; }
        public static float Float (Value value) { return NotNull (value).Float; }
        public static int Sint32 (Value value) { return NotNull (value).Sint32; }
        public static long Sint64 (Value value) { return NotNull (value).Sint64; }
        public static uint Uint32 (Value value) { return NotNull (value).Uint32; }
        public static ulong Uint64 (Value value) { return NotNull (value).Uint64; }
        public static bool Bool (Value value) { return NotNull (value).Bool; }

        /// <summary>
        /// The value, which must not be null, as the client's type for it cannot hold null.
        /// </summary>
        static Value NotNull (Value value)
        {
            if (IsNull (value))
                throw new UnsupportedException ("The client's type cannot hold null");
            return value;
        }

        public static string String (Value value)
        {
            return value.ValueCase == Value.ValueOneofCase.Null ? null : value.String;
        }

        public static byte[] Bytes (Value value)
        {
            return value.ValueCase == Value.ValueOneofCase.Null ? null : value.Bytes.ToByteArray ();
        }

        /// <summary>
        /// The connection an object made from an id belongs to.
        /// </summary>
        public static IConnection Connection { get; set; }

        public static T Object<T> (Value value) where T : RemoteObject
        {
            // A handle of 0 asks for an object of the class with the given id
            if (value.ValueCase == KRPC.Conformance.Value.ValueOneofCase.Object && value.Object.Handle == 0)
                return (T)Activator.CreateInstance (typeof (T), Connection, value.Object.Id);
            return (T)Any (value);
        }

        public static T Enum<T> (Value value)
        {
            return (T)global::System.Enum.ToObject (typeof (T), NotNull (value).Enum.Value);
        }

        public static Func<Value, T?> Nullable<T> (Func<Value, T> convert) where T : struct
        {
            return value => IsNull (value) ? (T?)null : convert (value);
        }

        public static Func<Value, IList<T>> List<T> (Func<Value, T> item)
        {
            return value => IsNull (value) ? null : value.List.Items_.Select (item).ToList ();
        }

        public static Func<Value, ISet<T>> Set<T> (Func<Value, T> item)
        {
            return value => IsNull (value) ? null : new HashSet<T> (value.Set.Items_.Select (item));
        }

        public static Func<Value, IDictionary<TKey, TValue>> Dictionary<TKey, TValue> (
            Func<Value, TKey> key, Func<Value, TValue> item)
        {
            return value => IsNull (value) ? null : value.Dictionary.Entries_.ToDictionary (
                x => key (x.Key), x => item (x.Value));
        }

        public static Func<Value, Tuple<T1>> Tuple<T1> (Func<Value, T1> item1)
        {
            return value => IsNull (value) ? null : global::System.Tuple.Create (
                item1 (value.Tuple.Items_ [0]));
        }

        public static Func<Value, Tuple<T1, T2>> Tuple<T1, T2> (
            Func<Value, T1> item1, Func<Value, T2> item2)
        {
            return value => IsNull (value) ? null : global::System.Tuple.Create (
                item1 (value.Tuple.Items_ [0]), item2 (value.Tuple.Items_ [1]));
        }

        public static Func<Value, Tuple<T1, T2, T3>> Tuple<T1, T2, T3> (
            Func<Value, T1> item1, Func<Value, T2> item2, Func<Value, T3> item3)
        {
            return value => IsNull (value) ? null : global::System.Tuple.Create (
                item1 (value.Tuple.Items_ [0]), item2 (value.Tuple.Items_ [1]),
                item3 (value.Tuple.Items_ [2]));
        }

        public static Func<Value, Tuple<T1, T2, T3, T4>> Tuple<T1, T2, T3, T4> (
            Func<Value, T1> item1, Func<Value, T2> item2, Func<Value, T3> item3,
            Func<Value, T4> item4)
        {
            return value => IsNull (value) ? null : global::System.Tuple.Create (
                item1 (value.Tuple.Items_ [0]), item2 (value.Tuple.Items_ [1]),
                item3 (value.Tuple.Items_ [2]), item4 (value.Tuple.Items_ [3]));
        }

        static bool IsNull (Value value)
        {
            return value.ValueCase == Value.ValueOneofCase.None || value.ValueCase == Value.ValueOneofCase.Null;
        }

        /// <summary>
        /// The value of any kind, with an object as the object its handle refers to.
        /// </summary>
        public static object Any (Value value)
        {
            switch (value.ValueCase) {
            case KRPC.Conformance.Value.ValueOneofCase.None:
            case KRPC.Conformance.Value.ValueOneofCase.Null:
                return null;
            case KRPC.Conformance.Value.ValueOneofCase.Object:
                return Handles.Get (value.Object.Handle);
            case KRPC.Conformance.Value.ValueOneofCase.Enum:
                return Enum<global::KRPC.Client.Services.TestService.TestEnum> (value);
            case KRPC.Conformance.Value.ValueOneofCase.Struct:
                return Dispatch.Arg_TestService_TestStruct (value);
            default:
                throw new NotSupportedException ("No conversion for " + value.ValueCase);
            }
        }
    }

    /// <summary>
    /// Results as conformance Values, converted without the client's decoder.
    /// </summary>
    static class Result
    {
        public static Value None () { return new Value (); }
        public static Value Double (double value) { return new Value { Double = value }; }
        public static Value Float (float value) { return new Value { Float = value }; }
        public static Value Sint32 (int value) { return new Value { Sint32 = value }; }
        public static Value Sint64 (long value) { return new Value { Sint64 = value }; }
        public static Value Uint32 (uint value) { return new Value { Uint32 = value }; }
        public static Value Uint64 (ulong value) { return new Value { Uint64 = value }; }
        public static Value Bool (bool value) { return new Value { Bool = value }; }

        public static Value String (string value)
        {
            return value == null ? new Value { Null = true } : new Value { String = value };
        }

        public static Value Bytes (byte[] value)
        {
            return value == null ? new Value { Null = true } : new Value { Bytes = ByteString.CopyFrom (value) };
        }

        public static Value Event (Event value)
        {
            return new Value { Event = Handles.Add (value) };
        }

        public static Value Message (IMessage value)
        {
            return new Value { Message = value.ToByteString () };
        }

        public static Value Object (RemoteObject value)
        {
            if (value == null)
                return Null ();
            var obj = new KRPC.Conformance.Object {
                Handle = Handles.Add (value),
                Id = RemoteObject.GetObjectId (value)
            };
            return new Value { Object = obj };
        }

        public static Func<T, Value> Enum<T> ()
        {
            return x => new Value { Enum = new KRPC.Conformance.Enum { Value = Convert.ToInt32 (x, global::System.Globalization.CultureInfo.InvariantCulture) } };
        }

        public static Func<T?, Value> Nullable<T> (Func<T, Value> convert) where T : struct
        {
            return x => x.HasValue ? convert (x.Value) : Null ();
        }

        public static Func<IList<T>, Value> List<T> (Func<T, Value> item)
        {
            return x => x == null ? Null () : new Value { List = Items (x.Select (item)) };
        }

        public static Func<ISet<T>, Value> Set<T> (Func<T, Value> item)
        {
            return x => x == null ? Null () : new Value { Set = Items (x.Select (item)) };
        }

        public static Func<IDictionary<TKey, TValue>, Value> Dictionary<TKey, TValue> (
            Func<TKey, Value> key, Func<TValue, Value> item)
        {
            return x => {
                if (x == null)
                    return Null ();
                var entries = new Entries ();
                foreach (var entry in x)
                    entries.Entries_.Add (new Entry { Key = key (entry.Key), Value = item (entry.Value) });
                return new Value { Dictionary = entries };
            };
        }

        public static Func<Tuple<T1>, Value> Tuple<T1> (Func<T1, Value> item1)
        {
            return x => x == null ? Null () : new Value { Tuple = Items (new [] { item1 (x.Item1) }) };
        }

        public static Func<Tuple<T1, T2>, Value> Tuple<T1, T2> (
            Func<T1, Value> item1, Func<T2, Value> item2)
        {
            return x => x == null ? Null () : new Value {
                Tuple = Items (new [] { item1 (x.Item1), item2 (x.Item2) })
            };
        }

        public static Func<Tuple<T1, T2, T3>, Value> Tuple<T1, T2, T3> (
            Func<T1, Value> item1, Func<T2, Value> item2, Func<T3, Value> item3)
        {
            return x => x == null ? Null () : new Value {
                Tuple = Items (new [] { item1 (x.Item1), item2 (x.Item2), item3 (x.Item3) })
            };
        }

        public static Func<Tuple<T1, T2, T3, T4>, Value> Tuple<T1, T2, T3, T4> (
            Func<T1, Value> item1, Func<T2, Value> item2, Func<T3, Value> item3,
            Func<T4, Value> item4)
        {
            return x => x == null ? Null () : new Value {
                Tuple = Items (new [] { item1 (x.Item1), item2 (x.Item2), item3 (x.Item3), item4 (x.Item4) })
            };
        }

        static Value Null ()
        {
            return new Value { Null = true };
        }

        static Items Items (IEnumerable<Value> values)
        {
            var items = new Items ();
            items.Items_.AddRange (values);
            return items;
        }

        public static IStreamHandle Stream<T> (Stream<T> stream, Func<T, Value> convert)
        {
            return new StreamHandle<T> (stream, convert);
        }
    }

    /// <summary>
    /// A stream, with its values converted to conformance Values.
    /// </summary>
    interface IStreamHandle
    {
        Value Get ();

        bool Wait (double timeout);

        float Rate { get; set; }

        object Condition { get; }

        /// <summary>
        /// The client's stream object.
        /// </summary>
        object Stream { get; }

        void Remove ();

        int AddCallback (Action<Value> callback);

        void RemoveCallback (int tag);

        int AddEndCallback (Action<Exception> callback);

        void RemoveEndCallback (int tag);
    }

    sealed class StreamHandle<T> : IStreamHandle
    {
        readonly Stream<T> stream;
        readonly Func<T, Value> convert;

        public StreamHandle (Stream<T> stream, Func<T, Value> convert)
        {
            this.stream = stream;
            this.convert = convert;
        }

        public Value Get ()
        {
            return convert (stream.Get ());
        }

        public bool Wait (double timeout)
        {
            lock (stream.Condition) {
                return stream.Wait (timeout);
            }
        }

        public float Rate {
            get { return stream.Rate; }
            set { stream.Rate = value; }
        }

        public object Condition {
            get { return stream.Condition; }
        }

        public object Stream {
            get { return stream; }
        }

        public void Remove ()
        {
            stream.Remove ();
        }

        public int AddCallback (Action<Value> callback)
        {
            return stream.AddCallback (x => callback (convert (x)));
        }

        public void RemoveCallback (int tag)
        {
            stream.RemoveCallback (tag);
        }

        public int AddEndCallback (Action<Exception> callback)
        {
            return stream.AddEndCallback (callback);
        }

        public void RemoveEndCallback (int tag)
        {
            stream.RemoveEndCallback (tag);
        }
    }
}
