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
        public Endpoint GetEndpoint { get { return _endpoint; } }

        private List<OutputPlug> _outputs;
        private List<InputPlug> _inputs;        
        
        [JsonProperty]
        public List<OutputPlug> Outputs { get { return _outputs; } }
        [JsonProperty]
        public List<InputPlug> Inputs { get { return _inputs; } }

        private Dictionary<string, List<InputPlug>> _connectedInputs;

        private SubscriberSocket _input;
        public SubscriberSocket InputSocket { get { return _input; } }

        private string _name;
        [JsonProperty]
        public string Name { get { return _name; } }

        public Node(string name, Endpoint endpoint = null)
        {
            _name = name;
            _inputs = new List<InputPlug>();
            _outputs = new List<OutputPlug>();
            _input = new SubscriberSocket();
            _input.ReceiveReady += IncomingMessage;
            _endpoint = endpoint;
            _connectedInputs = new Dictionary<string, List<InputPlug>>();
        }

        public void Close()
        {
            _input.Dispose();
            foreach (OutputPlug plug in _outputs)
                plug.Socket.Dispose();
        }

        public InputPlug CreateInputPlug(string plugname)
        {
            Console.WriteLine(String.Format("Creating input {0} for {1}", plugname, _name));
            InputPlug input = new InputPlug(plugname, this);
            _inputs.Add(input);
            return input;
        }

        public OutputPlug CreateOutputPlug(string plugname)
        {
            Console.WriteLine(String.Format("Creating output {0} for {1}", plugname, _name));
            OutputPlug output = new OutputPlug(plugname, this);
            _outputs.Add(output);
            return output;
        }

        public void ConnectPlugs(OutputPlug output, InputPlug input)
        {
            input.AddTarget(output.FullName);
            input.Socket.Connect(output.Address);
            input.Socket.Subscribe(output.FullName);
            Console.WriteLine("Subscribing to " + output.FullName);

            if (!_connectedInputs.ContainsKey(output.FullName))
                _connectedInputs.Add(output.FullName, new List<InputPlug>());

            _connectedInputs[output.FullName].Add(input);
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
    }
}
