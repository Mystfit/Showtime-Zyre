using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using Showtime.Zyre.Endpoints;
using MsgPack;

namespace Showtime.Zyre
{
    public class Message
    {
        public Address address;
        public string value;

        public Message(Address address, string val = "")
        {
            this.address = address;
            value = val;
        }

        public Message(string path="", string val="")
        {
            address = Address.FromFullPath(path);
            value = val;
        }

        public NetMQMessage ToNetMQMessage()
        {
            NetMQMessage msg = new NetMQMessage();
            msg.Append(address.ToString()); //Origin
            msg.Append(value);              //Message contents
            return msg;
        }

        public static Message FromZyreWhisper(NetMQMessage msg)
        {
            string addressfull = msg[1].ConvertToString();
            Message m = new Message(addressfull, msg[2].ConvertToString());
            return m;
        }

        public static Message FromNetMQMessage(NetMQMessage msg)
        {
            string addressfull = msg[0].ConvertToString();
            Message m = new Message(addressfull, msg[1].ConvertToString());
            return m;
        }

        public override string ToString()
        {
            return String.Format("Address:{0}, Value:{1}", address.ToString(), value);
        }
    }
}
