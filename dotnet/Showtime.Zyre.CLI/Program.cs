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

            using (LocalEndpoint local = new LocalEndpoint("local", (s) => { Console.WriteLine("LOCAL: " + s); }))
            using (LocalEndpoint remote = new LocalEndpoint("remote", (s) => { Console.WriteLine("REMOTE: " + s); }))
            {
                Node[] nodes = new Node[4];
                for (int i = 0; i < 4; i++)
                {
                    Endpoint endpoint = (i < 2) ? local : remote;
                    Node node = endpoint.CreateNode("node" + i);
                    nodes[i] = node;

                    OutputPlug output = node.CreateOutputPlug("out" + i);
                    InputPlug input = node.CreateInputPlug("in" + i);

                    if (i > 0)
                    {
                        Console.WriteLine("Connecting to " + output.Address);
                        node.ConnectPlugs(nodes[i-1].Outputs[0], input);
                    }
                }


                System.Threading.Thread.Sleep(1000);

                while (true)
                {
                    for (int i = 0; i < 3; i++)
                        nodes[i].Outputs[0].Update("hello");

                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }


    }
}
