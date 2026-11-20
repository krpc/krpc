using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using KRPC.Utils;
using TypeCode = KRPC.Schema.KRPC.Type.Types.TypeCode;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// The OpenAPI 3.0 document of the HTTP server, built from the service definitions and
    /// the routes. Version 3.0 has the widest support among the tools that read a document.
    /// </summary>
    static class OpenApi
    {
        const string ErrorSchema = "#/components/schemas/Error";

        /// <summary>
        /// The server URL in the document, as JSON, which a response replaces with the URL the
        /// request reached the server at. A tool given the document as a file sends its calls
        /// to that URL.
        /// </summary>
        public const string ServerUrl = "\"%SERVER%\"";

        public static string Document (Schema.KRPC.Services services, IEnumerable<Route> routes, string version)
        {
            var paths = new List<KeyValuePair<string, object>> ();
            foreach (var group in routes.GroupBy (x => x.Path)) {
                var operations = group.OrderBy (x => x.Method, System.StringComparer.Ordinal)
                    .Select (x => new KeyValuePair<string, object> (x.Method.ToLowerInvariant (), Operation (x)));
                paths.Add (new KeyValuePair<string, object> (group.Key, operations.ToList ()));
            }
            var schemas = Routes.Entries ("Error", ErrorSchemaObject ());
            foreach (var service in services.Services_) {
                foreach (var cls in service.Classes.Where (x => !Codec.Excluded (service.Name, x.Name)))
                    schemas.Add (new KeyValuePair<string, object> (service.Name + "." + cls.Name, ObjectSchema (cls.Documentation)));
                foreach (var enumeration in service.Enumerations)
                    schemas.Add (new KeyValuePair<string, object> (service.Name + "." + enumeration.Name, EnumerationSchema (enumeration)));
                foreach (var structure in service.Structs)
                    schemas.Add (new KeyValuePair<string, object> (service.Name + "." + structure.Name, StructureSchema (structure)));
            }
            return Json.Write (Routes.Entries (
                "openapi", "3.0.3",
                "info", Routes.Entries (
                    "title", "kRPC",
                    "version", version ?? string.Empty,
                    "description", "Calls the procedures of a kRPC server over HTTP, with JSON bodies."),
                "servers", new List<object> { Routes.Entries ("url", "%SERVER%") },
                "tags", services.Services_.Select (x => (object)Routes.Entries (
                    "name", x.Name, "description", Text (x.Documentation, "summary"))).ToList (),
                "paths", paths,
                "components", Routes.Entries ("schemas", schemas)));
        }

        static List<KeyValuePair<string, object>> Operation (Route route)
        {
            var procedure = route.Procedure;
            var operation = Routes.Entries (
                "operationId", route.Service + "_" + procedure.Name,
                "tags", new List<object> { route.Service });
            var summary = Text (procedure.Documentation, "summary");
            if (summary.Length > 0)
                operation.Add (Entry ("summary", summary));
            var remarks = Text (procedure.Documentation, "remarks");
            if (remarks.Length > 0)
                operation.Add (Entry ("description", remarks));
            if (route.Instance) {
                operation.Add (Entry ("parameters", new List<object> {
                    Routes.Entries (
                        "name", "id",
                        "in", "path",
                        "required", true,
                        "description", "The identifier of the object.",
                        "schema", TypeSchema (procedure.Parameters [0].Type))
                }));
            }
            if (route.Parameters.Count > 0) {
                var properties = new List<KeyValuePair<string, object>> ();
                var required = new List<object> ();
                foreach (var entry in route.Parameters.OrderBy (x => x.Value)) {
                    var parameter = procedure.Parameters [entry.Value];
                    var schema = TypeSchema (parameter.Type);
                    var description = route.Setter ? "The new value." : Text (procedure.Documentation, "param", parameter.Name);
                    if (description.Length > 0)
                        schema = Describe (schema, description);
                    properties.Add (Entry (entry.Key, schema));
                    if (!parameter.HasDefaultValue)
                        required.Add (entry.Key);
                }
                var body = Routes.Entries ("type", "object", "properties", properties, "additionalProperties", false);
                if (required.Count > 0)
                    body.Add (Entry ("required", required));
                operation.Add (Entry ("requestBody", Routes.Entries (
                    "required", required.Count > 0,
                    "content", Content (body))));
            }
            var result = Routes.Entries ("type", "object");
            var returnType = procedure.ReturnType;
            if (returnType != null && returnType.Code != TypeCode.None) {
                var value = TypeSchema (returnType);
                var returns = Text (procedure.Documentation, "returns");
                if (returns.Length > 0)
                    value = Describe (value, returns);
                result.Add (Entry ("properties", Routes.Entries ("value", value)));
                result.Add (Entry ("required", new List<object> { "value" }));
            }
            var error = Routes.Entries ("description", "The call failed.", "content", Content (Routes.Entries ("$ref", ErrorSchema)));
            operation.Add (Entry ("responses", Routes.Entries (
                "200", Routes.Entries ("description", "The call succeeded.", "content", Content (result)),
                "default", error)));
            if (procedure.Deprecated)
                operation.Add (Entry ("deprecated", true));
            return operation;
        }

        static List<KeyValuePair<string, object>> Content (object schema)
        {
            return Routes.Entries ("application/json", Routes.Entries ("schema", schema));
        }

        static KeyValuePair<string, object> Entry (string key, object value)
        {
            return new KeyValuePair<string, object> (key, value);
        }

        // A reference takes no sibling keywords in OpenAPI 3.0, so it is described through allOf
        static List<KeyValuePair<string, object>> Describe (List<KeyValuePair<string, object>> schema, string description)
        {
            if (schema.Any (x => x.Key == "$ref"))
                schema = Routes.Entries ("allOf", new List<object> { schema });
            schema.Add (Entry ("description", description));
            return schema;
        }

        static List<KeyValuePair<string, object>> ErrorSchemaObject ()
        {
            var text = Routes.Entries ("type", "string");
            return Routes.Entries (
                "type", "object",
                "properties", Routes.Entries ("error", Routes.Entries (
                    "type", "object",
                    "properties", Routes.Entries (
                        "service", text,
                        "name", text,
                        "description", text,
                        "stack_trace", text),
                    "required", new List<object> { "service", "name", "description", "stack_trace" })),
                "required", new List<object> { "error" });
        }

        static List<KeyValuePair<string, object>> ObjectSchema (string documentation)
        {
            var schema = Routes.Entries ("type", "integer", "format", "uint64", "minimum", 0);
            var summary = Text (documentation, "summary");
            return Describe (schema, (summary + " Passed as the object's identifier.").Trim ());
        }

        static List<KeyValuePair<string, object>> EnumerationSchema (Schema.KRPC.Enumeration enumeration)
        {
            var names = Routes.Entries ("type", "string", "enum",
                enumeration.Values.Select (x => (object)Naming.SnakeCase (x.Name)).ToList ());
            var schema = enumeration.Flags ? Routes.Entries ("type", "array", "items", names, "uniqueItems", true) : names;
            var summary = Text (enumeration.Documentation, "summary");
            return summary.Length > 0 ? Describe (schema, summary) : schema;
        }

        static List<KeyValuePair<string, object>> StructureSchema (Schema.KRPC.Struct structure)
        {
            var properties = structure.Fields.Select (x => {
                var schema = TypeSchema (x.Type);
                var summary = Text (x.Documentation, "summary");
                return Entry (Naming.SnakeCase (x.Name), summary.Length > 0 ? Describe (schema, summary) : schema);
            }).ToList ();
            var result = Routes.Entries (
                "type", "object",
                "properties", properties,
                "required", structure.Fields.Select (x => (object)Naming.SnakeCase (x.Name)).ToList (),
                "additionalProperties", false);
            var description = Text (structure.Documentation, "summary");
            return description.Length > 0 ? Describe (result, description) : result;
        }

        /// <summary>
        /// The schema of a value of the type, as the HTTP server carries it in JSON.
        /// </summary>
        static List<KeyValuePair<string, object>> TypeSchema (Schema.KRPC.Type type)
        {
            List<KeyValuePair<string, object>> schema;
            switch (type.Code) {
            case TypeCode.Double:
                schema = Routes.Entries ("type", "number", "format", "double");
                break;
            case TypeCode.Float:
                schema = Routes.Entries ("type", "number", "format", "float");
                break;
            case TypeCode.Sint32:
                schema = Routes.Entries ("type", "integer", "format", "int32");
                break;
            case TypeCode.Sint64:
                schema = Routes.Entries ("type", "integer", "format", "int64");
                break;
            case TypeCode.Uint32:
                schema = Routes.Entries ("type", "integer", "format", "uint32", "minimum", 0, "maximum", uint.MaxValue);
                break;
            case TypeCode.Uint64:
                schema = Routes.Entries ("type", "integer", "format", "uint64", "minimum", 0);
                break;
            case TypeCode.Bool:
                schema = Routes.Entries ("type", "boolean");
                break;
            case TypeCode.String:
                schema = Routes.Entries ("type", "string");
                break;
            case TypeCode.Bytes:
                schema = Routes.Entries ("type", "string", "format", "byte");
                break;
            case TypeCode.Class:
            case TypeCode.Enumeration:
            case TypeCode.Struct:
                schema = Routes.Entries ("$ref", "#/components/schemas/" + type.Service + "." + type.Name);
                break;
            case TypeCode.List:
                schema = Routes.Entries ("type", "array", "items", TypeSchema (type.Types_ [0]));
                break;
            case TypeCode.Set:
                schema = Routes.Entries ("type", "array", "items", TypeSchema (type.Types_ [0]), "uniqueItems", true);
                break;
            case TypeCode.Tuple:
                schema = Routes.Entries (
                    "type", "array",
                    "items", OneOf (type.Types_),
                    "minItems", type.Types_.Count,
                    "maxItems", type.Types_.Count);
                break;
            case TypeCode.Dictionary:
                if (type.Types_ [0].Code == TypeCode.String)
                    schema = Routes.Entries ("type", "object", "additionalProperties", TypeSchema (type.Types_ [1]));
                else
                    schema = Routes.Entries (
                        "type", "array",
                        "items", Routes.Entries (
                            "type", "array",
                            "items", OneOf (type.Types_),
                            "minItems", 2,
                            "maxItems", 2));
                break;
            default:
                schema = Routes.Entries ();
                break;
            }
            if (type.Nullable) {
                if (schema.Any (x => x.Key == "$ref"))
                    schema = Routes.Entries ("allOf", new List<object> { schema });
                schema.Add (Entry ("nullable", true));
            }
            return schema;
        }

        // OpenAPI 3.0 has one schema for the items of an array, so the items of a tuple may be
        // any of its types
        static List<KeyValuePair<string, object>> OneOf (IEnumerable<Schema.KRPC.Type> types)
        {
            var schemas = new List<List<KeyValuePair<string, object>>> ();
            var seen = new HashSet<string> ();
            foreach (var type in types) {
                var schema = TypeSchema (type);
                if (seen.Add (Json.Write (schema)))
                    schemas.Add (schema);
            }
            if (schemas.Count == 1)
                return schemas [0];
            return Routes.Entries ("oneOf", schemas.Cast<object> ().ToList ());
        }

        /// <summary>
        /// The text of an element of the XML documentation, with its markup reduced to plain
        /// text: a reference becomes the name it refers to.
        /// </summary>
        static string Text (string documentation, string element, string parameter = null)
        {
            if (string.IsNullOrEmpty (documentation))
                return string.Empty;
            var xml = new XmlDocument ();
            try {
                xml.LoadXml (documentation);
            } catch (XmlException) {
                return string.Empty;
            }
            var path = parameter == null ? "/doc/" + element : "/doc/" + element + "[@name='" + parameter + "']";
            var node = xml.SelectSingleNode (path);
            if (node == null)
                return string.Empty;
            var builder = new StringBuilder ();
            AppendText (builder, node);
            return string.Join (" ", builder.ToString ().Split (new [] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        static void AppendText (StringBuilder builder, XmlNode node)
        {
            foreach (XmlNode child in node.ChildNodes) {
                if (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA || child.NodeType == XmlNodeType.Whitespace) {
                    builder.Append (child.Value);
                    continue;
                }
                var element = child as XmlElement;
                if (element == null)
                    continue;
                switch (element.Name) {
                case "see":
                    var cref = element.GetAttribute ("cref");
                    builder.Append (cref.Length > 2 && cref [1] == ':' ? cref.Substring (2) : cref);
                    break;
                case "paramref":
                    builder.Append (Naming.SnakeCase (element.GetAttribute ("name")));
                    break;
                case "item":
                    builder.Append (" - ");
                    AppendText (builder, element);
                    break;
                default:
                    AppendText (builder, element);
                    break;
                }
            }
        }
    }
}
