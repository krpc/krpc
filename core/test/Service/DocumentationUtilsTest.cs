using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class DocumentationUtilsTest
    {
        [TestCase ("<see cref=\"T:KRPC.Test.Service.TestService\" />", "<see cref=\"T:TestService\" />")]
        [TestCase ("<see cref=\"M:KRPC.Test.Service.TestService.ProcedureNoArgsNoReturn\" />", "<see cref=\"M:TestService.ProcedureNoArgsNoReturn\" />")]
        [TestCase ("<see cref=\"P:KRPC.Test.Service.TestService.PropertyWithGetAndSet\" />", "<see cref=\"M:TestService.PropertyWithGetAndSet\" />")]
        [TestCase ("<see cref=\"T:KRPC.Test.Service.TestService.TestEnum\" />", "<see cref=\"T:TestService.TestEnum\" />")]
        [TestCase ("<see cref=\"F:KRPC.Test.Service.TestService.TestEnum.X\" />", "<see cref=\"M:TestService.TestEnum.X\" />")]
        [TestCase ("<see cref=\"T:KRPC.Test.Service.TestService+TestClass\" />", "<see cref=\"T:TestService.TestClass\" />")]
        [TestCase ("<see cref=\"M:KRPC.Test.Service.TestService.TestClass.FloatToString(System.Single)\" />", "<see cref=\"M:TestService.TestClass.FloatToString\" />")]
        [TestCase ("<see cref=\"P:KRPC.Test.Service.TestService.TestClass.IntProperty\" />", "<see cref=\"M:TestService.TestClass.IntProperty\" />")]
        [TestCase ("<doc>\n<summary>\nThe game scene. See <see cref=\"P:KRPC.Service.KRPC.KRPC.GameScene\" />.\n</summary>\n</doc>",
            "<doc>\n<summary>\nThe game scene. See <see cref=\"M:KRPC.GameScene\" />.\n</summary>\n</doc>")]
        public void ResolveCrefs (string input, string output)
        {
            Assert.AreEqual (output, DocumentationUtils.ResolveCrefs (input));
        }

        [TestCase ("<see cref=\"\" />")]
        [TestCase ("<see cref=\"Foo\" />")]
        [TestCase ("<see cref=\"Foo.Bar\" />")]
        [TestCase ("<see cref=\"T:Foo.Bar\" />")]
        [TestCase ("<see cref=\"M:Foo.Bar\" />")]
        [TestCase ("<see cref=\"P:Foo.Bar\" />")]
        [TestCase ("<see cref=\"F:Foo.Bar\" />")]
        public void ResolveIncorrectCrefs (string input)
        {
            Assert.Throws<DocumentationException> (() => DocumentationUtils.ResolveCrefs (input));
        }

        [TestCase ("", "")]
        [TestCase ("Use ProcedureNoArgsNoReturn instead.", "Use ProcedureNoArgsNoReturn instead.")]
        [TestCase ("Use <see cref='ProcedureNoArgsNoReturn'/> instead.",
            "Use <see cref=\"M:TestService.ProcedureNoArgsNoReturn\" /> instead.")]
        [TestCase ("Use <see cref='PropertyWithGetAndSet'/> instead.",
            "Use <see cref=\"M:TestService.PropertyWithGetAndSet\" /> instead.")]
        [TestCase ("Use <see cref='TestClass'/> instead.",
            "Use <see cref=\"T:TestService.TestClass\" /> instead.")]
        [TestCase ("Use <see cref='TestClass.FloatToString'/> instead.",
            "Use <see cref=\"M:TestService.TestClass.FloatToString\" /> instead.")]
        [TestCase ("Use <see cref='TestClass.IntProperty'/> instead.",
            "Use <see cref=\"M:TestService.TestClass.IntProperty\" /> instead.")]
        [TestCase ("Use <see cref='TestEnum'/> instead.",
            "Use <see cref=\"T:TestService.TestEnum\" /> instead.")]
        [TestCase ("Use <see cref='TestEnum.X'/> instead.",
            "Use <see cref=\"M:TestService.TestEnum.X\" /> instead.")]
        [TestCase ("See <see cref='M:KRPC.Test.Service.TestService.ProcedureNoArgsNoReturn'/> and <see cref='TestClass'/>.",
            "See <see cref=\"M:TestService.ProcedureNoArgsNoReturn\" /> and <see cref=\"T:TestService.TestClass\" />.")]
        public void ResolveDeprecationReason (string input, string output)
        {
            Assert.AreEqual (output, DocumentationUtils.ResolveDeprecationReason (input, typeof (TestService)));
        }

        [Test]
        public void ResolveDeprecationReasonInClassContext ()
        {
            Assert.AreEqual (
                "Use <see cref=\"M:TestService.TestClass.FloatToString\" /> instead.",
                DocumentationUtils.ResolveDeprecationReason (
                    "Use <see cref='FloatToString'/> instead.", typeof (TestService.TestClass)));
        }

        [Test]
        public void ResolveDeprecationReasonInEnumContext ()
        {
            Assert.AreEqual (
                "Use <see cref=\"M:TestService.TestEnum.X\" /> instead.",
                DocumentationUtils.ResolveDeprecationReason (
                    "Use <see cref='X'/> instead.", typeof (TestService.TestEnum)));
        }

        [TestCase ("Use <see cref='NonExistent'/> instead.")]
        [TestCase ("Use <see cref='NonExistent.Member'/> instead.")]
        [TestCase ("Use <see cref=''/> instead.")]
        [TestCase ("Not < valid XML.")]
        public void ResolveIncorrectDeprecationReason (string input)
        {
            Assert.Throws<DocumentationException> (
                () => DocumentationUtils.ResolveDeprecationReason (input, typeof (TestService)));
        }
    }
}
