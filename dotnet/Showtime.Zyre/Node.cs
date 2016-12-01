using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Showtime.Zyre.Endpoints;
using Showtime.Zyre.Plugs;

namespace Showtime.Zyre
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Node
    {
        private Endpoint _endpoint;
        public Endpoint Endpoint {
            get { return _endpoint; }
            set {
                _endpoint = value;
                Init();
            }
        }

        private List<OutputPlug> _outputs = new List<OutputPlug>();
        private List<InputPlug> _inputs = new List<InputPlug>();

        [JsonProperty]
        public List<OutputPlug> Outputs { get { return _outputs; } }
        [JsonProperty]
        public List<InputPlug> Inputs { get { return _inputs; } }

        private Dictionary<string, List<InputPlug>> _connectedInputs = new Dictionary<string, List<InputPlug>>();

        private SubscriberSocket _input;
        public SubscriberSocket InputSocket { get { return _input; } }

        private string _name;
        [JsonProperty]
        public string Name {
            get { return _name; }
            private set { _name = value; }
        }

        private Node(){}
        public Node(string name, Endpoint endpoint = null)
        {
            _name = name;
            _endpoint = endpoint;
            Init();
        }

        public void Init() {
            _input = new SubscriberSocket();
            _input.ReceiveReady += IncomingMessage;
            Endpoint.RegisterListenerNode(this);
        }

        public void Close()
        {
            _input.Dispose();
            foreach (OutputPlug plug in _outputs)
                plug.Socket.Dispose();
        }

        public InputPlug CreateInputPlug(string plugname)
        {
            Endpoint.Log(String.Format("Creating input {0} for {1}", plugname, _name));
            InputPlug input = new InputPlug(plugname, this);
            _inputs.Add(input);
            return input;
        }

        public OutputPlug CreateOutputPlug(string plugname)
        {
            Endpoint.Log(String.Format("Creating output {0} for {1}", plugname, _name));
            OutputPlug output = new OutputPlug(plugname, this);
            _outputs.Add(output);
            return output;
        }

        public void ConnectPlugs(OutputPlug output, InputPlug input)
        {
            input.Socket.Connect(output.Path.ToEndpoint());
            Endpoint.Log("Connecting to " + output.Path.ToEndpoint());

            input.Socket.Subscribe(output.Path.ToString());
            //input.Socket.SubscribeToAnyTopic();
            Endpoint.Log("Subscribing to " + output.Path.ToString());

            if (!_connectedInputs.ContainsKey(output.Path.ToString()))
                _connectedInputs.Add(output.Path.ToString(), new List<InputPlug>());

            _connectedInputs[output.Path.ToString()].Add(input);
        }

        private void IncomingMessage(object sender, NetMQ.NetMQSocketEventArgs e)
        {
            NetMQMessage msg = e.Socket.ReceiveMultipartMessage();
            Address address = Address.FromFullPath(msg[0].ConvertToString());

            if (_connectedInputs.ContainsKey(address.ToString()))
            {
                foreach (InputPlug plug in _connectedInputs[address.ToString()])
                {
                    plug.IncomingMessage(Message.FromNetMQMessage(msg));
                }
            }       
        }

        public void ListInputs()
        {
            Endpoint.Log("--------------------------");
            Endpoint.Log("Inputs:");
            Endpoint.Log("--------------------------");

            for (int i = 0; i < Inputs.Count; i++)
            {
                Endpoint.Log(i + ": " + Inputs[i].ToString());
            }
        }

        public void ListOutputs()
        {
            Endpoint.Log("\n--------------------------");
            Endpoint.Log("Outputs:");
            Endpoint.Log("--------------------------");
            for (int i = 0; i < Outputs.Count; i++)
            {
                Endpoint.Log(i + ": " + Outputs[i].ToString());
            }
        }

        public void ListPlugs()
        {
            ListInputs();
            ListOutputs(); 
        }
    }
}
