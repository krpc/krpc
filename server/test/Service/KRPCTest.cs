using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    [TestFixture]
    public class KRPCTest
    {
        [Test]
        public void GetVersion ()
        {
            var status = KRPC.Service.KRPC.GetStatus ();
            Assert.AreNotEqual (string.Empty, status.Version);
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void GetServices ()
        {
            var services = KRPC.Service.KRPC.GetServices ();
            Assert.IsNotNull (services);
            Assert.AreEqual (4, services.ServicesList.Count);

            var service = services.ServicesList.First (x => x.Name == "KRPC");
            Assert.AreEqual (6, service.Procedures.Count);
            Assert.AreEqual (0, service.Classes.Count);
            Assert.AreEqual (1, service.Enumerations.Count);

            int foundProcedures = 0;
            foreach (var method in service.Procedures) {
                if (method.Name == "GetStatus") {
                    MessageAssert.HasReturnType (method, "KRPC.Status");
                    MessageAssert.HasNoParameters (method);
                    MessageAssert.HasNoAttributes (method);
                    MessageAssert.HasDocumentation (method);
                } else if (method.Name == "GetServices") {
                    MessageAssert.HasReturnType (method, "KRPC.Services");
                    MessageAssert.HasNoParameters (method);
                    MessageAssert.HasNoAttributes (method);
                    MessageAssert.HasDocumentation (method);
                } else if (method.Name == "AddStream") {
                    MessageAssert.HasReturnType (method, "uint32");
                    MessageAssert.HasParameters (method, 1);
                    MessageAssert.HasParameter (method, 0, "KRPC.Request", "request");
                    MessageAssert.HasNoAttributes (method);
                    MessageAssert.HasDocumentation (method);
                } else if (method.Name == "RemoveStream") {
                    MessageAssert.HasNoReturnType (method);
                    MessageAssert.HasParameters (method, 1);
                    MessageAssert.HasParameter (method, 0, "uint32", "id");
                    MessageAssert.HasNoAttributes (method);
                    MessageAssert.HasDocumentation (method);
                } else if (method.Name == "get_Clients") {
                    MessageAssert.HasReturnType (method, "KRPC.List");
                    MessageAssert.HasNoParameters (method);
                    MessageAssert.HasAttributes (method, 2);
                    MessageAssert.HasAttribute (method, 0, "Property.Get(Clients)");
                    MessageAssert.HasAttribute (method, 1, "ReturnType.List(Tuple(bytes,string,string))");
                    MessageAssert.HasDocumentation (method);
                } else if (method.Name == "get_CurrentGameScene") {
                    MessageAssert.HasReturnType (method, "int32");
                    MessageAssert.HasNoParameters (method);
                    MessageAssert.HasAttributes (method, 2);
                    MessageAssert.HasAttribute (method, 0, "Property.Get(CurrentGameScene)");
                    MessageAssert.HasAttribute (method, 1, "ReturnType.Enum(KRPC.GameScene)");
                    MessageAssert.HasDocumentation (method);
                } else {
                    Assert.Fail ();
                }
                foundProcedures++;
            }
            Assert.AreEqual (6, foundProcedures);

            bool foundEnumeration = false;
            foreach (var enumeration in service.Enumerations) {
                if (enumeration.Name == "GameScene") {
                    foundEnumeration = true;
                    MessageAssert.HasDocumentation (enumeration,
                        "<doc>\n<summary>\nThe game scene. See <see cref=\"M:KRPC.CurrentGameScene\" />.\n</summary>\n</doc>");
                    MessageAssert.HasValues (enumeration, 5);
                    MessageAssert.HasValue (enumeration, 0, "SpaceCenter", 0,
                        "<doc>\n<summary>\nThe game scene showing the Kerbal Space Center buildings.\n</summary>\n</doc>");
                    MessageAssert.HasValue (enumeration, 1, "Flight", 1,
                        "<doc>\n<summary>\nThe game scene showing a vessel in flight (or on the launchpad/runway).\n</summary>\n</doc>");
                    MessageAssert.HasValue (enumeration, 2, "TrackingStation", 2,
                        "<doc>\n<summary>\nThe tracking station.\n</summary>\n</doc>");
                    MessageAssert.HasValue (enumeration, 3, "EditorVAB", 3,
                        "<doc>\n<summary>\nThe Vehicle Assembly Building.\n</summary>\n</doc>");
                    MessageAssert.HasValue (enumeration, 4, "EditorSPH", 4,
                        "<doc>\n<summary>\nThe Space Plane Hangar.\n</summary>\n</doc>");
                }
            }
            Assert.IsTrue (foundEnumeration);
        }
    }
}
