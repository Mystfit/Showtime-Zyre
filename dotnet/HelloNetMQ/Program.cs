using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ;
using NetMQ.Sockets;

namespace HelloNetMQ
{
    class Node
    {
        public PublisherSocket pub;
        public SubscriberSocket sub;
        public SubscriberSocket sub2;

        public NetMQPoller poller;


        public Node()
        {
            pub = new PublisherSocket("@inproc://publisher");
            sub = new SubscriberSocket();
            sub.Connect("inproc://publisher");
            sub.SubscribeToAnyTopic();
            sub.ReceiveReady += (s, a) =>
            {
                Console.WriteLine("First: " + a.Socket.ReceiveMultipartMessage()[0].ConvertToString());
            };

            sub2 = new SubscriberSocket();

            poller = new NetMQPoller();

            poller.Add(sub);
            poller.RunAsync();
        }

        public void Loop()
        {
            bool connected = false;

            //Publish message
            NetMQMessage msg = new NetMQMessage(1);
            msg.Append("32abd/node0974/out0");
            pub.SendMultipartMessage(msg);

            //At the end of the first run, create another sub to test late joiners
            if (!connected)
            {
                var sub2 = new SubscriberSocket();
                sub2.Connect("inproc://publisher");
                poller.Add(sub2);
                sub2.ReceiveReady += (s, a) =>
                {
                    Console.WriteLine("second: " + a.Socket.ReceiveMultipartMessage()[0].ConvertToString());
                };
                sub2.Subscribe("32abd/node0974/out0");
                connected = true;
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Node n = new Node();

            
            while (true)
            {
                n.Loop();
                System.Threading.Thread.Sleep(1000);
            }
            
        }
    }
}
