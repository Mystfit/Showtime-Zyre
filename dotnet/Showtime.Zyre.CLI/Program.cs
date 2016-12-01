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
            int numnodes = 2;

            //AsyncIO.ForceDotNet.Force();

            string guid = Guid.NewGuid().ToString();
            string endpointid = guid.Substring(Math.Max(0, guid.Length - 4));

            using (LocalEndpoint local = new LocalEndpoint(endpointid, (s) => { Console.WriteLine("LOCAL: " + s); }))
            //using (LocalEndpoint remote = new LocalEndpoint("remote", (s) => { Console.WriteLine("REMOTE: " + s); }))
            {

                Node[] nodes = new Node[numnodes];
                for (int i = 0; i < numnodes; i++)
                {
                    Endpoint endpoint = (i < 2) ? local : local;

                    string id = Guid.NewGuid().ToString();
                    Node node = endpoint.CreateNode("node-" + id.Substring(Math.Max(0, id.Length - 4)));
                    nodes[i] = node;

                    for(int j = 0; j < 1; j++)
                    {
                        node.CreateOutputPlug("out" + j);
                        node.CreateInputPlug("in" + j);
                    }
                }

                LinkNodesInChain(local);
                TestNodeLink(local);


                System.Threading.Thread.Sleep(1000);
                Console.Clear();

                


                Console.WriteLine("Connect to local or remote? (l/r)");


                Endpoint targetEndpoint = null;

                while (targetEndpoint == null)
                {
                    char key = Console.ReadKey().KeyChar;
                    Console.WriteLine("");
                    if (key == 'l')
                    {
                        targetEndpoint = local;

                    }
                    else if (key == 'r')
                    {
                        local.ListNodes();
                        Console.WriteLine("Waiting for remote nodes...");

                        while (local.RemoteEndpoints.Values.Count == 0)
                        {
                            System.Threading.Thread.Sleep(100);
                        }

                        foreach (Endpoint remoteEndpoint in local.RemoteEndpoints.Values)
                        {
                            targetEndpoint = remoteEndpoint;
                            break;
                        }
                    }
                    else
                    {
                        Console.WriteLine(string.Format("\nDidn't understand input \'{0}\'. Please enter l or r", key));
                    }
                }


                bool testChain = false;
                Console.WriteLine("Test node chain?");
                if((Console.ReadKey().KeyChar == 'y') ? true : false)
                {
                    LinkNodesInChain(targetEndpoint);
                    TestNodeLink(targetEndpoint);
                }

                local.ListNodes();
                Console.WriteLine("\nEnter index of source node");
                Node outnode = local.Nodes[int.Parse(Console.ReadLine())];

                outnode.ListOutputs();
                Console.WriteLine("\nEnter index of source plug");
                OutputPlug output = outnode.Outputs[int.Parse(Console.ReadLine())];

                targetEndpoint.ListNodes();
                Console.WriteLine("\nEnter index of destination node");
                Node innode = targetEndpoint.Nodes[int.Parse(Console.ReadLine())];

                innode.ListInputs();
                Console.WriteLine("\nEnter index of destination plug");
                InputPlug input = innode.Inputs[int.Parse(Console.ReadLine())];

                Console.WriteLine(string.Format("About to connect {0} -> {1}", output.Path, input.Path));

                input.Connect(output);
                System.Threading.Thread.Sleep(1000);
                output.Update("Message to fake remote plug");
                
                while (true)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;

                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        public static void LinkNodesInChain(Endpoint endpoint)
        {
            int i = 0;
            foreach(Node n in endpoint.Nodes)
            {
                if (i > 0)
                {
                    n.ConnectPlugs(endpoint.Nodes[i - 1].Outputs[0], n.Inputs[0]);
                    Console.WriteLine("Connecting to " + endpoint.Nodes[i - 1].Outputs[0].Path);
                }
                i++;
            }  
            
        }

        public static void TestNodeLink(Endpoint endpoint)
        {
            while (true)
            {
                foreach (Node n in endpoint.Nodes)
                {
                    foreach (OutputPlug p in n.Outputs)
                    {
                        p.Update("hi there");
                    }
                }
                System.Threading.Thread.Sleep(1000);
            }
        }

    }
}
