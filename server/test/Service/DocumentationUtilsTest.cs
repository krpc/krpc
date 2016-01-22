using NUnit.Framework;
using KRPC.Service;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class DocumentationUtilsTest
    {
        [Test]
        public void ResolveCrefs ()
        {
            Assert.AreEqual (
                "<see cref=\"T:TestService\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"T:KRPC.Test.Service.TestService\" />"));
            Assert.AreEqual (
                "<see cref=\"M:TestService.ProcedureNoArgsNoReturn\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"M:KRPC.Test.Service.TestService.ProcedureNoArgsNoReturn\" />"));
            Assert.AreEqual (
                "<see cref=\"M:TestService.PropertyWithGetAndSet\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"P:KRPC.Test.Service.TestService.PropertyWithGetAndSet\" />"));
            Assert.AreEqual (
                "<see cref=\"T:TestService.CSharpEnum\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"T:KRPC.Test.Service.TestService.CSharpEnum\" />"));
            Assert.AreEqual (
                "<see cref=\"M:TestService.CSharpEnum.x\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"F:KRPC.Test.Service.TestService.CSharpEnum.x\" />"));
            Assert.AreEqual (
                "<see cref=\"T:TestService.TestClass\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"T:KRPC.Test.Service.TestService+TestClass\" />"));
            Assert.AreEqual (
                "<see cref=\"M:TestService.TestClass.FloatToString\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"M:KRPC.Test.Service.TestService.TestClass.FloatToString(System.Single)\" />"));
            Assert.AreEqual (
                "<see cref=\"M:TestService.TestClass.IntProperty\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"P:KRPC.Test.Service.TestService.TestClass.IntProperty\" />"));
        }

        [Test]
        public void ResolveIncorrectCrefs ()
        {
            Assert.Throws<ServiceException> (() => DocumentationUtils.ResolveCrefs ("<see cref=\"\" />"));
            Assert.Throws<ServiceException> (() => DocumentationUtils.ResolveCrefs ("<see cref=\"Foo\" />"));
            Assert.Throws<ServiceException> (() => DocumentationUtils.ResolveCrefs ("<see cref=\"Foo.Bar\" />"));
            Assert.Throws<ServiceException> (() => DocumentationUtils.ResolveCrefs ("<see cref=\"T:Foo.Bar\" />"));
            Assert.Throws<ServiceException> (() => DocumentationUtils.ResolveCrefs ("<see cref=\"M:Foo.Bar\" />"));
            Assert.Throws<ServiceException> (() => DocumentationUtils.ResolveCrefs ("<see cref=\"P:Foo.Bar\" />"));
            Assert.Throws<ServiceException> (() => DocumentationUtils.ResolveCrefs ("<see cref=\"F:Foo.Bar\" />"));
        }
    }
}