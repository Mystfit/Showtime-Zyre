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

namespace Showtime.Zyre
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LocalEndpoint : Endpoint
    {
        private NetMQ.Zyre.Zyre _zyre;
        private NetMQPoller _poller;
        private Thread _pollthread;

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

        public LocalEndpoint(string name, Action<string> logger=null) : base(name, Guid.Empty, logger)
        {
            NetMQ.NetMQConfig.Linger = System.TimeSpan.FromSeconds(0);
            NetMQ.NetMQConfig.ThreadPoolSize = 3;

            _remoteEndpoints = new Dictionary<Guid, RemoteEndpoint>();
            _poller = new NetMQPoller();
            _poller.RunAsync();

            //_pollthread = new Thread(() =>
            //{
                // Bootstrap();
                //StartZyre();
                //_poller.Run();
            //});
            //_pollthread.Name = "endpoint-poller";
            //_pollthread.Start();
        }

        private void StartZyre()
        {
            lock (_sync)
            {
                _zyre = new NetMQ.Zyre.Zyre(Name, false, (s) => { });
                _zyre.Socket.ReceiveReady += ReceiveFromRemote;
                _poller.Add(_zyre.Socket);
                _zyre.Join("ZST");
                _zyre.Start();
                _uuid = _zyre.Uuid();
            }
        }

        public void Bootstrap()
        {
            //BUG
            //We create a temporary Zyre node to kickstart the poller which seems to block until a Zyre node is discovered
            lock (_sync)
            {
                NetMQ.Zyre.Zyre bootstrap = new NetMQ.Zyre.Zyre("bootstrap", true);
                bootstrap.Join("ZST");

                NetMQMessage m = new NetMQMessage(1);
                m.Append("WAKEUP");
                bootstrap.Shout("ZST", m);
                Thread.Sleep(100);
                bootstrap.Dispose();
            }
        }

        public override void Close()
        {
            base.Close();

            _poller.Stop();
            _pollthread.Join();
            _poller.Dispose();
            _zyre.Dispose();

            NetMQConfig.Cleanup();
        }

        public override void RegisterListenerNode(Node node)
        {
            lock (_sync)
            {
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

        public void SendToRemote(string msg)
        {
            NetMQMessage m = new NetMQMessage(1);
            m.Append(msg);
            _zyre.Shout("ZST", m);
        }

        private void ReceiveFromRemote(object sender, NetMQSocketEventArgs e)
        {
            NetMQMessage msg = e.Socket.ReceiveMultipartMessage();
            
            switch (msg[0].ConvertToString())
            {
                case "ENTER":
                    Console.WriteLine("New endpoint found");
                    string name = msg[2].ConvertToString();
                    Guid remoteId = new Guid(msg[1].Buffer);
                    RemoteEndpoint remote = new RemoteEndpoint(name, this, remoteId, (c)=> { Console.WriteLine("REMOTE: " + c); });
                    remote.RequestRemoteGraph(remoteId);

                    if (!_remoteEndpoints.ContainsKey(remoteId)){
                        _remoteEndpoints.Add(remoteId, remote);
                    } else
                    {
                        _remoteEndpoints[remoteId] = remote;
                    }
                    

                    break;
                case "WHISPER":
                    string whispertype = msg[3].ConvertToString();
                    if (whispertype == Endpoint.Commands.REQ_FULL_GRAPH.ToString())
                    {
                        Console.WriteLine("Sending full graph to " + msg[2].ConvertToString());
                        SendFullGraph(new Guid(msg[1].Buffer));
                    }
                    else if (whispertype == Endpoint.Commands.SEND_FULL_GRAPH.ToString())
                    {
                        ReceivedFullGraph(_remoteEndpoints[new Guid(msg[1].Buffer)], msg[4].ConvertToString());
                    } else if (whispertype == Endpoint.Commands.SEND_PARTIAL_GRAPH.ToString())
                    {
                        ReceivedPartialGraph();
                    }
                    break;
                case "SHOUT":
                    NetMQ.Zyre.ZreMsg.ShoutMessage s = new ZreMsg.ShoutMessage();
                    Console.WriteLine("Shout " + msg[4].ConvertToString());
                    break;
                default:
                    break;
            }
        }

        private void SendFullGraph(Guid peer)
        {
            NetMQMessage replymsg = null;
            replymsg = new NetMQMessage(2);
            replymsg.Append(Endpoint.Commands.SEND_FULL_GRAPH.ToString());
            replymsg.Append(JsonConvert.SerializeObject(_nodes));
            Whisper(peer, replymsg);
        }

        private void ReceivedFullGraph(Endpoint remoteEndpoint, string graphjson)
        {
            Console.WriteLine("Got full graph: " + graphjson);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new PrivateResolver()
            };

            List<Node> remotenodes = JsonConvert.DeserializeObject<List<Node>>(graphjson, settings);
            foreach(Node n in remotenodes)
            {
                remoteEndpoint.CreateNode(n);
                Console.WriteLine("New remote node " + n.Name);
                foreach (InputPlug p in n.Inputs)
                {
                    p.Owner = n;
                    Console.WriteLine("New remote input " + p.Path);
                }

                foreach (OutputPlug p in n.Outputs)
                {
                    p.Owner = n;
                    Console.WriteLine("New remote output " + p.Path);
                }
            }
        }

        private void ReceivedPartialGraph()
        {
            Console.WriteLine("Got partial graph");
        }

        public override void Whisper(Guid peer, NetMQMessage msg)
        {
            _zyre.Whisper(peer, msg);
        }
    }
}
