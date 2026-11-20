using System.Collections.Generic;

namespace KRPC.Server.HTTP
{
    /// <summary>
    /// A procedure, and the HTTP request that calls it.
    /// </summary>
    sealed class Route
    {
        public Route (string service, Schema.KRPC.Procedure procedure, string method, string path, bool instance, bool setter)
        {
            Service = service;
            Procedure = procedure;
            Method = method;
            Path = path;
            Instance = instance;
            Setter = setter;
            Parameters = new Dictionary<string, int> ();
        }

        public string Service { get; private set; }

        public Schema.KRPC.Procedure Procedure { get; private set; }

        /// <summary>
        /// GET for a getter, PUT for a setter, and POST for any other procedure.
        /// </summary>
        public string Method { get; private set; }

        /// <summary>
        /// The path, with <c>{id}</c> in place of the object of a class member.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Whether the procedure is a class member that takes the object as its first parameter.
        /// </summary>
        public bool Instance { get; private set; }

        public bool Setter { get; private set; }

        /// <summary>
        /// The position of each parameter the body passes, by its name in snake case.
        /// </summary>
        public IDictionary<string, int> Parameters { get; private set; }
    }
}
