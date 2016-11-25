using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using NetMQ;

namespace Showtime.Zyre
{
    public class InputPlug : Plug<SubscriberSocket>
    {
        private HashSet<string> _targets;

        public InputPlug(string name, Node owner) : base(name, owner)
        {
            _targets = new HashSet<string>();
            _socket = owner.InputSocket;
        }

        public void AddTarget(string target)
        {
            _targets.Add(target);
        }

        public void IncomingMessage(Message msg)
        {
            Console.WriteLine(string.Format("Plug {0} received value {1} from {2}", Name, msg.value, msg.fullPath));
        }
    }
}
