using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using NetMQ;

namespace Showtime.Zyre
{
    public class Manager
    {
        private NetMQPoller _poller;
        private List<Node> _nodes;
        private Thread _pollthread;

        public Manager()
        {
            _nodes = new List<Node>();
            _poller = new NetMQPoller();
            _pollthread = new Thread(Run);
            _pollthread.Start();
        }

        private void Run()
        {
            _poller.Run();
        }

        public void Close()
        {
            _poller.Stop();
            _pollthread.Join();
            _poller.Dispose();
            NetMQConfig.Cleanup();
        }

        public void RegisterNode(Node node)
        {
            _nodes.Add(node);
            _poller.Add(node.InputSocket);
        }
    }
}
