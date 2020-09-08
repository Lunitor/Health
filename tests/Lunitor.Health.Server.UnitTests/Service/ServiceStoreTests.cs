using Lunitor.Health.Server.Service;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Lunitor.Health.Server.UnitTests.Service
{
    public class ServiceStoreTests
    {
        private ServiceStore _serviceStore;
        private IConfiguration _configuration;

        private const string ServicesConfiguration = @"
        {
            ""Services"": [
                {
                    ""Name"": ""Spejz"",
                    ""LocalUrl"": ""http://localhost:8096/"",
                    ""LocalNetworkUrl"": ""http://testlocal:8096"",
                    ""PublicNetworkUrl"": ""https://public.network.net""
                },
                {
                    ""Name"": ""Lunitor"",
                    ""LocalUrl"": ""http://localhost:4444/"",
                    ""LocalNetworkUrl"": ""http://testlocal:4444"",
                    ""PublicNetworkUrl"": """"
                }
            ]
        }";
        private const string EmptyConfiguration = "{}";
        private const string EmptyServicesConfiguration = @"
        {
            ""Services"" : []
        }";


        public ServiceStoreTests()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(ServicesConfiguration)));
            _configuration = configurationBuilder.Build();

            _serviceStore = new ServiceStore(_configuration);
        }

        [Fact]
        public void LoadServicesFromAppSettingsShouldLoadEmptyListIfThereIsNoServiceSectionInConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(EmptyConfiguration)));
            _configuration = configurationBuilder.Build();

            var serviceStore = new ServiceStore(_configuration);
            var services = serviceStore.GetAll();

            Assert.NotNull(services);
            Assert.Empty(services);
        }

        [Fact]
        public void LoadServicesFromAppSettingsShouldLoadEmptyListIfServicesSectionIsEmptyInConfiguration()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonStream(new MemoryStream(Encoding.ASCII.GetBytes(EmptyServicesConfiguration)));
            _configuration = configurationBuilder.Build();

            var serviceStore = new ServiceStore(_configuration);

            var services = serviceStore.GetAll();

            Assert.NotNull(services);
            Assert.Empty(services);
        }

        [Fact]
        public void LoadServicesFromAppSettingsShouldLoadAllServicesFromConfiguration()
        {
            var services = _serviceStore.GetAll();

            Assert.NotNull(services);
            Assert.NotEmpty(services);
            Assert.Equal(2, services.Count());
        }
    }
}
