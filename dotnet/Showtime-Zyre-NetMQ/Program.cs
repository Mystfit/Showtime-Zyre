using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Showtime_Zyre;

namespace Showtime_Zyre_NetMQ
{
    class Program
    {
        private static ZstEndpoint z;
        static void Main(string[] args)
        {
            z = new ZstEndpoint("csharp_node", (s)=> { Console.WriteLine(s+"\n"); } );

            z.incoming += (s) => { Console.WriteLine("Recieved: " + s); };

            System.Threading.Thread.Sleep(500);
            z.Send("Hi from csharp");

        }
    }
}
