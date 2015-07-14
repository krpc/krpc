using NUnit.Framework;
using KRPC.Service;

namespace KRPCTest.Service
{
    [TestFixture]
    public class DocumentationUtilsTest
    {
        [Test]
        public void ResolveCrefs ()
        {
            Assert.AreEqual ("<see cref=\"\" />", DocumentationUtils.ResolveCrefs ("<see cref=\"\" />"));
            Assert.AreEqual ("<see cref=\"Foo\" />", DocumentationUtils.ResolveCrefs ("<see cref=\"Foo\" />"));
            Assert.AreEqual ("<see cref=\"Foo.Bar\" />", DocumentationUtils.ResolveCrefs ("<see cref=\"Foo.Bar\" />"));
            Assert.AreEqual ("<see cref=\"T:Foo.Bar\" />", DocumentationUtils.ResolveCrefs ("<see cref=\"T:Foo.Bar\" />"));
            Assert.AreEqual ("<see cref=\"M:Foo.Bar\" />", DocumentationUtils.ResolveCrefs ("<see cref=\"M:Foo.Bar\" />"));
            Assert.AreEqual ("<see cref=\"P:Foo.Bar\" />", DocumentationUtils.ResolveCrefs ("<see cref=\"P:Foo.Bar\" />"));
            Assert.AreEqual ("<see cref=\"F:Foo.Bar\" />", DocumentationUtils.ResolveCrefs ("<see cref=\"F:Foo.Bar\" />"));
            Assert.AreEqual (
                "<see cref=\"TestService\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"T:KRPCTest.Service.TestService\" />"));
            Assert.AreEqual (
                "<see cref=\"TestService.ProcedureNoArgsNoReturn\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"M:KRPCTest.Service.TestService.ProcedureNoArgsNoReturn\" />"));
            Assert.AreEqual (
                "<see cref=\"TestService.PropertyWithGetAndSet\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"P:KRPCTest.Service.TestService.PropertyWithGetAndSet\" />"));
            Assert.AreEqual (
                "<see cref=\"TestService.CSharpEnum\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"T:KRPCTest.Service.TestService.CSharpEnum\" />"));
            Assert.AreEqual (
                "<see cref=\"TestService.CSharpEnum.x\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"F:KRPCTest.Service.TestService.CSharpEnum.x\" />"));
            Assert.AreEqual (
                "<see cref=\"TestService.TestClass\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"T:KRPCTest.Service.TestService+TestClass\" />"));
            Assert.AreEqual (
                "<see cref=\"TestService.TestClass.FloatToString\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"M:KRPCTest.Service.TestService.TestClass.FloatToString(System.Single)\" />"));
            Assert.AreEqual (
                "<see cref=\"TestService.TestClass.IntProperty\" />",
                DocumentationUtils.ResolveCrefs ("<see cref=\"P:KRPCTest.Service.TestService.TestClass.IntProperty\" />"));
        }
    }
}