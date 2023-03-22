using System;
using KRPC.Service;
using KRPC.Service.Messages;
using NUnit.Framework;

namespace KRPC.Test.Service
{
    static class MessageAssert
    {
        public static void HasNoParameters (Procedure procedure)
        {
            Assert.AreEqual (0, procedure.Parameters.Count);
        }

        public static void HasParameters (Procedure procedure, int count)
        {
            Assert.AreEqual (count, procedure.Parameters.Count);
        }

        public static void HasParameter (Procedure procedure, int position, Type type, string name)
        {
            Assert.Less (position, procedure.Parameters.Count);
            var parameter = procedure.Parameters [position];
            Assert.AreEqual (type, parameter.Type);
            Assert.AreEqual (name, parameter.Name);
            Assert.IsFalse (parameter.HasDefaultValue);
            Assert.IsNull (parameter.DefaultValue);
        }

        public static void HasParameterWithDefaultValue (Procedure procedure, int position, Type type, string name, object defaultValue)
        {
            Assert.Less (position, procedure.Parameters.Count);
            var parameter = procedure.Parameters [position];
            Assert.AreEqual (type, parameter.Type);
            Assert.AreEqual (name, parameter.Name);
            Assert.IsTrue (parameter.HasDefaultValue);
            Assert.AreEqual (defaultValue, parameter.DefaultValue);
        }

        public static void HasNoReturnType (Procedure procedure)
        {
            Assert.IsFalse (procedure.HasReturnType);
            Assert.IsNull (procedure.ReturnType);
        }

        public static void HasReturnType (Procedure procedure, Type returnType, bool returnIsNullable = false)
        {
            Assert.IsTrue (procedure.HasReturnType);
            Assert.AreEqual (returnType, procedure.ReturnType);
            Assert.AreEqual (returnIsNullable, procedure.ReturnIsNullable);
        }

        public static void HasGameScene (Procedure procedure, GameScene gameScene)
        {
            Assert.AreEqual (gameScene, procedure.GameScene);
        }

        public static void HasNoDocumentation (Procedure procedure)
        {
            Assert.AreEqual (string.Empty, procedure.Documentation);
        }

        public static void HasDocumentation (Procedure procedure)
        {
            Assert.AreNotEqual (string.Empty, procedure.Documentation);
        }

        public static void HasDocumentation (Procedure procedure, string documentation)
        {
            Assert.AreEqual (documentation, procedure.Documentation);
        }

        public static void HasNoDocumentation (Class cls)
        {
            Assert.AreEqual (string.Empty, cls.Documentation);
        }

        public static void HasDocumentation (Class cls, string documentation)
        {
            Assert.AreEqual (documentation, cls.Documentation);
        }

        public static void HasValues (Enumeration enumeration, int count)
        {
            Assert.AreEqual (count, enumeration.Values.Count);
        }

        public static void HasValue (Enumeration enumeration, int position, string name, int value)
        {
            HasValue (enumeration, position, name, value, string.Empty);
        }

        public static void HasValue (Enumeration enumeration, int position, string name, int value, string documentation)
        {
            Assert.Less (position, enumeration.Values.Count);
            var enumerationValue = enumeration.Values [position];
            Assert.AreEqual (name, enumerationValue.Name);
            Assert.AreEqual (value, enumerationValue.Value);
            Assert.AreEqual (documentation, enumerationValue.Documentation);
        }

        public static void HasDocumentation (Enumeration enumeration, string documentation)
        {
            Assert.AreEqual (documentation, enumeration.Documentation);
        }

        public static void HasDocumentation (global::KRPC.Service.Messages.Exception exception, string documentation)
        {
            Assert.AreEqual (documentation, exception.Documentation);
        }
    }
}
