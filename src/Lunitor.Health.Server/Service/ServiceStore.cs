using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace Lunitor.Health.Server.Service
{
    public class ServiceStore : IServiceStore
    {
        private IEnumerable<Shared.Service> _services;

        private readonly IConfiguration _configuration;

        public ServiceStore(IConfiguration configuration)
        {
            _configuration = configuration;
            LoadServicesFromAppSettings();
        }

        private void LoadServicesFromAppSettings()
        {
            var servicesSection = _configuration.GetSection("Services");

            if (servicesSection.Exists())
                _services = servicesSection.Get<List<Shared.Service>>();
            else
                _services = new List<Shared.Service>();
        }

        public IEnumerable<Shared.Service> GetAll()
        {
            if (_services == null)
                return new List<Shared.Service>();

            return _services;
        }
    }
}
