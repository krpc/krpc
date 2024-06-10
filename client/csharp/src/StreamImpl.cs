using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using Google.Protobuf;
using KRPC.Client.Services.KRPC;
using KRPC.Schema.KRPC;

namespace KRPC.Client
{
    sealed class StreamImpl
    {
        readonly Connection connection;
        object _value;
        readonly object updateLock;
        readonly object condition = new object ();
        IDictionary<int, Action<object>> callbacks = new Dictionary<int, Action<object>> ();
        int nextCallbackKey;
        float rate = 0;

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

        public IDictionary<int, Action<object>> Callbacks {
            get { return callbacks; }
        }

        public int AddCallback (Action<object> callback) {
            lock (updateLock) {
                int key = nextCallbackKey;
                nextCallbackKey++;
                callbacks[key] = callback;
                return key;
            }
        }

        public void RemoveCallback (int key) {
            lock (updateLock) {
                callbacks.Remove (key);
            }
        }

        public void Remove () {
            connection.StreamManager.RemoveStream (Id);
            lock (updateLock) {
                _value = new System.InvalidOperationException ("Stream does not exist");
            }
        }
    }
}
