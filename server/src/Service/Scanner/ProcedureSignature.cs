using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using KRPC.Utils;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a procedure, including procedure name,
    /// parameter types and return types.
    /// </summary>
    class ProcedureSignature : ISerializable
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

            // Create builders for the parameter types that are message types
            //ParameterBuilders = Parameters
            //    .Select (x => {
            //    try {
            //        return ProtocolBuffers.IsAMessageType (x.Type) ? ProtocolBuffers.BuilderForMessageType (x.Type) : null;
            //    } catch (ArgumentException) {
            //        throw new ServiceException ("Failed to instantiate a message builder for parameter type " + x.Type.Name);
            //    }
            //}).ToArray ();
            HasReturnType = (handler.ReturnType != typeof(void));
            if (HasReturnType) {
                ReturnType = handler.ReturnType;
                // Check it's a valid return type
                if (!TypeUtils.IsAValidType (ReturnType)) {
                    throw new ServiceException (
                        ReturnType + " is not a valid Procedure return type, " +
                        "in " + FullyQualifiedName);
                }
                // Add return type attributes
                Attributes.AddRange (TypeUtils.ReturnTypeAttributes (ReturnType));
                // Create a builder if it's a message type
                //if (ProtocolBuffers.IsAMessageType (ReturnType)) {
                //    try {
                //        ReturnBuilder = ProtocolBuffers.BuilderForMessageType (ReturnType);
                //    } catch (ArgumentException) {
                //        throw new ServiceException ("Failed to instantiate a message builder for return type " + ReturnType.Name);
                //    }
                //}
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
