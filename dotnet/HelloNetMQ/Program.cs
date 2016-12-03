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

        public bool connected = false;


        public Node()
        {
            poller = new NetMQPoller();

            //Create node input
            sub = new SubscriberSocket();

            sub.ReceiveReady += (s, a) =>
            {
                Console.WriteLine("First: " + a.Socket.ReceiveMultipartMessage()[0].ConvertToString());
            };
            poller.Add(sub);
            poller.RunAsync();

            //Create plug output
            pub = new PublisherSocket("@inproc://publisher");
            //Connect plug out to node in

            sub.Connect("inproc://publisher");
            sub.Subscribe("32abd/node0974/out0");

            //Start poller
        }

        public void Loop()
        {

            //Publish message
            NetMQMessage msg = new NetMQMessage(1);
            msg.Append("32abd/node0974/out0");
            pub.SendMultipartMessage(msg);

            //At the end of the first run, create another sub to test late joiners
            if (!connected)
            {
                sub2 = new SubscriberSocket();
                sub2.Connect("inproc://publisher");
                poller.Add(sub2);
                sub2.ReceiveReady += (s, a) =>
                {
                    Console.WriteLine("Second: " + a.Socket.ReceiveMultipartMessage()[0].ConvertToString());
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
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
                n.Loop();
                System.Threading.Thread.Sleep(1000);
            }
            n.poller.StopAsync();
            n.poller.Dispose();
            n.sub.Dispose();
            n.pub.Dispose();
            n.sub2.Dispose();
            NetMQ.NetMQConfig.Cleanup();
        }
    }
}
