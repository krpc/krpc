using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace KRPC.Utils
{
    static class DocumentationExtensions
    {
        static IDictionary<string, XElement> documentation = new Dictionary<string, XElement> ();

        /// <summary>
        /// Remove indentiation from a multi-line string, where the first line
        /// is not indented, and following lines are indented.
        /// </summary>
        static string Dedent (String content)
        {
            var lines = content.Split ('\n');
            if (lines.Length == 1)
                return lines [0].Trim ();
            var indent = int.MaxValue;
            for (int i = 1; i < lines.Length; i++) {
                var newindex = 0;
                for (var j = 0; j < lines [i].Length; j++) {
                    if (lines [i] [j] != ' ')
                        break;
                    newindex++;
                }
                indent = Math.Min (indent, newindex);
            }
            lines [0] = new String (' ', indent) + lines [0];
            var result = "";
            foreach (var line in lines)
                result += line.Substring (indent) + "\n";
            return result.Trim ();
        }

        internal static String GetDocumentation (this MemberInfo member)
        {
            var location = member.Module.Assembly.Location;
            var path = Path.GetDirectoryName (location) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension (location) + ".xml";
            if (!documentation.ContainsKey (path)) {
                if (!File.Exists (path)) {
                    Logger.WriteLine ("No documentation found for " + member.Module.Assembly.Location + " at " + path);
                    documentation [path] = null;
                } else {
                    Logger.WriteLine ("Loading documentation for " + member.Module.Assembly.Location + " from " + path);
                    var document = XDocument.Load (path);
                    XElement membersNode = null;
                    foreach (var node in document.Root.Descendants()) {
                        if (node.Name == "members") {
                            membersNode = node;
                            break;
                        }
                    }
                    if (membersNode == null)
                        Logger.WriteLine ("Failed to load documentation for " + member.Module.Assembly.Location + " from " + path);
                    documentation [path] = membersNode;
                }
            }

            {
                var membersNode = documentation [path];
                if (membersNode == null)
                    return "";
                var name = GetDocumentationName (member);
                foreach (var node in membersNode.Descendants()) {
                    foreach (var attribute in node.Attributes ()) {
                        if (attribute.Name == "name" && attribute.Value == name) {
                            var content = "";
                            foreach (var descnode in node.Elements()) {
                                content += Dedent (descnode.ToString ().Replace ("\r\n", "\n")) + "\n";
                            }
                            return "<doc>\n" + content.TrimEnd () + "\n</doc>";
                        }
                    }
                }
                return "";
            }
        }

        internal static string GetDocumentationName (MemberInfo member)
        {
            char prefix;
            string name = member is Type ? ((Type)member).FullName : (member.DeclaringType.FullName + "." + member.Name);
            name = name.Replace ('+', '.');

            switch (member.MemberType) {

            case MemberTypes.NestedType:
            case MemberTypes.TypeInfo:
                prefix = 'T';
                break;

            case MemberTypes.Method:
                prefix = 'M';
                var parameters = String.Join (",", ((MethodBase)member).GetParameters ().Select (x => ParameterName (x)).ToArray ());
                if (!String.IsNullOrEmpty (parameters))
                    name += "(" + parameters + ")";
                break;

            case MemberTypes.Property:
                prefix = 'P';
                break;

            default:
                throw new ArgumentException ("Unknown member type");
            }
            return prefix + ":" + name;
        }

        static string TypeName (Type type)
        {
            var name = type.FullName;
            name = name.Replace ('+', '.');
            if (!type.IsGenericType)
                return name;
            name = name.Remove (name.IndexOf ('`'));
            var typeArguments = "{" + String.Join (",", type.GetGenericArguments ().Select (x => TypeName (x)).ToArray ()) + "}";
            return name + typeArguments;
        }

        static string ParameterName (ParameterInfo parameter)
        {
            return TypeName (parameter.ParameterType);
        }
    }
}
