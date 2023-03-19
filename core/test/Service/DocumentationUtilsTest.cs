using System.Diagnostics.CodeAnalysis;
using KRPC.Service;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSpeculativeGeneralityRule")]
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
        [TestCase ("<doc>\n<summary>\nThe game scene. See <see cref=\"P:KRPC.Service.KRPC.KRPC.CurrentGameScene\" />.\n</summary>\n</doc>",
            "<doc>\n<summary>\nThe game scene. See <see cref=\"M:KRPC.CurrentGameScene\" />.\n</summary>\n</doc>")]
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
    }
}
