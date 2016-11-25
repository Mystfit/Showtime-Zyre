using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;

namespace Showtime.Zyre
{
    public class Message
    {
        public string endpoint;
        public string node;
        public string originPlug;
        public string fullPath;
        public string value;

        public Message(string path="", string val="")
        {
            fullPath = path;
            string[] address = fullPath.Split('/');

            endpoint = address[0];
            node = address[1];
            originPlug = address[2];

            value = val;
        }

        public NetMQMessage ToNetMQMessage()
        {
            NetMQMessage msg = new NetMQMessage();
            msg.Append(String.Format("{0}/{1}/{2}",endpoint, node, originPlug));
            msg.Append(value);
            return msg;
        }

        public static Message FromNetMQMessage(NetMQMessage msg)
        {
            string addressfull = msg[0].ConvertToString();
            Message m = new Message(addressfull, msg[1].ConvertToString());
            return m;
        }


        public override string ToString()
        {
            return String.Format("Endpoint:{0}, Node:{1}, OriginPlug:{2}, Value:{3}", endpoint, node, originPlug, value);
        }


    }
}
