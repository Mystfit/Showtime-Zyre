using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Zyre;
using Newtonsoft.Json;
using Showtime.Zyre.Plugs;

namespace Showtime.Zyre.Endpoints
{
    public class RemoteEndpoint : Endpoint
    {
        public Endpoint Owner => _owner;
        private Endpoint _owner;

        public RemoteEndpoint(string name, Endpoint owner, Guid remoteUuid, Action<string> logger = null) : base(name, remoteUuid, logger)
        {
            _owner = owner;
        }

        public void RequestRemoteGraph(Guid remotepeer)
        {
            Owner.Log("Requesting remote graph from " + remotepeer);
            NetMQMessage replymsg = null;
            replymsg = new NetMQMessage(2);
            replymsg.Append(Endpoint.Commands.REQ_FULL_GRAPH.ToString());
            Whisper(remotepeer, replymsg);
        }

        public override bool IsPolling { get { return Owner.IsPolling; } }
        public override void CheckPolling()
        {
            Owner.CheckPolling();
        }

        public override void RegisterListenerNode(Node node)
        {
            _owner.RegisterListenerNode(node);
        }

        public override void DeregisterListenerNode(Node node)
        {
            _owner.DeregisterListenerNode(node);
        }

        public override void Whisper(Guid peer, NetMQMessage msg)
        {
            _owner.Whisper(peer, msg);
        }

        public override void PlugConnectionRequest(InputPlug input, OutputPlug output)
        {
            NetMQMessage msg = new NetMQMessage(3);
            msg.Append(Endpoint.Commands.PLUG_CONNECT.ToString());
            msg.Append(input.Path.ToString());
            msg.Append(output.Path.ToString());
            Whisper(Uuid, msg);
        }

        public override void SendMessageToOwner(NetMQMessage message)
        {
            message.Push(Endpoint.Commands.PLUG_MSG.ToString());  //Inject message type for whisper to seperate
            Whisper(Uuid, message);
        }

        public override void UpdateGraph(Node node, GraphUpdate.UpdateType type)
        {
            Log("In remote endpoint, we're not responsible for updating this part of the graph");
        }
    }
}
