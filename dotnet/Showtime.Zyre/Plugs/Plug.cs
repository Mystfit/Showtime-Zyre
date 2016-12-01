using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace Showtime.Zyre.Plugs
{
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class Plug<T> where T : NetMQSocket {
        protected Plug() { }
        public Plug(string name, Node owner)
        {
            _owner = owner;
            _name = name;
        }

        public abstract void Init();

        private string _name;
        public string Name
        {
            get { return _name; }
            private set { _name = value; }
        }

        private Node _owner;
        [JsonIgnore]
        public Node Owner
        {
            get { return _owner; }
            set
            {
                _owner = value;
                Init();
            }
        }

        protected string _currentValue;
        public string CurrentValue
        {
            get { return _currentValue; }
            private set { _currentValue = value; }
        }

        private bool _dirty;
        public void SetDirty() { _dirty = true; }
        public void SetClean() { _dirty = false; }
        public bool IsDirty { get { return _dirty; } }

        public Address Path { get { return _path; } }
        protected Address _path;

        protected T _socket;
        [JsonIgnore]
        public T Socket { get { return _socket; } }
    }
}
