using System;

namespace PolyMessage
{
    public enum PolyConnectionState
    {
        Created, Opened, Closed
    }

    public class PolyConnection
    {
        private PolyConnectionState _state;
        private Uri _localAddress;
        private Uri _remoteAddress;

        internal PolyConnection()
        {
            _state = PolyConnectionState.Created;
        }

        private void EnsureNotInCreatedState()
        {
            if (State == PolyConnectionState.Created)
                throw new InvalidOperationException("Connection needs to be opened in order to have addresses.");
        }

        public PolyConnectionState State
        {
            get => _state;
            set => _state = value;
        }

        public Uri LocalAddress
        {
            get
            {
                EnsureNotInCreatedState();
                return _localAddress;
            }
            protected set
            {
                if (value == null)
                    throw new ArgumentNullException();
                _localAddress = value;
            }
        }

        public Uri RemoteAddress
        {
            get
            {
                EnsureNotInCreatedState();
                return _remoteAddress;
            }
            protected set
            {
                if (value == null)
                    throw new ArgumentNullException();
                _remoteAddress = value;
            }
        }
    }

    public class PolyMutableConnection : PolyConnection
    {
        public void SetOpened(Uri localAddress, Uri remoteAddress)
        {
            State = PolyConnectionState.Opened;
            LocalAddress = localAddress;
            RemoteAddress = remoteAddress;
        }

        public void SetClosed()
        {
            State = PolyConnectionState.Closed;
        }
    }
}