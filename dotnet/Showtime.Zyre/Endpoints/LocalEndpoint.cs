using System;
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
        private NetMQ.Zyre.Zyre zyre;
        private NetMQPoller _poller;
        private Thread _pollthread;

        public List<RemoteEndpoint> RemoteEndpoints { get { return _remoteEndpoints; } }
        private List<RemoteEndpoint> _remoteEndpoints;

        public LocalEndpoint(string name, Action<string> logger=null) : base(name, logger)
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
            zyre = new NetMQ.Zyre.Zyre(Name, false, (s)=> { });
            zyre.Socket.ReceiveReady += ReceiveFromRemote;
            _poller.Add(zyre.Socket);
            zyre.Join("ZST");
            zyre.Start();            
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
            zyre.Dispose();

            NetMQConfig.Cleanup();
        }

        public override Node CreateNode(string name)
        {
            Node node = base.CreateNode(name, this);
            _poller.Add(node.InputSocket);
            return node;
        }

        public void SendToRemote(string msg)
        {
            NetMQMessage m = new NetMQMessage(1);
            m.Append(msg);
            zyre.Shout("ZST", m);
        }

        private void ReceiveFromRemote(object sender, NetMQSocketEventArgs e)
        {
            NetMQMessage msg = e.Socket.ReceiveMultipartMessage();
            NetMQMessage replymsg = null;
            switch (msg[0].ConvertToString())
            {
                case "ENTER":
                    Console.WriteLine("New endpoint found");
                    replymsg = new NetMQMessage(2);
                    replymsg.Append(Endpoint.Commands.REQ_FULL_GRAPH.ToString());
                    replymsg.Append(zyre.Uuid().ToString());
                    zyre.Whisper(new Guid(msg[1].Buffer), replymsg);
                    break;
                case "WHISPER":
                    if (msg[3].ConvertToString() == Endpoint.Commands.REQ_FULL_GRAPH.ToString())
                    {
                        Console.WriteLine("Sending full graph to " + msg[2].ConvertToString());
                        replymsg = new NetMQMessage(2);
                        replymsg.Append(Endpoint.Commands.SEND_FULL_GRAPH.ToString());
                        replymsg.Append(zyre.Uuid().ToString());
                        zyre.Whisper(new Guid(msg[1].Buffer), replymsg);
                    } else if (msg[3].ConvertToString() == Endpoint.Commands.SEND_FULL_GRAPH.ToString())
                    {
                        Console.WriteLine("Got full graph");
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
    }
}
