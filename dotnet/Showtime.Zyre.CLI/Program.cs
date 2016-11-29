using System;
using System.Collections.Generic;
using System.Linq;
using AsyncIO;
using Showtime;
using Showtime.Zyre;
using Showtime.Zyre.Plugs;
using Showtime.Zyre.Endpoints;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AsyncIO.ForceDotNet.Force();

            using (LocalEndpoint local = new LocalEndpoint(Guid.NewGuid().ToString(), (s) => { Console.WriteLine("LOCAL: " + s); }))
            //using (LocalEndpoint remote = new LocalEndpoint("remote", (s) => { Console.WriteLine("REMOTE: " + s); }))
            {
                Node[] nodes = new Node[4];
                for (int i = 0; i < 4; i++)
                {
                    Endpoint endpoint = (i < 2) ? local : local;
                    Node node = endpoint.CreateNode("node" + i);
                    nodes[i] = node;

                    for(int j = 0; j < 1; j++)
                    {
                        node.CreateOutputPlug("out" + j);
                        node.CreateInputPlug("in" + j);
                    }

                    if (i > 0)
                    {
                        for (int j = 0; j < 1; j++)
                        {
                            Console.WriteLine("Connecting to " + nodes[i].Outputs[j].Path);
                            node.ConnectPlugs(nodes[i - 1].Outputs[j], nodes[i].Inputs[j]);
                        }
                    }
                }


                System.Threading.Thread.Sleep(1000);

                while (true)
                {
                    foreach(Node n in local.Nodes)
                    {
                        foreach(OutputPlug p in n.Outputs)
                        {
                            p.Update("Hello");
                        }
                    }
                    

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }


    }
}
