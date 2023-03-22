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

        /// <summary>
        /// Whether the procedure is a static method.
        /// </summary>
        public bool IsStatic { get; private set; }

        /// <summary>
        /// Whether the procedure is a class method/property.
        /// </summary>
        public bool IsClassMember { get; private set; }

        /// <summary>
        /// If the procedure is a class method/property, the name of the class it is a member of.
        /// </summary>
        public string ClassName { get; private set; }

        /// <summary>
        /// Whether the procedure is a property getter.
        /// </summary>
        public bool IsPropertyGetter { get; private set; }

        /// <summary>
        /// Whether the procedure is a property setter.
        /// </summary>
        public bool IsPropertySetter { get; private set; }

        /// <summary>
        /// If the procedure is a property getter/setter, the name of the property.
        /// </summary>
        public string PropertyName { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidLongParameterListsRule")]
        internal ProcedureSignature (string serviceName, string procedureName, uint id, string documentation, IProcedureHandler handler, GameScene gameScene)
        {
            Name = procedureName;
            FullyQualifiedName = serviceName + "." + Name;
            Id = id;
            Documentation = DocumentationUtils.ResolveCrefs (documentation);
            Handler = handler;
            GameScene = gameScene;
            Parameters = handler.Parameters.Select (x => new ParameterSignature (FullyQualifiedName, x)).ToList ();

            var returnType = handler.ReturnType;
            HasReturnType = (returnType != typeof(void));
            if (HasReturnType) {
                ReturnType = returnType;
                // Check it's a valid return type
                if (!TypeUtils.IsAValidType (returnType))
                    throw new ServiceException (returnType + " is not a valid Procedure return type, " + "in " + FullyQualifiedName);
                ReturnIsNullable = handler.ReturnIsNullable;
            }

            var parts = procedureName.Split (new char[]{'_'});
            if (parts.Length == 2) {
                if (parts [0] == ("get")) {
                    IsPropertyGetter = true;
                    PropertyName = parts [1];
                } else if (parts [0] == "set") {
                    IsPropertySetter = true;
                    PropertyName = parts [1];
                } else {
                    IsClassMember = true;
                    ClassName = parts [0];
                }
            } else if (parts.Length == 3) {
                if (parts [1] == "get") {
                    IsClassMember = true;
                    IsPropertyGetter = true;
                    PropertyName = parts [2];
                } else if (parts [1] == "set") {
                    IsClassMember = true;
                    ClassName = parts [0];
                    IsPropertySetter = true;
                    PropertyName = parts [2];
                } else if (parts [1] == "static") {
                    IsClassMember = true;
                    ClassName = parts [0];
                    IsStatic = true;
                }
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
            {
                info.AddValue("return_type", TypeUtils.SerializeType(ReturnType));
                info.AddValue("return_is_nullable", ReturnIsNullable);
            }
            if (GameScene != GameScene.All)
                info.AddValue ("game_scenes", GameSceneUtils.Serialize(GameScene));
            info.AddValue ("documentation", Documentation);
        }
    }
}
