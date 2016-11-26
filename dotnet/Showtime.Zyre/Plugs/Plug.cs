using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace Showtime.Zyre.Plugs
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Plug<T> where T : NetMQSocket
    {
        public Plug(string name, Node owner)
        {
            _owner = owner;
            _name = name;
        }

        private string _name;
        public string Name { get { return _name; } }

        private Node _owner;
        public Node Owner { get { return _owner; } }

        protected bool _isconnected;
        public bool IsConnected { get { return _isconnected; } }

        protected string _targetaddress;
        public string Address { get { return _targetaddress; } }

        protected string _currentValue;
        public string CurrentValue { get { return _currentValue; } }

        private bool _dirty;
        public void SetDirty() { _dirty = true; }
        public void SetClean() { _dirty = false; }
        public bool IsDirty { get { return _dirty; } }

        protected T _socket;
        public T Socket { get { return _socket; } }

        [JsonProperty]
        public string FullName { get { return String.Format("{0}/{1}/{2}", _owner.GetEndpoint?.Name, _owner.Name, _name); } }

    }
}
