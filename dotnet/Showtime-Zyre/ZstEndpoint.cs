using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ.Zyre;

namespace Showtime_Zyre
{

    public class ZstEndpoint
    {
        private Zyre z;

        public delegate void IncomingListener(string message);
        public IncomingListener incoming;

        public ZstEndpoint(string name, Action<string> logger)
        {
            NetMQ.NetMQConfig.Linger = System.TimeSpan.FromSeconds(0);
            z = new Zyre(name, true, logger);
            z.Join("CHAT");
            z.ShoutEvent += Z_ShoutEvent;
            z.Start();
        }

        public void Close()
        {
            z.Dispose();
            NetMQ.NetMQConfig.Cleanup();
        }

        public void Send(string msg)
        {
            NetMQ.NetMQMessage m = new NetMQ.NetMQMessage(1);
            m.Append(msg);
            z.Shout("CHAT", m);
        }

        private void Z_ShoutEvent(object sender, NetMQ.Zyre.ZyreEvents.ZyreEventShout e)
        {
            //if (incoming != null)
            incoming(e.Content.ToString());
        }
    }
}
