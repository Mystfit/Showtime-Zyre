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
    public class Node : IDisposable
    {
        private Endpoint _endpoint;
        public Endpoint Endpoint {
            get { return _endpoint; }
            set {
                _endpoint = value;
                Init();
            }
        }

        [JsonProperty]
        public bool destroyed;

        private List<OutputPlug> _outputs = new List<OutputPlug>();
        private List<InputPlug> _inputs = new List<InputPlug>();

        [JsonProperty]
        public List<OutputPlug> Outputs { get { return _outputs; } }
        [JsonProperty]
        public List<InputPlug> Inputs { get { return _inputs; } }

        private Dictionary<string, List<InputPlug>> _connectedInputs = new Dictionary<string, List<InputPlug>>();
        public Dictionary<string, List<InputPlug>> ConnectedInputs { get { return _connectedInputs; } }

        private SubscriberSocket _input;
        public SubscriberSocket InputSocket { get { return _input; } }

        private string _name;
        [JsonProperty]
        public string Name {
            get { return _name; }
            private set { _name = value; }
        }

        private Node() { }
        public Node(string name, Endpoint endpoint = null)
        {
            _name = name;
            _endpoint = endpoint;
            Init();
        }

        public void Init() {
            _input = new SubscriberSocket();
            _input.ReceiveReady += IncomingMessage;
        }

        public InputPlug CreateInputPlug(InputPlug plug)
        {
            if(plug.Owner == null)
                plug.Owner = this;

            if (!Inputs.Any(p => p.Name == plug.Name)) {
                Inputs.Add(plug);
            } else
            {
                return Inputs.Find(p => p.Name == plug.Name);
            }

            return plug;
        }

        public InputPlug CreateInputPlug(string plugname)
        {
            if(Inputs.Any(p => p.Name == plugname))
                throw new ArgumentException("Input plug already exists!");

            Endpoint.Log(String.Format("Creating input {0} for {1}", plugname, _name));
            InputPlug input = new InputPlug(plugname, this);
            _inputs.Add(input);
            Endpoint.UpdateGraph(this, GraphUpdate.UpdateType.UPDATED);
            return input;
        }

        public OutputPlug CreateOutputPlug(OutputPlug plug)
        {
            if (plug.Owner == null)
                plug.Owner = this;

            if (!Outputs.Any(p => p.Name == plug.Name))
            {
                plug.Owner = this;
                Outputs.Add(plug);
            }
            else
            {
                return Outputs.Find(p => p.Path == plug.Path);
            }

            return plug;
        }

        public OutputPlug CreateOutputPlug(string plugname)
        {
            if (Outputs.Any(p => p.Name == plugname))
                throw new ArgumentException("Output plug already exists!");

            Endpoint.Log(String.Format("Creating output {0} for {1}", plugname, _name));
            OutputPlug output = new OutputPlug(plugname, this);
            _outputs.Add(output);
            Endpoint.UpdateGraph(this, GraphUpdate.UpdateType.UPDATED);
            return output;
        }

        public void RegisterListener(InputPlug input, OutputPlug output)
        {
            if (!ConnectedInputs.ContainsKey(output.Path.ToString()))
                ConnectedInputs.Add(output.Path.ToString(), new List<InputPlug>());

            if (!ConnectedInputs[output.Path.ToString()].Contains(input)) {
                ConnectedInputs[output.Path.ToString()].Add(input);
                Endpoint.RegisterListenerNode(this);
                Endpoint.CheckPolling();

                if (input.Owner.Endpoint != output.Owner.Endpoint)
                {
                    Endpoint.Log("In plug connection request");
                    Endpoint.PlugConnectionRequest(input, output);
                }
            } else
            {
                Endpoint.Log("Plug is already connected");
            }
        }


        private void IncomingMessage(object sender, NetMQ.NetMQSocketEventArgs e)
        {
            NetMQMessage msg = e.Socket.ReceiveMultipartMessage();
            Address address = Address.FromFullPath(msg[0].ConvertToString());

            if (_connectedInputs.ContainsKey(address.ToString()))
            {
                foreach (InputPlug plug in _connectedInputs[address.ToString()])
                {
                    Message localmessage = Message.FromNetMQMessage(msg);

                    if (localmessage.address.endpoint != Endpoint.Name && Endpoint.GetType() != typeof(LocalEndpoint))
                    {
                        Endpoint.Log("Received message intended for remote destination");
                        Endpoint.SendMessageToOwner(msg);
                    } else
                    {
                        plug.IncomingMessage(localmessage);
                    }
                }
            }
        }

        public void UpdateGraphPlugs(Plug plug)
        {
            Endpoint.UpdateGraph(this, GraphUpdate.UpdateType.UPDATED);
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

        public string Path { get { return string.Format("{0}/{1}", Endpoint, Name); } }

        public void ListPlugs()
        {
            ListInputs();
            ListOutputs(); 
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //Endpoint.RemoveNode(this);

                    destroyed = true;
                    foreach (OutputPlug plug in _outputs)
                        plug.Dispose();
                    _outputs.Clear();

                    foreach (InputPlug plug in _inputs)
                        plug.Socket.Dispose();
                    _inputs.Clear();
                    _input.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Node() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
