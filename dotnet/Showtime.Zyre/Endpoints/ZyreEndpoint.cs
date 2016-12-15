using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ.Zyre;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Showtime.Zyre.Endpoints;
using Showtime.Zyre.Plugs;
using NetMQ.Zyre.ZyreEvents;

namespace Showtime.Zyre
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ZyreEndpoint : Endpoint
    {
        private NetMQ.Zyre.Zyre _zyre;
        private NetMQPoller _poller;

        private Thread _updateThread;
        private bool _finishGraphUpdates;
        private List<GraphUpdate> _readyGraphUpdates;
        private BlockingQueue<GraphUpdate> _waitingGraphUpdates;

        public Dictionary<Guid, RemoteEndpoint> RemoteEndpoints { get { return _remoteEndpoints; } }
        private Dictionary<Guid, RemoteEndpoint> _remoteEndpoints;

        public override Guid Uuid
        {
            get
            {
                if (_zyre != null)
                    return _zyre.Uuid();
                return Guid.Empty;
            }
        }

        public ZyreEndpoint(string name, Action<string> logger=null) : base(name, Guid.Empty, logger)
        {
#if NET35
            AsyncIO.ForceDotNet.Force();
#endif
            NetMQ.NetMQConfig.Linger = System.TimeSpan.FromSeconds(0);

            _remoteEndpoints = new Dictionary<Guid, RemoteEndpoint>();

            _poller = new NetMQPoller();
            //StartZyre();

            _readyGraphUpdates = new List<GraphUpdate>();
            _waitingGraphUpdates = new BlockingQueue<GraphUpdate>();
            _updateThread = new Thread(RunUpdateLoop);
            _updateThread.Start();
        }

        public void StartZyre()
        {
            lock (_sync)
            {
                _zyre = new NetMQ.Zyre.Zyre(Name, true, Log);
                //_zyre.Socket.ReceiveReady += ReceiveFromRemote;

                _zyre.EnterEvent += _zyre_EnterEvent;
                _zyre.ShoutEvent += _zyre_ShoutEvent;
                _zyre.WhisperEvent += _zyre_WhisperEvent;

                //_poller.Add(_zyre.Socket);

                _zyre.Join("ZST");
                _zyre.Start();
                _zyre.Peers();

                //CheckPolling();

                _uuid = _zyre.Uuid();
            }
        }

        private void _zyre_WhisperEvent(object sender, NetMQ.Zyre.ZyreEvents.ZyreEventWhisper e)
        {
            HandleWhisperMessage(e.SenderUuid, e.SenderName, e.Content);
        }

        private void _zyre_ShoutEvent(object sender, NetMQ.Zyre.ZyreEvents.ZyreEventShout e)
        {
            HandleShoutMessage(e.SenderUuid, e.SenderName, e.Content);
        }

        private void _zyre_EnterEvent(object sender, NetMQ.Zyre.ZyreEvents.ZyreEventEnter e)
        {
            HandleEnterMessage(e.SenderUuid, e.SenderName);
        }

        public override bool IsPolling{ get { return _poller.IsRunning; } }

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

        public override void UpdateGraph(Node node, GraphUpdate.UpdateType type)
        {
            GraphUpdate update = new GraphUpdate() { updatetype = type, node = node };
            _waitingGraphUpdates.Enqueue(new GraphUpdate() { updatetype = type, node = node });
        }

        private void RunUpdateLoop()
        {
            try
            {
                foreach (GraphUpdate update in _waitingGraphUpdates)
                {
                    if(!_readyGraphUpdates.Any(n => n.node.Path == update.node.Path))
                        _readyGraphUpdates.Add(update);
                    if (_waitingGraphUpdates.Length <= 0)
                    {
                        SendGraph(_readyGraphUpdates);
                        _readyGraphUpdates.Clear();
                    }
                }
            } catch(ThreadAbortException exit)
            {
                Log("Closing endpoint update thread " + exit);
            }
        }

        public override void RegisterListenerNode(Node node)
        {
            lock (_sync)
            {
                if(!_poller.ContainsSocket(node.InputSocket))
                    _poller.Add(node.InputSocket);
            }
        }

        public override void DeregisterListenerNode(Node node)
        {
            lock (_sync)
            {
                _poller.Remove(node.InputSocket);
            }
        }

        private void ReceiveFromRemote(object sender, NetMQSocketEventArgs e)
        {
            NetMQMessage msg = e.Socket.ReceiveMultipartMessage();
            string msgtype = msg.Pop().ConvertToString();
            Guid remoteId = new Guid(msg.Pop().Buffer);
            string name = msg.Pop().ConvertToString();

            switch (msgtype)
            {
                case "ENTER":
                    HandleEnterMessage(remoteId, name);
                    break;
                case "WHISPER":
                    HandleWhisperMessage(remoteId, name, msg);
                    break;
                case "SHOUT":
                    HandleShoutMessage(remoteId, name, msg);
                    break;
                default:
                    break;
            }
        }

        private void HandleEnterMessage(Guid sender, string name){
            Console.WriteLine("New endpoint found");
            RemoteEndpoint remote = new RemoteEndpoint(name, this, sender, (c) => { Console.WriteLine("REMOTE: " + c); });
            remote.RequestRemoteGraph(sender);

            if (!_remoteEndpoints.ContainsKey(sender))
            {
                _remoteEndpoints.Add(sender, remote);
            }
            else
            {
                _remoteEndpoints[sender] = remote;
            }
        }

        private void HandleWhisperMessage(Guid sender, string name, NetMQMessage msg)
        {
            string whispertype = msg[0].ConvertToString();
            if (whispertype == Endpoint.Commands.REQ_FULL_GRAPH.ToString())
            {
                SendFullGraph();
            }
            else if (whispertype == Endpoint.Commands.SEND_GRAPH.ToString())
            {
                ReceivedGraph(_remoteEndpoints[sender], msg[1].ConvertToString());
            }
            else if (whispertype == Endpoint.Commands.PLUG_MSG.ToString())
            {
                Log("Received remote message for local plug: " + msg.ToString());
                Message remotemsg = Message.FromZyreWhisper(msg);
                OutputPlug outplug = RemoteEndpoints[sender].LocateOutputPlugAtAddress(remotemsg.address);
                outplug.Update(remotemsg);
            }
            else if (whispertype == Endpoint.Commands.PLUG_CONNECT.ToString())
            {
                Address inputaddress = Address.FromFullPath(msg[1].ConvertToString());
                Address outputaddress = Address.FromFullPath(msg[2].ConvertToString());

                Log(string.Format("Remote request to connect O->:{0} to ->I:{1}", inputaddress.ToString(), outputaddress.ToString()));
                OutputPlug outplug = RemoteEndpoints[sender].LocateOutputPlugAtAddress(outputaddress);
                InputPlug inplug = LocateInputPlugAtAddress(inputaddress);
                inplug.Connect(outplug);
            }
            else
            {
                Log("Unknown whisper type received for message " + msg.ToString());
            }
        }

        public void HandleShoutMessage(Guid sender, string name, NetMQMessage msg)
        {
            string shouttype = msg[0].ConvertToString();
            if (shouttype == Endpoint.Commands.SEND_GRAPH.ToString())
            {
                ReceivedGraph(_remoteEndpoints[sender], msg[1].ConvertToString());
            }
        }

        private void SendFullGraph()
        {
            Console.WriteLine("Queuing full graph update");
            foreach(Node node in _nodes)
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
            _zyre?.Shout("ZST", msg);
        }

        public override void SendMessageToOwner(NetMQMessage message)
        {
            throw new MethodAccessException("Local endpoint received message destined for remote endpoint.");
        }

        public override void PlugConnectionRequest(InputPlug input, OutputPlug output)
        {
            Log("Local endpoint trying to initiate remote plug connection. Triggered from remote endpoint connection. Can safely ignore.");
        }

        public override void Whisper(Guid peer, NetMQMessage msg)
        {
            _zyre.Whisper(peer, msg);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _readyGraphUpdates = null;
                _updateThread.Abort();
                _poller.StopAsync();
                _poller.Dispose();
            }
            base.Dispose(disposing);

            _zyre.Dispose();
            NetMQConfig.Cleanup(true);
        }
    }
}
