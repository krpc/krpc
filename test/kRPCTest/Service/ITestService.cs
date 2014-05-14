using System.Collections.Generic;

namespace KRPCTest.Service
{
    public interface ITestService
    {
        void ProcedureNoArgsNoReturn ();

        void ProcedureSingleArgNoReturn (KRPC.Schema.KRPC.Response data);

        void ProcedureThreeArgsNoReturn (KRPC.Schema.KRPC.Response x,
                                         KRPC.Schema.KRPC.Request y, KRPC.Schema.KRPC.Response z);

        KRPC.Schema.KRPC.Response ProcedureNoArgsReturns ();

        KRPC.Schema.KRPC.Response ProcedureSingleArgReturns (KRPC.Schema.KRPC.Response data);

        int ProcedureWithValueTypes (float x, string y, byte[] z);

        string PropertyWithGetAndSet { get; set; }

        string PropertyWithGet { get; }

        string PropertyWithSet { set; }

        TestService.TestClass CreateTestObject (string value);

        void DeleteTestObject (TestService.TestClass obj);

        TestService.TestClass EchoTestObject (TestService.TestClass obj);

        void ProcedureSingleOptionalArgNoReturn (string x);

        void ProcedureThreeOptionalArgsNoReturn (float x, string y, int z);

        void ProcedureEnumArg (KRPC.Schema.Test.TestEnum x);

        KRPC.Schema.Test.TestEnum ProcedureEnumReturn ();

        void ProcedureCSharpEnumArg (TestService.CSharpEnum x);

        TestService.CSharpEnum ProcedureCSharpEnumReturn ();

        void BlockingProcedureNoReturn (int n);

        int BlockingProcedureReturns (int n, int sum);

        IList<string> EchoList (IList<string> l);

        IDictionary<int,string> EchoDictionary (IDictionary<int,string> d);

        HashSet<int> EchoSet (HashSet<int> h);

        IDictionary<int,IList<string>> EchoNestedCollection (IDictionary<int,IList<string>> c);

        IList<TestService.TestClass> EchoListOfObjects (IList<TestService.TestClass> l);
    }
}
