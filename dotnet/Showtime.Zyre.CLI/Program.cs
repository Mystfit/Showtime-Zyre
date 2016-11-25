using System;
using System.Collections.Generic;
using System.Linq;
using AsyncIO;
using Showtime;
using Showtime.Zyre;

namespace ConsoleApp1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AsyncIO.ForceDotNet.Force();


            Endpoint endpoint = new Endpoint("csharp_endpoint", (s) => { Console.WriteLine("REMOTE: " + s + "\n"); });

            
            Node node1 = endpoint.CreateNode("node1");
            Node node2 = endpoint.CreateNode("node2");

            //Pair1
            OutputPlug output1 = node1.CreateOutputPlug("out1");
            InputPlug input1 = node1.CreateInputPlug("in1");

            //Pair2
            OutputPlug output2 = node2.CreateOutputPlug("out2");
            InputPlug input2 = node2.CreateInputPlug("in2");

            Console.WriteLine("Connecting to " + output1.Address);
            node2.Connect(output1, input2);
            node1.Connect(output2, input1);

            System.Threading.Thread.Sleep(1000);
            
            while (true)
            {
                output1.Update("hello");
                output2.Update("hi");
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) break;
                System.Threading.Thread.Sleep(1000);
            }
            Console.WriteLine("Closing...");
            endpoint.Close();

            Console.WriteLine("Done");
        }
    }
}
