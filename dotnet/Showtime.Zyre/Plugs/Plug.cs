using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace Showtime.Zyre.Plugs
{
    public abstract class Plug : IDisposable
    {
        protected Plug() { }
        public Plug(string name, Node owner)
        {
            _owner = owner;
            _name = name;
        }

        public abstract void Init();

        private string _name;
        public string Name
        {
            get { return _name; }
            private set { _name = value; }
        }

        private Node _owner;
        [JsonIgnore]
        public Node Owner
        {
            get { return _owner; }
            set
            {
                _owner = value;
                Init();
            }
        }

        protected string _currentValue;
        public string CurrentValue
        {
            get { return _currentValue; }
            private set { _currentValue = value; }
        }

        private bool _dirty;
        public void SetDirty() { _dirty = true; }
        public void SetClean() { _dirty = false; }
        public bool IsDirty { get { return _dirty; } }

        public Address Path { get { return _path; } }
        protected Address _path;

        public bool destroyed;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    destroyed = true;
                    Owner.UpdateGraphPlugs(this);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Plug() {
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

    [JsonObject(MemberSerialization.OptOut)]
    public abstract class Plug<T> : Plug where T : NetMQSocket
    {
        protected T _socket;
        private string name;

        protected Plug() { }
        public Plug(string name, Node owner) : base(name, owner)
        {
        }

        [JsonIgnore]
        public T Socket { get { return _socket; } }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _socket.Dispose();
        }
    }
}
