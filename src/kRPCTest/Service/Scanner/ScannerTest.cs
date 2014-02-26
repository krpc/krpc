using NUnit.Framework;
using System;

namespace KRPCTest.Service.Scanner
{
    [TestFixture ()]
    public class ScannerTest
    {
        /// <summary>
        /// Check the output of the service scanner
        /// </summary>
        [Test]
        public void GetServices ()
        {
            var services = KRPC.Service.KRPC.GetServices ();
            Assert.IsNotNull (services);
            Assert.AreEqual (2, services.Services_Count);
            foreach (KRPC.Schema.KRPC.Service service in services.Services_List) {
                if (service.Name == "TestService") {
                    Assert.AreEqual (18, service.ProceduresCount);
                    int found = 0;
                    foreach (var method in service.ProceduresList) {
                        if (method.Name == "ProcedureNoArgsNoReturn") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureSingleArgNoReturn") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureThreeArgsNoReturn") {
                            Assert.AreEqual (3, method.ParameterTypesCount);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.AreEqual ("KRPC.Request", method.ParameterTypesList [1]);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [2]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureNoArgsReturns") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureSingleArgReturns") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("KRPC.Response", method.ParameterTypesList [0]);
                            Assert.AreEqual ("KRPC.Response", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "ProcedureWithValueTypes") {
                            Assert.AreEqual (3, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("float", method.ParameterTypesList [0]);
                            Assert.AreEqual ("string", method.ParameterTypesList [1]);
                            Assert.AreEqual ("bytes", method.ParameterTypesList [2]);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (0, method.AttributesCount);
                            found++;
                        }
                        if (method.Name == "get_PropertyWithGetAndSet") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Get(PropertyWithGetAndSet)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "set_PropertyWithGetAndSet") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("string", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Set(PropertyWithGetAndSet)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "get_PropertyWithGet") {
                            Assert.AreEqual (0, method.ParameterTypesCount);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Get(PropertyWithGet)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "set_PropertyWithSet") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("string", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Property.Set(PropertyWithSet)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "CreateTestObject") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("string", method.ParameterTypesList [0]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint64", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ReturnType.Class(TestClass)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "DeleteTestObject") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("ParameterType(0).Class(TestClass)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "TestClass_FloatToString") {
                            Assert.AreEqual (2, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.AreEqual ("float", method.ParameterTypesList [1]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Method(TestClass,FloatToString)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestClass)", method.AttributesList [1]);
                            found++;
                        }
                        if (method.Name == "TestClass_ObjectToString") {
                            Assert.AreEqual (2, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [1]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("string", method.ReturnType);
                            Assert.AreEqual (3, method.AttributesCount);
                            Assert.AreEqual ("Class.Method(TestClass,ObjectToString)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(0).Class(TestClass)", method.AttributesList [1]);
                            Assert.AreEqual ("ParameterType(1).Class(TestClass)", method.AttributesList [2]);
                            found++;
                        }
                        if (method.Name == "TestClass_get_IntProperty") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("int32", method.ReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Get(TestClass,IntProperty)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "TestClass_set_IntProperty") {
                            Assert.AreEqual (2, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.AreEqual ("int32", method.ParameterTypesList [1]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (1, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Set(TestClass,IntProperty)", method.AttributesList [0]);
                            found++;
                        }
                        if (method.Name == "TestClass_get_ObjectProperty") {
                            Assert.AreEqual (1, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.IsTrue (method.HasReturnType);
                            Assert.AreEqual ("uint64", method.ReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Get(TestClass,ObjectProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ReturnType.Class(TestClass)", method.AttributesList [1]);
                            found++;
                        }
                        if (method.Name == "TestClass_set_ObjectProperty") {
                            Assert.AreEqual (2, method.ParameterTypesCount);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [0]);
                            Assert.AreEqual ("uint64", method.ParameterTypesList [1]);
                            Assert.IsFalse (method.HasReturnType);
                            Assert.AreEqual (2, method.AttributesCount);
                            Assert.AreEqual ("Class.Property.Set(TestClass,ObjectProperty)", method.AttributesList [0]);
                            Assert.AreEqual ("ParameterType(1).Class(TestClass)", method.AttributesList [1]);
                            found++;
                        }
                    }
                    Assert.AreEqual (18, found);
                }
            }
        }
    }
}

