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

        public PolyConnectionState State => _state;

        public Uri LocalAddress
        {
            get
            {
                EnsureNotInCreatedState();
                return _localAddress;
            }
        }

        public Uri RemoteAddress
        {
            get
            {
                EnsureNotInCreatedState();
                return _remoteAddress;
            }
        }

        internal void SetOpened(Uri localAddress, Uri remoteAddress)
        {
            _state = PolyConnectionState.Opened;
            _localAddress = localAddress;
            _remoteAddress = remoteAddress;
        }

        internal void SetClosed()
        {
            _state = PolyConnectionState.Closed;
        }
    }
}