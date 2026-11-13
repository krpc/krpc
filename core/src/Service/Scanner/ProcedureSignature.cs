using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using KRPC.Utils;

namespace KRPC.Service.Scanner
{
    /// <summary>
    /// Signature information for a procedure, including procedure name,
    /// parameter types and return types.
    /// </summary>
    public sealed class ProcedureSignature
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
        /// The mod integrations the procedure requires, or null.
        /// </summary>
        public ModRequirement RequiredMods { get; private set; }

        /// <summary>
        /// Whether the procedure is deprecated.
        /// </summary>
        public bool Deprecated { get; private set; }

        /// <summary>
        /// If the procedure is deprecated, the reason for its deprecation (may be empty).
        /// </summary>
        public string DeprecatedReason { get; private set; }

        /// <summary>
        /// Whether the procedure is for client libraries only.
        /// </summary>
        public bool Internal { get; private set; }

        /// <summary>
        /// The procedure's parameters.
        /// </summary>
        public IList<ParameterSignature> Parameters { get; private set; }

        /// <summary>
        /// Whether the procedure returns a value.
        /// </summary>
        public bool HasReturnType { get; private set; }

        /// <summary>
        /// The return type of the procedure, with the nullability of every position inside it.
        /// </summary>
        public TypeSpec ReturnSpec { get; private set; }

        /// <summary>
        /// Create a procedure signature. An id of 0 means the id is generated from the name.
        /// </summary>
        internal ProcedureSignature (string serviceName, string procedureName, uint id, string documentation, IProcedureHandler handler, GameScene gameScene, bool deprecated, string deprecatedReason, bool isInternal = false, ModRequirement requiredMod = null)
        {
            Name = procedureName;
            FullyQualifiedName = serviceName + "." + Name;
            Id = TypeUtils.GetId (procedureName, id);
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Handler = handler;
            GameScene = gameScene;
            RequiredMods = requiredMod;
            Deprecated = deprecated;
            DeprecatedReason = deprecatedReason;
            Internal = isInternal;
            if (Internal && serviceName != "KRPC")
                throw new ServiceException (FullyQualifiedName + " is internal, which only a procedure of the KRPC service can be");
            Parameters = handler.Parameters.Select (x => new ParameterSignature (FullyQualifiedName, x)).ToList ();

            var returnType = handler.ReturnType;
            HasReturnType = (returnType != typeof(void));
            if (HasReturnType) {
                // Check it's a valid return type
                if (!TypeUtils.IsAValidType (returnType))
                    throw new ServiceException (returnType + " is not a valid Procedure return type, " + "in " + FullyQualifiedName);
                // A stream or an event is returned only as the whole value, and a stream only by an
                // internal procedure
                if (TypeUtils.HoldsAStream (returnType) &&
                    returnType != typeof(Messages.Stream) && returnType != typeof(Messages.Event))
                    throw new ServiceException ("The return value of " + FullyQualifiedName + " holds a stream or an event inside another value");
                if (returnType == typeof(Messages.Stream) && !Internal)
                    throw new ServiceException ("The return value of " + FullyQualifiedName + " holds a stream, which only an internal procedure can return");
                ReturnSpec = TypeSpec.Create (
                    returnType, handler.ReturnIsNullable, handler.ReturnNullablePaths,
                    "the return value of " + FullyQualifiedName);
            }
        }

        /// <summary>
        /// The values of the signature in a service definition.
        /// </summary>
        public OrderedDictionary Serialize ()
        {
            var result = new OrderedDictionary ();
            result ["id"] = Id;
            result ["parameters"] = Parameters.Select (x => x.Serialize ()).ToList ();
            if (HasReturnType)
                result ["return_type"] = TypeUtils.SerializeType (ReturnSpec);
            if (GameScene != GameScene.All)
                result ["game_scenes"] = GameSceneUtils.Serialize(GameScene);
            result ["documentation"] = Documentation;
            if (Deprecated) {
                result ["deprecated"] = true;
                result ["deprecated_reason"] = DeprecatedReason;
            }
            if (Internal)
                result ["internal"] = true;
            return result;
        }
    }
}
