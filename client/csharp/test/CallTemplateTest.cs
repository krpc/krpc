using System;
using KRPC.Client.Services.TestService;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class CallTemplateTest
    {
        static void AssertTemplate (string procedure, Schema.KRPC.ProcedureCall call)
        {
            Assert.AreEqual ("TestService", call.Service);
            Assert.AreEqual (procedure, call.Procedure);
            Assert.IsEmpty (call.Arguments);
        }

        [Test]
        public void ClassMembers ()
        {
            AssertTemplate ("TestClass_get_IntProperty",
                Connection.GetCallTemplate ((TestClass x) => x.IntProperty));
            AssertTemplate ("TestClass_FloatToString",
                Connection.GetCallTemplate ((TestClass x) => x.FloatToString (default)));
            AssertTemplate ("TestClass_set_IntProperty",
                Connection.GetSetCallTemplate<TestClass> (nameof (TestClass.IntProperty)));
            AssertTemplate ("TestClass_set_StringPropertyPrivateGet",
                Connection.GetSetCallTemplate<TestClass> (nameof (TestClass.StringPropertyPrivateGet)));
        }

        [Test]
        public void ServiceMembersAndStaticMethods ()
        {
            Connection connection = null;
            AssertTemplate ("Int32ToString",
                Connection.GetCallTemplate (() => connection.TestService ().Int32ToString (default)));
            AssertTemplate ("get_StringProperty",
                Connection.GetCallTemplate (() => connection.TestService ().StringProperty));
            AssertTemplate ("set_StringProperty",
                Connection.GetSetCallTemplate<Service> (nameof (Service.StringProperty)));
            AssertTemplate ("TestClass_static_StaticMethod",
                Connection.GetCallTemplate (() => TestClass.StaticMethod (connection, default, default)));
        }

        [Test]
        public void MemberOfAnotherObject ()
        {
            TestClass other = null;
            Assert.Throws<ArgumentException> (
                () => Connection.GetCallTemplate ((TestClass x) => other.IntProperty));
            Assert.Throws<ArgumentException> (
                () => Connection.GetCallTemplate (() => other.IntProperty));
        }

        [Test]
        public void NotAProcedure ()
        {
            Assert.Throws<ArgumentException> (
                () => Connection.GetCallTemplate ((TestClass x) => x.IntProperty + 1));
            Assert.Throws<ArgumentException> (
                () => Connection.GetCallTemplate ((TestClass x) => x.ToString ()));
            Assert.Throws<ArgumentException> (
                () => Connection.GetSetCallTemplate<TestClass> (nameof (TestClass.StringPropertyPrivateSet)));
        }
    }
}
