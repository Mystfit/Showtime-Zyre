using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Showtime.Zyre.Endpoints
{
    public class Endpoint : IDisposable
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

        public Endpoint(string name, Action<string> logger)
        {
            _name = name;
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
            return CreateNode(name, this);
        }

        public virtual Node CreateNode(string name, Endpoint endpoint)
        {
            Node node = new Node(name, endpoint);
            _nodes.Add(node);
            return node;
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void Dispose()
        {
            Close();
        }
    }
}
