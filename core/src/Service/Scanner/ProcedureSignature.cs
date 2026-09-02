using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a procedure, including procedure name,
    /// parameter types and return types.
    /// </summary>
    [Serializable]
    public sealed class ProcedureSignature : ISerializable
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
        /// Id of the procedure. Uniquely identifies the procedure within the service.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// Documentation for the procedure
        /// </summary>
        public string Documentation { get; private set; }

        /// <summary>
        /// The method that implements the procedure.
        /// </summary>
        public IProcedureHandler Handler { get; private set; }

        /// <summary>
        /// Which game scene(s) the service should be available during
        /// </summary>
        public GameScene GameScene { get; private set; }

        /// <summary>
        /// Whether the procedure is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the procedure is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// The procedure's parameters.
        /// </summary>
        public IList<ParameterSignature> Parameters { get; private set; }

        /// <summary>
        /// Whether the procedure returns a value.
        /// </summary>
        public bool HasReturnType { get; private set; }

        /// <summary>
        /// Return type of the procedure.
        /// </summary>
        public Type ReturnType { get; private set; }

        /// <summary>
        /// Whether the return type of the procedure could be null.
        /// </summary>
        public bool ReturnIsNullable { get; private set; }

        internal ProcedureSignature (string serviceName, string procedureName, uint id, string documentation, IProcedureHandler handler, GameScene gameScene, bool deprecated, string deprecatedReason)
        {
            Name = procedureName;
            FullyQualifiedName = serviceName + "." + Name;
            Id = id;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Handler = handler;
            GameScene = gameScene;
            Deprecated = deprecated;
            DeprecatedReason = deprecatedReason;
            Parameters = handler.Parameters.Select (x => new ParameterSignature (FullyQualifiedName, x)).ToList ();

            var returnType = handler.ReturnType;
            HasReturnType = (returnType != typeof(void));
            if (HasReturnType) {
                ReturnIsNullable = handler.ReturnIsNullable;
                // A Nullable<T> value-type return is represented by its underlying type T, and is
                // implicitly nullable.
                var underlyingType = System.Nullable.GetUnderlyingType (returnType);
                if (underlyingType != null) {
                    returnType = underlyingType;
                    ReturnIsNullable = true;
                }
                ReturnType = returnType;
                // Check it's a valid return type
                if (!TypeUtils.IsAValidType (returnType))
                    throw new ServiceException (returnType + " is not a valid Procedure return type, " + "in " + FullyQualifiedName);
            }
        }

        /// <summary>
        /// Serialize the signature.
        /// </summary>
        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue ("id", Id);
            info.AddValue ("parameters", Parameters);
            if (ReturnType != null)
                info.AddValue("return_type", TypeUtils.SerializeType(ReturnType, ReturnIsNullable));
            if (GameScene != GameScene.All)
                info.AddValue ("game_scenes", GameSceneUtils.Serialize(GameScene));
            info.AddValue ("documentation", Documentation);
            if (Deprecated) {
                info.AddValue ("deprecated", true);
                info.AddValue ("deprecated_reason", DeprecatedReason);
            }
        }
    }
}
