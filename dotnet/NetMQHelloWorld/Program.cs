using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;

namespace NetMQHelloWorld
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AsyncIO.ForceDotNet.Force();
            using (var rep = new ResponseSocket("@inproc://rep"))

            using (var req = new RequestSocket(">inproc://rep"))

            using (var poller = new NetMQPoller { rep })

            {

                // this event will be raised by the Poller

                rep.ReceiveReady += (s, a) =>

                {

                    bool more;

                    string messageIn = a.Socket.ReceiveFrameString(out more);

                    Console.WriteLine("messageIn = {0}", messageIn);

                    a.Socket.SendFrame("World");



                    // REMOVE THE SOCKET!

                    poller.Remove(a.Socket);

                };



                // start the poller

                poller.RunAsync();



                // send a request

                req.SendFrame("Hello");



                bool more2;

                string messageBack = req.ReceiveFrameString(out more2);

                Console.WriteLine("messageBack = {0}", messageBack);



                // SEND ANOTHER MESSAGE

                req.SendFrame("Hello Again");



                // give the message a chance to be processed (though it won't be)

                System.Threading.Thread.Sleep(1000);

            }
        }
    }
}
