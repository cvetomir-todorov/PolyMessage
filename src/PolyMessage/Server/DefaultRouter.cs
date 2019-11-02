using System;
using System.Collections.Generic;
using PolyMessage.Endpoints;

namespace PolyMessage.Server
{
    internal interface IRouter
    {
        Endpoint ChooseEndpoint(string message);
    }

    internal sealed class DefaultRouter : IRouter
    {
        private readonly List<Endpoint> _endpoints;

        public DefaultRouter(IReadOnlyCollection<Endpoint> endpoints)
        {
            if (endpoints.Count <= 0)
                throw new ArgumentException("There should be at least one endpoint.", nameof(endpoints));

            _endpoints = new List<Endpoint>(endpoints);
        }

        public Endpoint ChooseEndpoint(string message)
        {
            return _endpoints[0];
        }
    }
}
