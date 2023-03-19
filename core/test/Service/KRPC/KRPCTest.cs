using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;

namespace KRPC.Test.Service.KRPC
{
    [TestFixture]
    [SuppressMessage ("Gendarme.Rules.Naming", "AvoidRedundancyInTypeNameRule")]
    public class KRPCTest
    {
        [Test]
        public void GetVersion ()
        {
            var status = global::KRPC.Service.KRPC.KRPC.GetStatus ();
            Assert.AreNotEqual (string.Empty, status.Version);
        }

        [Test]
        [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongMethodsRule")]
        public void GetServices ()
        {
            var services = global::KRPC.Service.KRPC.KRPC.GetServices ();
            Assert.IsNotNull (services);
            Assert.AreEqual (4, services.ServicesList.Count);

            var service = services.ServicesList.First (x => x.Name == "KRPC");
            Assert.AreEqual (67, service.Procedures.Count);
            Assert.AreEqual (2, service.Classes.Count);
            Assert.AreEqual (1, service.Enumerations.Count);

            int foundProcedures = 0;
            foreach (var proc in service.Procedures) {
                if (proc.Name == "GetClientID") {
                    MessageAssert.HasReturnType (proc, typeof(byte[]));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "GetClientName") {
                    MessageAssert.HasReturnType (proc, typeof(string));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "GetStatus") {
                    MessageAssert.HasReturnType (proc, typeof(global::KRPC.Service.Messages.Status));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "GetServices") {
                    MessageAssert.HasReturnType (proc, typeof(global::KRPC.Service.Messages.Services));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "AddStream") {
                    MessageAssert.HasReturnType (proc, typeof(global::KRPC.Service.Messages.Stream));
                    MessageAssert.HasParameters (proc, 2);
                    MessageAssert.HasParameter (proc, 0, typeof (global::KRPC.Service.Messages.ProcedureCall), "call");
                    MessageAssert.HasParameterWithDefaultValue (proc, 1, typeof (bool), "start", true);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "StartStream") {
                    MessageAssert.HasNoReturnType(proc);
                    MessageAssert.HasParameters(proc, 1);
                    MessageAssert.HasParameter(proc, 0, typeof(ulong), "id");
                    MessageAssert.HasDocumentation(proc);
                } else if (proc.Name == "SetStreamRate") {
                    MessageAssert.HasNoReturnType(proc);
                    MessageAssert.HasParameters(proc, 2);
                    MessageAssert.HasParameter(proc, 0, typeof(ulong), "id");
                    MessageAssert.HasParameter(proc, 1, typeof(float), "rate");
                    MessageAssert.HasDocumentation(proc);
                } else if (proc.Name == "RemoveStream") {
                    MessageAssert.HasNoReturnType (proc);
                    MessageAssert.HasParameters (proc, 1);
                    MessageAssert.HasParameter (proc, 0, typeof(ulong), "id");
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "get_Clients") {
                    MessageAssert.HasReturnType (proc, typeof(IList<Tuple<byte[],string,string>>));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else if (proc.Name == "get_CurrentGameScene") {
                    MessageAssert.HasReturnType (proc, typeof(global::KRPC.Service.KRPC.KRPC.GameScene));
                    MessageAssert.HasNoParameters (proc);
                    MessageAssert.HasDocumentation (proc);
                } else {
                    foundProcedures--;
                }
                foundProcedures++;
            }
            Assert.AreEqual (10, foundProcedures);

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

            int foundExceptions = 0;
            foreach (var exception in service.Exceptions) {
                if (exception.Name == "InvalidOperationException") {
                    MessageAssert.HasDocumentation (exception,
                        "<doc>\n<summary>\nA method call was made to a method that is invalid\ngiven the current state of the object.\n</summary>\n</doc>");
                } else if (exception.Name == "ArgumentException") {
                    MessageAssert.HasDocumentation (exception,
                        "<doc>\n<summary>\nA method was invoked where at least one of the passed arguments does not\nmeet the parameter specification of the method.\n</summary>\n</doc>");
                } else if (exception.Name == "ArgumentNullException") {
                    MessageAssert.HasDocumentation (exception,
                        "<doc>\n<summary>\nA null reference was passed to a method that does not accept it as a valid argument.\n</summary>\n</doc>");
                } else if (exception.Name == "ArgumentOutOfRangeException") {
                    MessageAssert.HasDocumentation (exception,
                        "<doc>\n<summary>\nThe value of an argument is outside the allowable range of values as defined by the invoked method.\n</summary>\n</doc>");
                }
                foundExceptions++;
            }
            Assert.AreEqual (4, foundExceptions);
        }

        [Test]
        public void PauseUnpause ()
        {
            bool paused = false;
            global::KRPC.Service.CallContext.IsPaused = () => paused;
            global::KRPC.Service.CallContext.Pause = () => paused = true;
            global::KRPC.Service.CallContext.Unpause = () => paused = false;

            global::KRPC.Service.KRPC.KRPC.Paused = true;
            Assert.True (paused);
            Assert.True (global::KRPC.Service.KRPC.KRPC.Paused);

            global::KRPC.Service.KRPC.KRPC.Paused = false;
            Assert.False (paused);
            Assert.False (global::KRPC.Service.KRPC.KRPC.Paused);
        }
    }
}
