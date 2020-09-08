using Lunitor.Health.Shared;
using System.Collections.Generic;

namespace Lunitor.Health.Server.UnitTests
{
    public static class TestDataProvider
    {
        public static readonly Shared.Service TestService = new Shared.Service
        {
            Name = "test",
            LocalUrl = "http://localhost:1234",
            LocalNetworkUrl = "http://test:1234",
            PublicNetworkUrl = "http://public.test.net",
        };
        public static readonly List<ServiceCheckResultDto> TestServiceErrors = new List<ServiceCheckResultDto>{
                new ServiceCheckResultDto
                {
                    Errors = new Dictionary<string, string>
                        {
                                        {TestService.LocalNetworkUrl, "timeout"},
                                        {TestService.PublicNetworkUrl, "404"}
                        },
                    Service = TestService
                }};
    }
}
