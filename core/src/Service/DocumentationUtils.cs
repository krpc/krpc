using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using KRPC.Service.Attributes;
using KRPC.Utils;

namespace KRPC.Service
{
    static class DocumentationUtils
    {
        [SuppressMessage ("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule")]
        [SuppressMessage ("Gendarme.Rules.Performance", "AvoidRepetitiveCallsToPropertiesRule")]
        [SuppressMessage ("Gendarme.Rules.Portability", "NewLineLiteralRule")]
        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidSwitchStatementsRule")]
        public static string ResolveCrefs (string documentation)
        {
            if (documentation.Length == 0)
                return string.Empty;

            var output = new StringBuilder ();
            using (XmlReader reader = XmlReader.Create (new StringReader (documentation))) {
                var ws = new XmlWriterSettings ();
                ws.OmitXmlDeclaration = true;
                ws.NewLineChars = "\n";
                using (XmlWriter writer = XmlWriter.Create (output, ws)) {
                    while (reader.Read ()) {
                        switch (reader.NodeType) {
                        case XmlNodeType.Element:
                            writer.WriteStartElement (reader.Name);
                            bool shortTag = reader.IsEmptyElement;
                            for (int i = 0; i < reader.AttributeCount; i++) {
                                reader.MoveToAttribute (i);
                                var name = reader.Name;
                                var value = reader.Value;
                                if (name == "cref")
                                    value = ResolveCref (value);
                                writer.WriteStartAttribute (name);
                                writer.WriteValue (value);
                                writer.WriteEndAttribute ();
                            }
                            if (shortTag)
                                writer.WriteEndElement ();
                            break;
                        case XmlNodeType.Text:
                        case XmlNodeType.SignificantWhitespace:
                        case XmlNodeType.Whitespace:
                            writer.WriteString (reader.Value);
                            break;
                        case XmlNodeType.XmlDeclaration:
                        case XmlNodeType.ProcessingInstruction:
                            writer.WriteProcessingInstruction (reader.Name, reader.Value);
                            break;
                        case XmlNodeType.Comment:
                            writer.WriteComment (reader.Value);
                            break;
                        case XmlNodeType.EndElement:
                            writer.WriteFullEndElement ();
                            break;
                        default:
                            throw new InvalidOperationException ("Unhandled");
                        }
                    }
                }
            }
            return output.ToString ();
        }

        static string ResolveCref (string cref)
        {
            if (cref.Length <= 3 || cref [1] != ':')
                throw new DocumentationException ("Invalid cref '" + cref + "'");
            var code = cref [0];
            var reference = cref.Substring (2);
            if (code == 'T')
                return ResolveTypeCref (reference);
            if (code == 'M')
                return ResolveMethodCref (reference);
            if (code == 'P')
                return ResolvePropertyCref (reference);
            if (code == 'F')
                return ResolveFieldCref (reference);
            throw new DocumentationException ("Invalid code '" + code + "' in cref '" + cref + "'");
        }

        static string ResolveTypeCref (string reference)
        {
            var type = GetType (reference);
            var name = type.Name;
            if (Reflection.HasAttribute<KRPCServiceAttribute> (type)) {
                TypeUtils.ValidateKRPCService (type);
                return "T:" + TypeUtils.GetServiceName (type);
            } else if (Reflection.HasAttribute<KRPCClassAttribute> (type)) {
                TypeUtils.ValidateKRPCClass (type);
                return "T:" + TypeUtils.GetClassServiceName (type) + "." + name;
            } else if (Reflection.HasAttribute<KRPCEnumAttribute> (type)) {
                TypeUtils.ValidateKRPCEnum (type);
                return "T:" + TypeUtils.GetEnumServiceName (type) + "." + name;
            }
            throw new DocumentationException ("Type '" + name + "' is not a kRPC service, class or enumeration");
        }

        static string ResolveMethodCref (string reference)
        {
            reference = reference.Split (new char[]{'('}) [0];
            var type = GetType (GetTypeName (reference));
            var method = GetMethod (type, GetMemberName (reference));
            var name = type.Name + "." + method.Name;
            if (Reflection.HasAttribute<KRPCProcedureAttribute> (method)) {
                TypeUtils.ValidateKRPCProcedure (method);
                return "M:" + name;
            } else if (Reflection.HasAttribute<KRPCMethodAttribute> (method)) {
                TypeUtils.ValidateKRPCMethod (type, method);
                return "M:" + TypeUtils.GetClassServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC procedure or method");
        }

        static string ResolvePropertyCref (string reference)
        {
            var type = GetType (GetTypeName (reference));
            var property = GetProperty (type, GetMemberName (reference));
            var name = type.Name + "." + property.Name;
            if (Reflection.HasAttribute<KRPCServiceAttribute> (type) &&
                Reflection.HasAttribute<KRPCPropertyAttribute> (property)) {
                TypeUtils.ValidateKRPCProperty (property);
                return "M:" + name;
            } else if (Reflection.HasAttribute<KRPCClassAttribute> (type) &&
                       Reflection.HasAttribute<KRPCPropertyAttribute> (property)) {
                TypeUtils.ValidateKRPCClassProperty (type, property);
                return "M:" + TypeUtils.GetClassServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC property");
        }

        static string ResolveFieldCref (string reference)
        {
            var type = GetType (GetTypeName (reference));
            var field = GetField (type, GetMemberName (reference));
            var name = type.Name + "." + field.Name;
            if (Reflection.HasAttribute<KRPCEnumAttribute> (type)) {
                TypeUtils.ValidateKRPCEnum (type);
                return "M:" + TypeUtils.GetEnumServiceName (type) + "." + name;
            }
            throw new DocumentationException ("'" + name + "' is not a kRPC enumeration value");
        }

        static string GetTypeName (string reference)
        {
            var parts = reference.Split (new char[]{'.'}).ToList ();
            parts.RemoveAt (parts.Count - 1);
            return string.Join (".", parts.ToArray ());
        }

        static string GetMemberName (string reference)
        {
            var parts = reference.Split (new char[]{'.'}).ToList ();
            return parts [parts.Count - 1];
        }

        static Type GetType (string name)
        {
            var type = Reflection.GetType (name);
            if (type == null)
                throw new DocumentationException ("Type '" + name + "' not found");
            return type;
        }

        static MethodInfo GetMethod (Type type, string name)
        {
            var method = type.GetMethod (name);
            if (method == null)
                throw new DocumentationException ("Method '" + type.Name + "." + name + "' not found");
            return method;
        }

        static PropertyInfo GetProperty (Type type, string name)
        {
            var property = type.GetProperty (name);
            if (property == null)
                throw new DocumentationException ("Property '" + type.Name + "." + name + "' not found");
            return property;
        }

        static FieldInfo GetField (Type type, string name)
        {
            var field = type.GetField (name);
            if (field == null)
                throw new DocumentationException ("Field '" + type.Name + "." + name + "' not found");
            return field;
        }
    }
}
