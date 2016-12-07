using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NetMQ;
using Showtime.Zyre.Plugs;

namespace Showtime.Zyre.Endpoints
{
    public struct GraphUpdate
    {
        public enum UpdateType { CREATED=0, DESTROYED, UPDATED };
        public UpdateType updatetype;
        public Node node;
        public static bool operator ==(GraphUpdate update1, GraphUpdate update2)
        {
            return update1.node.Equals(update2.node) && update1.updatetype.Equals(update2.updatetype);
        }

        public static bool operator !=(GraphUpdate update1, GraphUpdate update2)
        {
            return !update1.node.Equals(update2.node) || !update1.updatetype.Equals(update2.updatetype);
        }
    }

    public abstract class Endpoint : IDisposable
    {
        public enum Commands
        {
            REQ_FULL_GRAPH,
            SEND_GRAPH,
            PLUG_MSG,
            PLUG_CONNECT
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

        public Node CreateNode(Node node)
        {
            if(node.Endpoint == null)
                node.Endpoint = this;

            if (!_nodes.Any(n => n.Path == node.Path))
            {
                _nodes.Add(node);
                foreach(InputPlug input in node.Inputs)
                {
                    if (input.Owner == null)                    
                        node.CreateInputPlug(input);
                }
                    
                foreach (OutputPlug output in node.Outputs)
                {
                    if(output.Owner == null)
                        node.CreateOutputPlug(output);
                }
                    
            } else
            {
                return _nodes.Find(n => n.Path == node.Path);
            }
           
            return node;
        }

        public virtual Node CreateNode(string name)
        {
            Node node = new Node(name, this);
            _nodes.Add(node);
            return node;
        }

        public abstract void UpdateGraph(Node node, GraphUpdate.UpdateType type);
        public abstract void RegisterListenerNode(Node node);
        public abstract void DeregisterListenerNode(Node node);

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        protected bool _isPolling;
        public abstract bool IsPolling { get; }
        public abstract void CheckPolling();
        public virtual void RemoveNode(Node node)
        {
            if (!IsDisposing)
                _nodes.Remove(node);
        }

        public abstract void Whisper(Guid peer, NetMQMessage msg);
        public abstract void SendMessageToOwner(NetMQMessage msg);
        public abstract void PlugConnectionRequest(InputPlug input, OutputPlug output);

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

        public OutputPlug LocateOutputPlugAtAddress(Address address)
        {
            Node n = Nodes.Find(node => node.Name == address.node);
            OutputPlug p = n.Outputs.Find(outplug => outplug.Name == address.originPlug);
            return Nodes.Find(node => node.Name == address.node)?.Outputs.Find(outplug => outplug.Name == address.originPlug);
        }

        public InputPlug LocateInputPlugAtAddress(Address address)
        {
            Node n = Nodes.Find(node => node.Name == address.node);
            InputPlug p = n.Inputs.Find(outplug => outplug.Name == address.originPlug);
            return Nodes.Find(node => node.Name == address.node)?.Inputs.Find(outplug => outplug.Name == address.originPlug);
        }


        #region IDisposable Support
        private bool _disposedValue = false; // To detect redundant calls
        public bool IsDisposing { get { return _disposedValue; } }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _disposedValue = true;
                    foreach (Node n in Nodes)
                    {
                        n.Dispose();
                    }
                    Nodes.Clear();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Endpoint() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
