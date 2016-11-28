using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NetMQ;

namespace Showtime.Zyre.Endpoints
{
    public abstract class Endpoint : IDisposable
    {
        public enum Commands
        {
            REQ_FULL_GRAPH,
            SEND_FULL_GRAPH,
            SEND_PARTIAL_GRAPH
        }

        [JsonProperty]
        public string Name { get { return _name; } }
        private string _name;

        [JsonProperty]
        protected List<Node> _nodes;

        public virtual Guid Uuid { get { return _uuid; } }
        protected Guid _uuid;

        public Endpoint(string name, Guid uuid, Action<string> logger = null)
        {
            _name = name;
            _uuid = uuid;
            _nodes = new List<Node>();
        }

        public virtual void Close()
        {
            foreach (Node n in _nodes)
            {
                n.Close();
            }
        }

        public virtual Node CreateNode(string name)
        {
            Node node = new Node(name, this);
            RegisterListenerNode(node);
            _nodes.Add(node);
            return node;
        }

        public abstract void RegisterListenerNode(Node node);
        public abstract void DeregisterListenerNode(Node node);

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void Dispose()
        {
            Close();
        }

        public abstract void Whisper(Guid peer, NetMQMessage msg);
    }
}
