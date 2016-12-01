using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using NetMQ;
using Newtonsoft.Json;

namespace Showtime.Zyre.Plugs
{
    public class InputPlug : Plug<SubscriberSocket>
    {
        private InputPlug() { }
        public InputPlug(string name, Node owner) : base(name, owner)
        {
            Init();
        }

        public override void Init()
        {
            _path = new Address(Owner.Endpoint.Name, Owner.Name, Name);
            _socket = Owner.InputSocket;
        }

        public void IncomingMessage(Message msg)
        {
            Owner.Endpoint.Log(string.Format("Plug {0} received value {1} from {2}", Path.ToString(), msg.value, msg.address));

            if (msg.address.endpoint != Owner.Endpoint.Name)
            {
                Owner.Endpoint.Log("Received message intended for remote destination");
            }
        }

        public void Connect(OutputPlug outplug)
        {
            Owner.ConnectPlugs(outplug, this);
        }

        public override string ToString()
        {
            return "   I<- " + Name;
        }
    }
}
