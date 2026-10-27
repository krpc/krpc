using System;
using System.Collections.Generic;
using System.Linq;
using Expression = KRPC.Client.Services.KRPC.Expression;
using TestClass = KRPC.Client.Services.TestService.TestClass;
using TestEnum = KRPC.Client.Services.TestService.TestEnum;
using TestStruct = KRPC.Client.Services.TestService.TestStruct;
using KRPC.Conformance;

namespace KRPC.Client.Conformance
{
    /// <summary>
    /// Server side functions, streamed and run at the types the driver names.
    /// </summary>
    static class Functions
    {
        sealed class Entry
        {
            public Func<Value, object> Arg;
            public Func<Connection, Expression, bool, IStreamHandle> AddStream;
            public Func<Connection, Expression, object[], Value> Run;
        }

        static Entry Make<T> (Func<Value, T> arg, Func<T, Value> result)
        {
            return new Entry {
                Arg = value => arg (value),
                AddStream = (connection, function, nullable) => Result.Stream (
                    connection.AddStream<T> (function), nullable ? result : value => NotNull (result (value))),
                Run = (connection, function, args) => result (connection.Run<T> (function, args))
            };
        }

        static readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry> {
            { "double", Make<double> (Arg.Double, Result.Double) },
            { "float", Make<float> (Arg.Float, Result.Float) },
            { "sint32", Make<int> (Arg.Sint32, Result.Sint32) },
            { "sint64", Make<long> (Arg.Sint64, Result.Sint64) },
            { "uint32", Make<uint> (Arg.Uint32, Result.Uint32) },
            { "uint64", Make<ulong> (Arg.Uint64, Result.Uint64) },
            { "bool", Make<bool> (Arg.Bool, Result.Bool) },
            { "string", Make<string> (Arg.String, Result.String) },
            { "bytes", Make<byte[]> (Arg.Bytes, Result.Bytes) },
            { "TestClass", Make<TestClass> (Arg.Object<TestClass>, Result.Object) },
            { "TestEnum", Make<TestEnum> (Arg.Enum<TestEnum>, Result.Enum<TestEnum> ()) },
            { "TestStruct", Make<TestStruct> (Dispatch.Arg_TestService_TestStruct, Dispatch.Result_TestService_TestStruct) },
            { "nullable sint32", Make<int?> (Arg.Nullable<int> (Arg.Sint32), Result.Nullable<int> (Result.Sint32)) },
            { "list(sint32)", Make<IList<int>> (Arg.List<int> (Arg.Sint32), Result.List<int> (Result.Sint32)) },
            { "list(sint64)", Make<IList<long>> (Arg.List<long> (Arg.Sint64), Result.List<long> (Result.Sint64)) },
            {
                "list(nullable sint32)",
                Make<IList<int?>> (
                    Arg.List (Arg.Nullable (Arg.Sint32)),
                    Result.List<int?> (Result.Nullable<int> (Result.Sint32)))
            },
            { "list(string)", Make<IList<string>> (Arg.List<string> (Arg.String), Result.List<string> (Result.String)) },
            {
                "list(nullable TestClass)",
                Make<IList<TestClass>> (Arg.List<TestClass> (Arg.Object<TestClass>), Result.List<TestClass> (Result.Object))
            },
            { "tuple(sint32)", Make<Tuple<int>> (Arg.Tuple<int> (Arg.Sint32), Result.Tuple<int> (Result.Sint32)) },
            { "set(sint32)", Make<ISet<int>> (Arg.Set<int> (Arg.Sint32), Result.Set<int> (Result.Sint32)) },
            {
                "tuple(sint32,bool)",
                Make<Tuple<int, bool>> (Arg.Tuple<int, bool> (Arg.Sint32, Arg.Bool), Result.Tuple<int, bool> (Result.Sint32, Result.Bool))
            },
            {
                "dictionary(string,list(sint32))",
                Make<IDictionary<string, IList<int>>> (
                    Arg.Dictionary<string, IList<int>> (Arg.String, Arg.List<int> (Arg.Sint32)),
                    Result.Dictionary<string, IList<int>> (Result.String, Result.List<int> (Result.Sint32)))
            },
            {
                "dictionary(string,nullable TestClass)",
                Make<IDictionary<string, TestClass>> (
                    Arg.Dictionary<string, TestClass> (Arg.String, Arg.Object<TestClass>),
                    Result.Dictionary<string, TestClass> (Result.String, Result.Object))
            },
            {
                "tuple(sint32,nullable TestClass)",
                Make<Tuple<int, TestClass>> (
                    Arg.Tuple<int, TestClass> (Arg.Sint32, Arg.Object<TestClass>),
                    Result.Tuple<int, TestClass> (Result.Sint32, Result.Object))
            }
        };

        static Entry Get (string type)
        {
            Entry entry;
            if (!entries.TryGetValue (type, out entry))
                throw new UnsupportedException ("A function of type " + type);
            return entry;
        }

        public static Value Call (Connection connection, Invoke invoke)
        {
            return Result.Object (Expression.Call (connection, Conformance.Dispatch.Call (connection, invoke)));
        }

        public static IStreamHandle AddStream (Connection connection, AddFunctionStream command)
        {
            var nullable = command.Type.StartsWith ("nullable ", StringComparison.Ordinal);
            return Get (command.Type).AddStream (connection, Arg.Object<Expression> (command.Function), nullable);
        }

        public static Value AddEvent (Connection connection, AddFunctionEvent command)
        {
            return Result.Event (connection.AddEvent (Arg.Object<Expression> (command.Function)));
        }

        public static Value Run (Connection connection, RunFunction command)
        {
            var function = Arg.Object<Expression> (command.Function);
            var args = command.Arguments.Select (
                (value, i) => value.ValueCase == Value.ValueOneofCase.Null ? null : Get (command.ArgumentTypes [i]).Arg (value)).ToArray ();
            if (command.Type.Length == 0) {
                connection.Run (function, args);
                return Result.None ();
            }
            var result = Get (command.Type).Run (connection, function, args);
            return command.Type.StartsWith ("nullable ", StringComparison.Ordinal) ? result : NotNull (result);
        }

        /// <summary>
        /// A function's value at a type named without "nullable". A null is unsupported, as the
        /// client's type can hold it.
        /// </summary>
        static Value NotNull (Value value)
        {
            if (value.ValueCase == Value.ValueOneofCase.Null)
                throw new UnsupportedException ("The client's type can hold null");
            return value;
        }
    }
}
