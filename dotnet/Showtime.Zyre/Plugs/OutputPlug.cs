using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Showtime.Zyre.Plugs;
using Newtonsoft.Json;

namespace Showtime.Zyre.Plugs
{
    public class OutputPlug : Plug<PublisherSocket>
    {
        private OutputPlug() { }
        public OutputPlug(string name, Node owner) : base(name, owner)
        {
            Init();
        }

        public override void Init()
        {
            _path = new Address(Owner.Endpoint.Name, Owner.Name, Name);
            _socket = new PublisherSocket(String.Format("@inproc://{0}", Path.ToString()));
        }

        public virtual void Update(string value)
        {
            _currentValue = value;
            NetMQMessage msg = new Message(Path, value).ToNetMQMessage();
            Owner.Endpoint.Log("Sending update from " + Path.ToString());
            _socket.SendMultipartMessage(msg);
        }

        public override string ToString()
        {
            return "   O-> " + Name;
        }
    }
}
