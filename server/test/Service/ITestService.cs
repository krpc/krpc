using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace KRPC.Test.Service
{
    public interface ITestService
    {
        void ProcedureWithoutAttribute ();

        void ProcedureNoArgsNoReturn ();

        void ProcedureSingleArgNoReturn (string x);

        void ProcedureThreeArgsNoReturn (string x, int y, string z);

        string ProcedureNoArgsReturns ();

        string ProcedureSingleArgReturns (string x);

        string PropertyWithGetAndSet { get; set; }

        string PropertyWithGet { get; }

        [SuppressMessage ("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        string PropertyWithSet { set; }

        TestService.TestClass CreateTestObject (string value);

        void DeleteTestObject (TestService.TestClass obj);

        TestService.TestClass EchoTestObject (TestService.TestClass obj);

        TestService.TestClass ReturnNullWhenNotAllowed ();

        void ProcedureSingleOptionalArgNoReturn (string x);

        void ProcedureThreeOptionalArgsNoReturn (float x, string y, int z);

        void ProcedureOptionalNullArg (TestService.TestClass x);

        void ProcedureEnumArg (TestService.TestEnum x);

        TestService.TestEnum ProcedureEnumReturn ();

        void BlockingProcedureNoReturn (int n);

        int BlockingProcedureReturns (int n, int sum);

        IList<string> EchoList (IList<string> l);

        IDictionary<int,string> EchoDictionary (IDictionary<int,string> d);

        HashSet<int> EchoSet (HashSet<int> h);

        Tuple<int,bool> EchoTuple (Tuple<int,bool> t);

        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        IDictionary<int,IList<string>> EchoNestedCollection (IDictionary<int,IList<string>> c);

        IList<TestService.TestClass> EchoListOfObjects (IList<TestService.TestClass> l);

        Tuple<int,bool> TupleDefault (Tuple<int,bool> x);

        IList<int> ListDefault (IList<int> x);

        HashSet<int> SetDefault (HashSet<int> x);

        IDictionary<int,bool> DictionaryDefault (IDictionary<int,bool> x);

        void ProcedureAvailableInInheritedGameScene();

        void ProcedureAvailableInSpecifiedGameScene();

        string PropertyAvailableInInheritedGameScene { get; }

        string PropertyAvailableInSpecifiedGameScene { get; }
    }
}
