using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ.Zyre;
using Newtonsoft.Json;

namespace Showtime.Zyre.Endpoints
{
    public class RemoteEndpoint : Endpoint
    {

        public string RemoteUUID { get { return _remoteUUID; } }
        private string _remoteUUID;

        public RemoteEndpoint(string name, string remoteUUID, Action<string> logger=null) : base(name, logger)
        {
            _remoteUUID = remoteUUID;
        }



        public void SyncNodes(NetMQ.Zyre.Zyre z)
        {
            
            //Create local nodes to represent remote nodes
            string remoteName = "fake";

            //foreach(string n in )
            //Node remoteNode = CreateNode(remoteName);



        } 

    }
}
