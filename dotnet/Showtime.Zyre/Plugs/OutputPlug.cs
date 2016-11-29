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
            _socket.SendMultipartMessage(new Message(Path, value).ToNetMQMessage());
        }
    }
}
