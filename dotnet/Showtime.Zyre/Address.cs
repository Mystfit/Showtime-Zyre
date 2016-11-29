using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Showtime.Zyre
{
    [JsonObject]
    public class Address
    {
        public string endpoint;
        public string node;
        public string originPlug;

        [JsonConstructor]
        private Address()
        {
        }

        public Address(string endpoint, string node, string originplug)
        {
            this.endpoint = endpoint;
            this.node = node;
            this.originPlug = originplug;
        }

        public static Address FromFullPath(string fullpath)
        {
            string[] address = fullpath.Split('/');
            return new Address(address[0], address[1], address[2]);
        }

        public override string ToString(){ return string.Format("{0}/{1}/{2}", endpoint, node, originPlug); }
        public string ToEndpoint() { return string.Format("inproc://{0}", this.ToString()); }
    }
}
