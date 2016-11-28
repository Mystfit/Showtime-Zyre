﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ.Zyre;
using NetMQ;
using Newtonsoft.Json;
using Showtime.Zyre.Endpoints;

namespace Showtime.Zyre
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LocalEndpoint : Endpoint
    {
        private NetMQ.Zyre.Zyre _zyre;
        private NetMQPoller _poller;
        private Thread _pollthread;

        public List<RemoteEndpoint> RemoteEndpoints { get { return _remoteEndpoints; } }
        private List<RemoteEndpoint> _remoteEndpoints;

        public override Guid Uuid
        {
            get
            {
                if (_zyre != null)
                    return _zyre.Uuid();
                return Guid.Empty;
            }
        }

        public LocalEndpoint(string name, Action<string> logger=null) : base(name, Guid.Empty)
        {
            _remoteEndpoints = new List<RemoteEndpoint>();

            NetMQ.NetMQConfig.Linger = System.TimeSpan.FromSeconds(0);
            _poller = new NetMQPoller();
            _pollthread = new Thread(() => {
                Bootstrap();
                _poller.Run();
            });
            _pollthread.Name = "endpoint-poller";
            _pollthread.Start();

            StartZyre();
        }

        private void StartZyre()
        {
            _zyre = new NetMQ.Zyre.Zyre(Name, false, (s)=> { });
            _zyre.Socket.ReceiveReady += ReceiveFromRemote;
            _poller.Add(_zyre.Socket);
            _zyre.Join("ZST");
            _zyre.Start();
            _uuid = _zyre.Uuid();
        }

        public void Bootstrap()
        {
            //BUG
            //We create a temporary Zyre node to kickstart the poller which seems to block until a Zyre node is discovered
            NetMQ.Zyre.Zyre bootstrap = new NetMQ.Zyre.Zyre("bootstrap", true);
            bootstrap.Join("ZST");

            NetMQMessage m = new NetMQMessage(1);
            m.Append("WAKEUP");
            bootstrap.Shout("ZST", m);
            Thread.Sleep(100);
            bootstrap.Dispose();
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
            _poller.Add(node.InputSocket);
        }

        public override void DeregisterListenerNode(Node node)
        {
            _poller.Remove(node.InputSocket);
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
                    RemoteEndpoint remote = new RemoteEndpoint(msg[2].ConvertToString(), this, new Guid(msg[1].Buffer));
                    remote.RequestRemoteGraph(new Guid(msg[1].Buffer));
                    _remoteEndpoints.Add(remote);

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
                        ReceivedFullGraph(msg[5].ConvertToString());
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
            replymsg.Append(_zyre.Uuid().ToString());
            replymsg.Append(JsonConvert.SerializeObject(_nodes));
            Whisper(peer, replymsg);
        }

        private void ReceivedFullGraph(string graphjson)
        {
            Console.WriteLine("Got full graph: " + graphjson);
            JsonConvert.DeserializeObject<List<Node>>(graphjson);
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
