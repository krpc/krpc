using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using Google.Protobuf;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    [SuppressMessage ("Gendarme.Rules.Maintainability", "AvoidLackOfCohesionOfMethodsRule")]
    [SuppressMessage ("Gendarme.Rules.Naming", "UseCorrectSuffixRule")]
    sealed class StreamImpl
    {
        readonly Connection connection;
        object _value;
        readonly object updateLock;
        readonly object condition = new object ();
        List<Action<object>> callbacks = new List<Action<object>> ();
        float rate = 0;

        [SuppressMessage ("Gendarme.Rules.Maintainability", "VariableNamesShouldNotMatchFieldNamesRule")]
        public StreamImpl (Connection connection, ulong id,
                           System.Type returnType, object updateLock)
        {
            this.connection = connection;
            Id = id;
            ReturnType = returnType;
            this.updateLock = updateLock;
        }

        public ulong Id { get; private set; }

        public System.Type ReturnType { get; private set; }

        public void Start () {
            if (!Started) {
                connection.KRPC ().StartStream (Id);
                Started = true;
            }
        }

        public float Rate {
            get { return rate; }
            set {
                rate = value;
                connection.KRPC ().SetStreamRate (Id, value);
            }
        }

        public bool Started { get; private set; }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public object Value {
            get {
                if (!Updated)
                    throw new System.InvalidOperationException ("Stream has no value");
                return _value;
            }
            set {
                lock (updateLock) {
                    _value = value;
                    Updated = true;
                }
            }
        }

        public bool Updated { get; private set; }

        public object Condition {
            get { return condition; }
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public List<Action<object>> Callbacks {
            get { return callbacks; }
        }

        [SuppressMessage ("Gendarme.Rules.Smells", "AvoidCodeDuplicatedInSameClassRule")]
        public void AddCallback (Action<object> callback) {
            lock (updateLock) {
                callbacks = new List<Action<object>>(callbacks);
                callbacks.Add (callback);
            }
        }

        [SuppressMessage ("Gendarme.Rules.BadPractice", "CheckNewExceptionWithoutThrowingRule")]
        public void Remove () {
            connection.StreamManager.RemoveStream (Id);
            lock (updateLock) {
                _value = new System.InvalidOperationException ("Stream does not exist");
            }
        }
    }
}
