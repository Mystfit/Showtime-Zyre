using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;

namespace Showtime.Zyre
{
    public class OutputPlug : Plug<PublisherSocket>
    {
        public OutputPlug(string name, Node owner) : base(name, owner)
        {
            _socket = new PublisherSocket(String.Format("@inproc://{0}", FullName));
            _targetaddress = _socket.Options.LastEndpoint;
        }

        public void Update(string value)
        {
            //if(String.Equals(_currentValue, value))
            //    return;
            _currentValue = value;
            _socket.SendMultipartMessage(new Message(FullName, value).ToNetMQMessage());
        }
    }
}
