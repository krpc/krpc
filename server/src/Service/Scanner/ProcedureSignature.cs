using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a procedure, including procedure name,
    /// parameter types and return types.
    /// </summary>
    sealed class ProcedureSignature : ISerializable
    {
        /// <summary>
        /// Name of the procedure, not including the service it is in.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Name of the procedure including the service it is in.
        /// I.e. ServiceName.ProcedureName
        /// </summary>
        public string FullyQualifiedName { get; private set; }

        /// <summary>
        /// Documentation for the procedure
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// The method that implements the procedure.
        /// </summary>
        public IProcedureHandler Handler { get; private set; }

        public IList<ParameterSignature> Parameters { get; private set; }

        public bool HasReturnType { get; private set; }

        public Type ReturnType { get; private set; }

        /// <summary>
        /// Which game scene(s) the service should be active during
        /// </summary>
        public GameScene GameScene { get; private set; }

        public List<string> Attributes { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        public ProcedureSignature (string serviceName, string procedureName, string documentation, IProcedureHandler handler, GameScene gameScene, params string[] attributes)
        {
            Name = procedureName;
            FullyQualifiedName = serviceName + "." + Name;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Handler = handler;
            GameScene = gameScene;
            Attributes = attributes.ToList ();
            Parameters = handler.Parameters.Select (x => new ParameterSignature (FullyQualifiedName, x)).ToList ();

            // Add parameter type attributes
            for (int position = 0; position < Parameters.Count; position++)
                Attributes.AddRange (TypeUtils.ParameterTypeAttributes (position, Parameters [position].Type));

            var returnType = handler.ReturnType;
            HasReturnType = (returnType != typeof(void));
            if (HasReturnType) {
                ReturnType = returnType;
                // Check it's a valid return type
                if (!TypeUtils.IsAValidType (returnType))
                    throw new ServiceException (returnType + " is not a valid Procedure return type, " + "in " + FullyQualifiedName);
                // Add return type attributes
                Attributes.AddRange (TypeUtils.ReturnTypeAttributes (returnType));
            }
        }

        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("parameters", Parameters);
            if (ReturnType != null)
                info.AddValue ("return_type", TypeUtils.GetTypeName (ReturnType));
            info.AddValue ("attributes", Attributes);
            info.AddValue ("documentation", Documentation);
        }
    }
}
