using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Zyre;
using Newtonsoft.Json;

namespace Showtime.Zyre.Endpoints
{
    public class RemoteEndpoint : Endpoint
    {
        public Endpoint Owner => _owner;
        private Endpoint _owner;

        public RemoteEndpoint(string name, Endpoint owner, Guid remoteUuid) : base(name, remoteUuid)
        {
            _owner = owner;
        }

        public void RequestRemoteGraph(Guid remotepeer)
        {
            NetMQMessage replymsg = null;
            replymsg = new NetMQMessage(2);
            replymsg.Append(Endpoint.Commands.REQ_FULL_GRAPH.ToString());
            replymsg.Append(_owner.Uuid.ToString());
            Whisper(remotepeer, replymsg);
        }

        public override void RegisterListenerNode(Node node)
        {
            _owner.RegisterListenerNode(node);
        }

        public override void DeregisterListenerNode(Node node)
        {
            _owner.DeregisterListenerNode(node);
        }

        public void SendToRemote(NetMQMessage msg)
        {
            Whisper(Uuid, msg);
        }

        public override void Whisper(Guid peer, NetMQMessage msg)
        {
            _owner.Whisper(peer, msg);
        }

        
    }
}
