using System;
using System.Collections.Generic;
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
            Assert.AreNotEqual (String.Empty, status.Version);
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
            foreach (var proc in service.Procedures) {
                if (proc.Name == "GetStatus") {
                    MessageAssert.HasReturnType (proc, typeof(KRPC.Service.Messages.Status));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "GetServices") {
                    MessageAssert.HasReturnType (proc, typeof(KRPC.Service.Messages.Services));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "AddStream") {
                    MessageAssert.HasReturnType (proc, typeof(KRPC.Service.Messages.Stream));
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(KRPC.Service.Messages.ProcedureCall), "call");
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "RemoveStream") {
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(ulong), "id");
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "get_Clients") {
                    MessageAssert.HasReturnType (proc, typeof(IList<KRPC.Utils.Tuple<byte[],string,string>>));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "get_CurrentGameScene") {
                    MessageAssert.HasReturnType (proc, typeof(KRPC.Service.KRPC.GameScene));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
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
