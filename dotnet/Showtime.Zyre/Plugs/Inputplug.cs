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
                Owner.Endpoint.Whisper(Owner.Endpoint.Uuid, msg.ToNetMQMessage());
            }
        }

        public void Connect(OutputPlug outplug)
        {
            if (!IsListeningTo(outplug))
            {
                Socket.Subscribe(outplug.Path.ToString());
                Owner.Endpoint.Log("Subscribing to " + outplug.Path.ToString());

                Socket.Connect(outplug.Path.ToEndpoint());
                Owner.Endpoint.Log("Connecting to " + outplug.Path.ToEndpoint());

                Owner.Endpoint.RegisterListenerNode(Owner);
                Owner.Endpoint.CheckPolling();

                Owner.RegisterListener(this, outplug);
            }
        }

        public override string ToString()
        {
            return "   I<- " + Name;
        }

        public bool IsListeningTo(OutputPlug plug)
        {
            return Owner.ConnectedInputs.ContainsKey(plug.Path.ToString());
        }
    }
}
