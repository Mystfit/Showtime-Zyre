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
            int numnodes = 4;
#if NET35
            AsyncIO.ForceDotNet.Force();
#endif

            string guid = Guid.NewGuid().ToString();
            string endpointid = guid.Substring(Math.Max(0, guid.Length - 4));

            using (LocalEndpoint local = new LocalEndpoint(endpointid, (s) => { Console.WriteLine("LOCAL: " + s); }))
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

                Console.Clear();

                LinkNodesInChain(local);
                TestNodeLink(local);

                Console.WriteLine("Connect to local or remote? (l/r)");
                Endpoint targetEndpoint = null;
                while (targetEndpoint == null)
                {
                    char key = Console.ReadKey().KeyChar;
                    Console.WriteLine("");
                    if (key == 'l')
                    {
                        targetEndpoint = local;

                        Console.WriteLine("Test node chain?");
                        if ((Console.ReadKey().KeyChar == 'y') ? true : false)
                        {
                            LinkNodesInChain(targetEndpoint);
                            TestNodeLink(targetEndpoint);
                        }
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
                        Console.WriteLine(string.Format("Didn't understand input \'{0}\'. Please enter l or r", key));
                    }
                }

                //Setup message test
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

                do
                {
                    Console.WriteLine("\nEnter message:");
                    output.Update(Console.ReadLine());
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape);


                Console.WriteLine("Testing local output removal");
                output.Dispose();

                Console.WriteLine("\nRemaining outputs:");
                outnode.ListOutputs();

                System.Threading.Thread.Sleep(1000);


                Console.WriteLine("Testing local node removal");
                outnode.Dispose();

                Console.WriteLine("\nRemaining nodes:");
                local.ListNodes();

                System.Threading.Thread.Sleep(1000);

                //local.Dispose();

            }
        }

        public static void LinkNodesInChain(Endpoint endpoint)
        {
            int i = 0;
            foreach(Node n in endpoint.Nodes)
            {
                if (i > 0)
                {
                    n.Inputs[0].Connect(endpoint.Nodes[i - 1].Outputs[0]);
                    Console.WriteLine("Connecting to " + endpoint.Nodes[i - 1].Outputs[0].Path);
                }
                i++;
            }  
            
        }

        public static void TestNodeLink(Endpoint endpoint)
        {
            
            foreach (Node n in endpoint.Nodes)
            {
                foreach (OutputPlug p in n.Outputs)
                {
                    p.Update("hi there");
                }
            }            
        }
    }
}
