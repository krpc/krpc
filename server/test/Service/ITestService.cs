using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using KRPC.Service.Messages;

namespace KRPC.Test.Service
{
    public interface ITestService
    {
        void ProcedureNoArgsNoReturn ();

        void ProcedureSingleArgNoReturn (Response data);

        void ProcedureThreeArgsNoReturn (Response x, Request y, Response z);

        Response ProcedureNoArgsReturns ();

        Response ProcedureSingleArgReturns (Response data);

        int ProcedureWithValueTypes (float x, string y, byte[] z);

        string PropertyWithGetAndSet { get; set; }

        string PropertyWithGet { get; }

        [SuppressMessage ("Gendarme.Rules.Design", "AvoidPropertiesWithoutGetAccessorRule")]
        string PropertyWithSet { set; }

        TestService.TestClass CreateTestObject (string value);

        void DeleteTestObject (TestService.TestClass obj);

        TestService.TestClass EchoTestObject (TestService.TestClass obj);

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

        KRPC.Utils.Tuple<int,bool> EchoTuple (KRPC.Utils.Tuple<int,bool> t);

        [SuppressMessage ("Gendarme.Rules.Design.Generic", "DoNotExposeNestedGenericSignaturesRule")]
        IDictionary<int,IList<string>> EchoNestedCollection (IDictionary<int,IList<string>> c);

        IList<TestService.TestClass> EchoListOfObjects (IList<TestService.TestClass> l);

        KRPC.Utils.Tuple<int,bool> TupleDefault (KRPC.Utils.Tuple<int,bool> x);

        IList<int> ListDefault (IList<int> x);

        HashSet<int> SetDefault (HashSet<int> x);

        IDictionary<int,bool> DictionaryDefault (IDictionary<int,bool> x);
    }
}
