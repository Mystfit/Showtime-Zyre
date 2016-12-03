using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NetMQ;
using Showtime.Zyre.Plugs;

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
        public List<Node> Nodes { get { return _nodes; } }

        public virtual Guid Uuid { get { return _uuid; } }
        protected Guid _uuid;

        private Action<string> _logger;
        public void Log(string message)
        {
            _logger?.Invoke(message);
        }

        protected object _sync;

        public Endpoint(string name, Guid uuid, Action<string> logger = null)
        {
            _sync = new object();
            _name = name;
            _uuid = uuid;
            _nodes = new List<Node>();
            _logger = logger;
        }

        public virtual void Close()
        {
            foreach (Node n in _nodes)
            {
                n.Close();
            }
        }

        public Node CreateNode(Node node)
        {
            node.Endpoint = this;
            _nodes.Add(node);
            return node;
        }

        public Node CreateNode(string name)
        {
            Node node = new Node(name, this);
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

        protected bool _isPolling;
        public abstract bool IsPolling { get; }
        public abstract void CheckPolling();

        public abstract void Whisper(Guid peer, NetMQMessage msg);

        public void ListNodes(bool listplugs=false)
        {
            Log("--------------------------");
            Log(string.Format("Nodes in {0}", Name));
            Log("--------------------------");
            for(int i = 0; i < _nodes.Count; i++) 
            {
                Node n = _nodes[i];
                Log(i + ": " + n.Name);
                if (listplugs)
                    n.ListPlugs();
            }
        }
    }
}
