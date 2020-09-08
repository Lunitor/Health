using Lunitor.Health.Client;
using Lunitor.Health.Shared;
using System;
using Xunit;

namespace Lunitor.Health.Server.UnitTests
{
    public class ServiceCheckResultTests
    {
        private readonly Service TestService = new Service
        {
            Name = "TestService",
            LocalUrl = "http://localhost:1234",
            LocalNetworkUrl = "http://testlocalnetwork:1234",
            PublicNetworkUrl = "http://test.public.net"
        };

        [Fact]
        public void ErrorsShouldBeInitializedToEmptyIDictionaryImplementation()
        {
            var result = new ServiceCheckResult();

            Assert.NotNull(result.Errors);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void EndpointHealthyShouldThrowArgumentExceptionWhenNullEmptyWhitespaceUrlPassed(string url)
        {
            var result = new ServiceCheckResult();

            Assert.ThrowsAny<ArgumentException>(() => result.EndpointHealthy(url));
        }

        [Fact]
        public void EndpointHealthyShouldThrowArgumentExceptionWhenPassedUrlIsNotInTheServicesUrls()
        {
            var result = new ServiceCheckResult()
            {
                 Service = TestService
            };

            Assert.ThrowsAny<ArgumentException>(() => result.EndpointHealthy("http://localhost:9999"));
        }

        [Fact]
        public void EndpointHealthyShouldReturnFalseIfThereIsAtLeastOneErrorWithGivenUrlInErrors()
        {
            var testUrl = TestService.PublicNetworkUrl;

            var result = new ServiceCheckResult()
            {
                Service = TestService
            };
            result.Errors.Add(testUrl, "some error");

            Assert.False(result.EndpointHealthy(testUrl));
        }

        [Fact]
        public void EndpointHealthyShouldReturnTrueIfThereIsNoErrorWithGivenUrlInErrors()
        {
            var result = new ServiceCheckResult()
            {
                Service = TestService
            };

            Assert.True(result.EndpointHealthy(TestService.PublicNetworkUrl));
        }
    }
}
