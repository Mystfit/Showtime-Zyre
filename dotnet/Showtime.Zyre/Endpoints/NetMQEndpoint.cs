using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using Showtime.Zyre.Plugs;
using Newtonsoft.Json;

namespace Showtime.Zyre.Endpoints
{
    public class NetMQEndpoint : Endpoint
    {
        private NetMQPoller _poller;

        private Thread _updateThread;
        private bool _finishGraphUpdates;
        private List<GraphUpdate> _readyGraphUpdates;
        private BlockingQueue<GraphUpdate> _waitingGraphUpdates;

        public Dictionary<Guid, RemoteEndpoint> RemoteEndpoints { get { return _remoteEndpoints; } }
        private Dictionary<Guid, RemoteEndpoint> _remoteEndpoints;

        public NetMQEndpoint(string name, Guid uuid, Action<string> logger = null) : base(name, uuid, logger)
        {
#if NET35
            AsyncIO.ForceDotNet.Force();
#endif
            NetMQ.NetMQConfig.Linger = System.TimeSpan.FromSeconds(0);
            _remoteEndpoints = new Dictionary<Guid, RemoteEndpoint>();
            _poller = new NetMQPoller();
            _readyGraphUpdates = new List<GraphUpdate>();
            _waitingGraphUpdates = new BlockingQueue<GraphUpdate>();
            _updateThread = new Thread(RunUpdateLoop);
            _updateThread.Start();
        }

        private void RunUpdateLoop()
        {
            try
            {
                foreach (GraphUpdate update in _waitingGraphUpdates)
                {
                    if (!_readyGraphUpdates.Any(n => n.node.Path == update.node.Path))
                        _readyGraphUpdates.Add(update);
                    if (_waitingGraphUpdates.Length <= 0)
                    {
                        SendGraph(_readyGraphUpdates);
                        _readyGraphUpdates.Clear();
                    }
                }
            }
            catch (ThreadAbortException exit)
            {
                Log("Closing endpoint update thread " + exit);
            }
        }

        public override bool IsPolling
        {
            get
            {
                return _poller.IsRunning;
            }
        }

        public override void CheckPolling()
        {
            if (!IsPolling)
            {
                _poller.RunAsync();
            }
        }

        public override void RemoveNode(Node node)
        {
            _poller.Remove(node.InputSocket);
            base.RemoveNode(node);
        }

        public override Node CreateNode(string name)
        {
            Node node = base.CreateNode(name);
            UpdateGraph(node, GraphUpdate.UpdateType.CREATED);
            return node;
        }

        public override void DeregisterListenerNode(Node node)
        {
            lock (_sync)
            {
                _poller.Remove(node.InputSocket);
            }
        }

        public override void PlugConnectionRequest(InputPlug input, OutputPlug output)
        {
            Log("Local endpoint trying to initiate remote plug connection. Triggered from remote endpoint connection. Can safely ignore.");
        }

        public override void RegisterListenerNode(Node node)
        {
            lock (_sync)
            {
                if (!_poller.ContainsSocket(node.InputSocket))
                    _poller.Add(node.InputSocket);
            }
        }

        public override void SendMessageToOwner(NetMQMessage msg)
        {
            throw new MethodAccessException("Local endpoint received message destined for remote endpoint.");
        }

        public override void UpdateGraph(Node node, GraphUpdate.UpdateType type)
        {
            GraphUpdate update = new GraphUpdate() { updatetype = type, node = node };
            _waitingGraphUpdates.Enqueue(new GraphUpdate() { updatetype = type, node = node });
        }

        public override void Whisper(Guid peer, NetMQMessage msg)
        {
            Log("This is when I would send a message direct to another host");
        }

        public void SendGraph(List<GraphUpdate> nodes)
        {
            string jsonfullgraph = JsonConvert.SerializeObject(nodes);
            Log("Dispatching graph update");

            NetMQMessage replymsg = null;
            replymsg = new NetMQMessage(2);
            replymsg.Append(Endpoint.Commands.SEND_GRAPH.ToString());
            replymsg.Append(jsonfullgraph);
            Shout(replymsg);
        }


        public void Shout(NetMQMessage msg)
        {
            Log("This is when I would broadcast a message to all other endpoints");
        }

        private void ReceiveFromRemote(object sender, NetMQSocketEventArgs e)
        {
            NetMQMessage msg = e.Socket.ReceiveMultipartMessage();
            Guid remoteId = new Guid(msg[1].Buffer);
            string name = msg[2].ConvertToString();

            switch (msg[0].ConvertToString())
            {
                case "ENTER":
                    Console.WriteLine("New endpoint found");
                    RemoteEndpoint remote = new RemoteEndpoint(name, this, remoteId, (c) => { Console.WriteLine("REMOTE: " + c); });
                    remote.RequestRemoteGraph(remoteId);

                    if (!_remoteEndpoints.ContainsKey(remoteId))
                    {
                        _remoteEndpoints.Add(remoteId, remote);
                    }
                    else
                    {
                        _remoteEndpoints[remoteId] = remote;
                    }
                    break;
                case "WHISPER":
                    HandleWhisperMessage(msg);
                    break;
                case "SHOUT":
                    HandleShoutMessage(msg);
                    break;
                default:
                    break;
            }
        }

        private void HandleWhisperMessage(NetMQMessage msg)
        {
            string whispertype = msg[3].ConvertToString();
            if (whispertype == Endpoint.Commands.REQ_FULL_GRAPH.ToString())
            {
                SendFullGraph();
            }
            else if (whispertype == Endpoint.Commands.SEND_GRAPH.ToString())
            {
                ReceivedGraph(_remoteEndpoints[new Guid(msg[1].Buffer)], msg[4].ConvertToString());
            }
            else if (whispertype == Endpoint.Commands.PLUG_MSG.ToString())
            {
                Log("Received remote message for local plug: " + msg.ToString());
                Message remotemsg = Message.FromZyreWhisper(msg);
                OutputPlug outplug = RemoteEndpoints[new Guid(msg[1].Buffer)].LocateOutputPlugAtAddress(remotemsg.address);
                outplug.Update(remotemsg);
            }
            else if (whispertype == Endpoint.Commands.PLUG_CONNECT.ToString())
            {
                Address inputaddress = Address.FromFullPath(msg[4].ConvertToString());
                Address outputaddress = Address.FromFullPath(msg[5].ConvertToString());

                Log(string.Format("Remote request to connect O->:{0} to ->I:{1}", inputaddress.ToString(), outputaddress.ToString()));
                OutputPlug outplug = RemoteEndpoints[new Guid(msg[1].Buffer)].LocateOutputPlugAtAddress(outputaddress);
                InputPlug inplug = LocateInputPlugAtAddress(inputaddress);
                inplug.Connect(outplug);
            }
            else
            {
                Log("Unknown whisper type received for message " + msg.ToString());
            }
        }

        public void HandleShoutMessage(NetMQMessage msg)
        {
            string shouttype = msg[4].ConvertToString();
            if (shouttype == Endpoint.Commands.SEND_GRAPH.ToString())
            {
                ReceivedGraph(_remoteEndpoints[new Guid(msg[1].Buffer)], msg[5].ConvertToString());
            }
        }

        private void SendFullGraph()
        {
            Console.WriteLine("Queuing full graph update");
            foreach (Node node in _nodes)
            {
                UpdateGraph(node, GraphUpdate.UpdateType.UPDATED);
            }
        }

        private void ReceivedGraph(Endpoint remoteEndpoint, string graphjson)
        {
            Console.WriteLine("Got graph update from " + remoteEndpoint.Name);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new PrivateResolver()
            };

            List<GraphUpdate> remoteupdates = JsonConvert.DeserializeObject<List<GraphUpdate>>(graphjson, settings);
            foreach (GraphUpdate nodeupdate in remoteupdates)
            {
                //Create or retrieve current node
                Node activenode = remoteEndpoint.CreateNode(nodeupdate.node);

                var missingremoteinputs = nodeupdate.node.Inputs.Where(i => i.destroyed);
                var missingremoteoutputs = nodeupdate.node.Outputs.Where(o => o.destroyed);

                var newremoteinputs = nodeupdate.node.Inputs.Where(p => !activenode.Inputs.Any(l => p.Path == l.Path) && !p.destroyed);
                var newremoteoutputs = nodeupdate.node.Outputs.Where(p => !activenode.Outputs.Any(l => p.Path == l.Path) && !p.destroyed);

                //Remove old plugs from the local node
                foreach (InputPlug input in missingremoteinputs)
                {
                    InputPlug localinput = activenode.Inputs.Find(i => input.Name == i.Name);
                    localinput.Dispose();
                }

                foreach (OutputPlug output in missingremoteoutputs)
                {
                    OutputPlug localoutput = activenode.Outputs.Find(o => output.Name == o.Name);
                    localoutput.Dispose();
                }

                //Add new plugs to nodes.
                foreach (InputPlug newinput in newremoteinputs)
                {
                    activenode.CreateInputPlug(newinput);
                }

                foreach (OutputPlug newoutput in newremoteoutputs)
                {
                    activenode.CreateOutputPlug(newoutput);
                }
            }

            //Remove missing nodes that have disappeared from the remote endpoint
            var missingnodes = remoteEndpoint.Nodes.Where(p => !remoteupdates.Any(l => p.Name == l.node.Name));

            foreach (Node node in missingnodes)
                node.Dispose();

            Nodes.RemoveAll(n => missingnodes.Any(o => o.Name == n.Name));
        }
    }
}
