using Ardalis.GuardClauses;
using Lunitor.Health.Shared;
using System;

namespace Lunitor.Health.Client
{
    public class ServiceCheckResult : ServiceCheckResultDto
    {
        public bool Healthy => Errors.Count == 0;

        public bool EndpointHealthy(string url)
        {
            Guard.Against.NullOrWhiteSpace(url, nameof(url));
            if (NotAnEndpointOfService(url))
                throw new ArgumentException($"{url} is not an endpoint of {Service.Name}");

            return !Errors.ContainsKey(url);
        }

        private bool NotAnEndpointOfService(string url)
        {
            return Service.LocalUrl != url && Service.LocalNetworkUrl != url && Service.PublicNetworkUrl != url;
        }
    }
}
