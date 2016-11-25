using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ.Zyre;
using NetMQ;

namespace Showtime.Zyre
{
    public class Endpoint
    {
        private NetMQ.Zyre.Zyre zyre;

        private string _name;
        public string Name { get { return _name; } }

        public delegate void IncomingListener(string message);
        public IncomingListener incoming;

        private NetMQPoller _poller;
        private List<Node> _nodes;
        private Thread _pollthread;

        public Endpoint(string name, Action<string> logger)
        {
            _name = name;
            NetMQ.NetMQConfig.Linger = System.TimeSpan.FromSeconds(0);
            zyre = new NetMQ.Zyre.Zyre(name, false, logger);
            zyre.Socket.ReceiveReady += ReceiveFromRemote;
            zyre.Join("ZST");
            zyre.Start();

            _nodes = new List<Node>();
            _poller = new NetMQPoller();
            _poller.Add(zyre.Socket);
            _pollthread = new Thread(() => {
                _poller.Run();
            });
            _pollthread.Name = "endpoint-poller";
            _pollthread.Start();
        }

        public void Close()
        {
            _poller.Stop();
            _pollthread.Join();
            _poller.Dispose();

            foreach(Node n in _nodes)
            {
                n.Close();
            }

            zyre.Dispose();
            NetMQConfig.Cleanup();
        }

        public Node CreateNode(string name)
        {
            Node node = new Node(name, this);
            _nodes.Add(node);
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
            Console.WriteLine("REMOTE: Received" + e.Socket.ReceiveMultipartMessage().ToString());
        }
    }
}
