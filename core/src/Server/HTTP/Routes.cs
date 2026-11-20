using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KRPC.Utils;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// The path of each procedure, built from the service definitions.
    /// </summary>
    sealed class Routes
    {
        // The routes by path and then by method
        readonly Dictionary<string, Dictionary<string, Route>> routes = new Dictionary<string, Dictionary<string, Route>> ();

        public Routes (Schema.KRPC.Services services)
        {
            foreach (var service in services.Services_) {
                foreach (var procedure in service.Procedures) {
                    var route = Create (service.Name, procedure);
                    if (route == null)
                        continue;
                    Dictionary<string, Route> methods;
                    if (!routes.TryGetValue (route.Path, out methods)) {
                        methods = new Dictionary<string, Route> ();
                        routes [route.Path] = methods;
                    }
                    if (methods.ContainsKey (route.Method)) {
                        Logger.WriteLine ("HTTP: " + service.Name + "." + procedure.Name + " has the same path as " +
                            service.Name + "." + methods [route.Method].Procedure.Name + ", " + route.Path, Logger.Severity.Warning);
                        continue;
                    }
                    methods [route.Method] = route;
                }
            }
        }

        /// <summary>
        /// A name in kebab case, the words of its snake case joined by '-'.
        /// </summary>
        public static string KebabCase (string name)
        {
            return Naming.SnakeCase (name).Replace ('_', '-');
        }

        static Route Create (string service, Schema.KRPC.Procedure procedure)
        {
            if (procedure.Internal)
                return null;
            if (procedure.ReturnType != null && procedure.ReturnType.Code != Schema.KRPC.Type.Types.TypeCode.None &&
                !Codec.Supported (procedure.ReturnType))
                return null;
            if (procedure.Parameters.Any (x => x.Type == null || !Codec.Supported (x.Type)))
                return null;

            // The kind of procedure, from its name
            var parts = procedure.Name.Split ('_');
            var prefix = "/" + KebabCase (service) + "/";
            Route route;
            if (parts.Length == 1)
                route = new Route (service, procedure, "POST", prefix + KebabCase (parts [0]), false, false);
            else if (parts.Length == 2 && (parts [0] == "get" || parts [0] == "set"))
                route = new Route (service, procedure, parts [0] == "get" ? "GET" : "PUT",
                    prefix + KebabCase (parts [1]), false, parts [0] == "set");
            else if (parts.Length == 2)
                route = new Route (service, procedure, "POST",
                    prefix + KebabCase (parts [0]) + "/{id}/" + KebabCase (parts [1]), true, false);
            else if (parts.Length == 3 && parts [1] == "static")
                route = new Route (service, procedure, "POST",
                    prefix + KebabCase (parts [0]) + "/" + KebabCase (parts [2]), false, false);
            else if (parts.Length == 3 && (parts [1] == "get" || parts [1] == "set"))
                route = new Route (service, procedure, parts [1] == "get" ? "GET" : "PUT",
                    prefix + KebabCase (parts [0]) + "/{id}/" + KebabCase (parts [2]), true, parts [1] == "set");
            else
                return null;

            var first = route.Instance ? 1 : 0;
            if (procedure.Parameters.Count < first)
                return null;
            for (int i = first; i < procedure.Parameters.Count; i++)
                route.Parameters [route.Setter ? "value" : Naming.SnakeCase (procedure.Parameters [i].Name)] = i;
            return route;
        }

        /// <summary>
        /// A JSON object from alternating keys and values.
        /// </summary>
        public static List<KeyValuePair<string, object>> Entries (params object[] entries)
        {
            var result = new List<KeyValuePair<string, object>> ();
            for (int i = 0; i < entries.Length; i += 2)
                result.Add (new KeyValuePair<string, object> ((string)entries [i], entries [i + 1]));
            return result;
        }

        /// <summary>
        /// The route for a request. A class member's path holds the object identifier, which
        /// is returned in <paramref name="id"/>. Throws a <see cref="RequestException"/> if
        /// no procedure has the path, or none takes the method.
        /// </summary>
        public Route Find (string method, string path, out ulong id)
        {
            id = 0;
            var segments = path.Split ('/');
            // The path starts with '/', so a class member is "", service, class, id, member
            if (segments.Length == 5) {
                if (!ulong.TryParse (segments [3], NumberStyles.None, CultureInfo.InvariantCulture, out id))
                    throw new RequestException (404, "No procedure has the path " + path);
                segments [3] = "{id}";
                path = string.Join ("/", segments);
            }
            Dictionary<string, Route> methods;
            if (!routes.TryGetValue (path, out methods))
                throw new RequestException (404, "No procedure has the path " + path);
            Route route;
            if (!methods.TryGetValue (method, out route))
                throw new RequestException (405, path + " takes " + string.Join (", ", methods.Keys.OrderBy (x => x, StringComparer.Ordinal))) {
                    Allow = string.Join (", ", methods.Keys.OrderBy (x => x, StringComparer.Ordinal))
                };
            return route;
        }
    }
}
