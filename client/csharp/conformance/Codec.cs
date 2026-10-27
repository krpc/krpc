using System;
using System.Collections.Generic;
using Google.Protobuf;
using KRPC.Client.Services.TestService;
using KRPC.Conformance;

namespace KRPC.Client.Conformance
{
    /// <summary>
    /// The client's encoder and decoder, for each codec type the driver names.
    /// </summary>
    static class Codec
    {
        sealed class Entry
        {
            public TypeSpec Spec;
            public Func<Value, object> Arg;
            public Func<object, Value> Result;
        }

        static Entry Make<T> (TypeSpec spec, Func<Value, T> arg, Func<T, Value> result)
        {
            return new Entry { Spec = spec, Arg = x => arg (x), Result = x => result ((T)x) };
        }

        static TypeSpec For<T> ()
        {
            return TypeSpec.For (typeof (T));
        }

        static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry> {
            { "double", Make (For<double> (), Arg.Double, Result.Double) },
            { "float", Make (For<float> (), Arg.Float, Result.Float) },
            { "sint32", Make (For<int> (), Arg.Sint32, Result.Sint32) },
            { "sint64", Make (For<long> (), Arg.Sint64, Result.Sint64) },
            { "uint32", Make (For<uint> (), Arg.Uint32, Result.Uint32) },
            { "uint64", Make (For<ulong> (), Arg.Uint64, Result.Uint64) },
            { "bool", Make (For<bool> (), Arg.Bool, Result.Bool) },
            { "string", Make (For<string> (), Arg.String, Result.String) },
            { "bytes", Make (For<byte[]> (), Arg.Bytes, Result.Bytes) },
            { "TestEnum", Make (For<TestEnum> (), Arg.Enum<TestEnum>, Result.Enum<TestEnum> ()) },
            { "TestClass", Make<TestClass> (For<TestClass> (), Arg.Object<TestClass>, Result.Object) },
            {
                "list(sint32)",
                Make (For<IList<int>> (), Arg.List (Arg.Sint32), Result.List<int> (Result.Sint32))
            },
            {
                "set(sint32)",
                Make (For<ISet<int>> (), Arg.Set (Arg.Sint32), Result.Set<int> (Result.Sint32))
            },
            {
                "dictionary(string,sint32)",
                Make (
                    For<IDictionary<string, int>> (),
                    Arg.Dictionary (Arg.String, Arg.Sint32),
                    Result.Dictionary<string, int> (Result.String, Result.Sint32))
            },
            {
                "tuple(sint32,bool)",
                Make (
                    For<Tuple<int, bool>> (),
                    Arg.Tuple (Arg.Sint32, Arg.Bool),
                    Result.Tuple<int, bool> (Result.Sint32, Result.Bool))
            },
            {
                "list(list(nullable TestClass))",
                Make (
                    new TypeSpec (typeof (IList<IList<TestClass>>), new TypeSpec (typeof (IList<TestClass>), TypeSpec.Null (typeof (TestClass)))),
                    Arg.List (Arg.List (Arg.Object<TestClass>)),
                    Result.List<IList<TestClass>> (Result.List<TestClass> (Result.Object)))
            },
            {
                "list(nullable sint32)",
                Make (
                    For<IList<int?>> (),
                    Arg.List (Arg.Nullable (Arg.Sint32)),
                    Result.List<int?> (Result.Nullable<int> (Result.Sint32)))
            },
            {
                "list(nullable bool)",
                Make (
                    For<IList<bool?>> (),
                    Arg.List (Arg.Nullable (Arg.Bool)),
                    Result.List<bool?> (Result.Nullable<bool> (Result.Bool)))
            },
            {
                "list(nullable bytes)",
                Make (
                    new TypeSpec (typeof (IList<byte[]>), TypeSpec.Null (typeof (byte[]))),
                    Arg.List (Arg.Bytes),
                    Result.List<byte[]> (Result.Bytes))
            },
            {
                "dictionary(string,nullable TestClass)",
                Make (
                    new TypeSpec (typeof (IDictionary<string, TestClass>), For<string> (), TypeSpec.Null (typeof (TestClass))),
                    Arg.Dictionary (Arg.String, Arg.Object<TestClass>),
                    Result.Dictionary<string, TestClass> (Result.String, Result.Object))
            },
            {
                "tuple(sint32,nullable TestClass)",
                Make (
                    new TypeSpec (typeof (Tuple<int, TestClass>), For<int> (), TypeSpec.Null (typeof (TestClass))),
                    Arg.Tuple (Arg.Sint32, Arg.Object<TestClass>),
                    Result.Tuple<int, TestClass> (Result.Sint32, Result.Object))
            },
            {
                "list(nullable TestClass)",
                Make (
                    new TypeSpec (typeof (IList<TestClass>), TypeSpec.Null (typeof (TestClass))),
                    Arg.List (Arg.Object<TestClass>),
                    Result.List<TestClass> (Result.Object))
            },
            {
                "tuple(uint32)",
                Make (For<Tuple<uint>> (), Arg.Tuple (Arg.Uint32), Result.Tuple<uint> (Result.Uint32))
            },
            {
                "tuple(uint32,string,bool)",
                Make (
                    For<Tuple<uint, string, bool>> (),
                    Arg.Tuple (Arg.Uint32, Arg.String, Arg.Bool),
                    Result.Tuple<uint, string, bool> (Result.Uint32, Result.String, Result.Bool))
            },
            {
                "tuple(sint32,sint64)",
                Make (
                    For<Tuple<int, long>> (),
                    Arg.Tuple (Arg.Sint32, Arg.Sint64),
                    Result.Tuple<int, long> (Result.Sint32, Result.Sint64))
            },
            {
                "tuple(sint32,nullable string)",
                Make (
                    new TypeSpec (typeof (Tuple<int, string>), For<int> (), TypeSpec.Null (typeof (string))),
                    Arg.Tuple (Arg.Sint32, Arg.String),
                    Result.Tuple<int, string> (Result.Sint32, Result.String))
            },
            {
                "dictionary(string,list(uint32))",
                Make (
                    For<IDictionary<string, IList<uint>>> (),
                    Arg.Dictionary (Arg.String, Arg.List (Arg.Uint32)),
                    Result.Dictionary<string, IList<uint>> (Result.String, Result.List<uint> (Result.Uint32)))
            },
            {
                "dictionary(string,nullable sint32)",
                Make (
                    For<IDictionary<string, int?>> (),
                    Arg.Dictionary (Arg.String, Arg.Nullable (Arg.Sint32)),
                    Result.Dictionary<string, int?> (Result.String, Result.Nullable<int> (Result.Sint32)))
            },
            {
                "list(nullable list(uint32))",
                Make (
                    new TypeSpec (typeof (IList<IList<uint>>), TypeSpec.Null (typeof (IList<uint>))),
                    Arg.List (Arg.List (Arg.Uint32)),
                    Result.List<IList<uint>> (Result.List<uint> (Result.Uint32)))
            },
            {
                "list(nullable TestStruct)",
                Make (
                    For<IList<TestStruct?>> (),
                    Arg.List (Arg.Nullable (Dispatch.Arg_TestService_TestStruct)),
                    Result.List<TestStruct?> (Result.Nullable<TestStruct> (Dispatch.Result_TestService_TestStruct)))
            },
            {
                "list(string)",
                Make (For<IList<string>> (), Arg.List (Arg.String), Result.List<string> (Result.String))
            },
            { "TestStruct", Make (For<TestStruct> (), Dispatch.Arg_TestService_TestStruct, Dispatch.Result_TestService_TestStruct) },
            {
                "TestNullableStruct",
                Make (For<TestNullableStruct> (), Dispatch.Arg_TestService_TestNullableStruct, Dispatch.Result_TestService_TestNullableStruct)
            },
        };

        static Entry Find (string type)
        {
            Entry entry;
            if (!entries.TryGetValue (type, out entry))
                throw new UnsupportedException ("The testee has no codec type " + type);
            return entry;
        }

        public static Value Encode (Encode command)
        {
            var entry = Find (command.Type);
            var data = Encoder.Encode (entry.Arg (command.Value), entry.Spec);
            return new Value { Bytes = data };
        }

        public static Value Decode (IConnection connection, Decode command)
        {
            var entry = Find (command.Type);
            return entry.Result (Encoder.Decode (command.Data, entry.Spec, connection));
        }
    }
}
