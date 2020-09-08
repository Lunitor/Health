using Lunitor.Health.Server.Service;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lunitor.Health.Server.UnitTests.Service
{
    public class ServiceCheckerTests
    {
        private ServiceChecker _serviceChecker;
        private Mock<HttpMessageHandler> _messageHandlerMock;

        private readonly Shared.Service TestService = new Shared.Service
        {
            Name = "TestService",
            LocalUrl = "http://localhost:1234",
            LocalNetworkUrl = "http://testlocal:1234",
            PublicNetworkUrl = "http://test.public.net"
        };

        public ServiceCheckerTests()
        {
            _messageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            var httpClient = new HttpClient(_messageHandlerMock.Object);
            _serviceChecker = new ServiceChecker(httpClient);
        }

        [Theory]
        [InlineData(true, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        public async Task CheckServiceAsyncShouldReturnheckResultWithErrorWhenAtLeastOneResponseStatusCodeNotOK(bool localResponse, bool localNetworkResponse, bool publicNetworkResponse)
        {
            _messageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    var requestUrl = request.RequestUri.ToString();

                    if ((requestUrl == TestService.LocalUrl && localResponse) ||
                        (requestUrl == TestService.LocalNetworkUrl && localNetworkResponse) ||
                        (requestUrl == TestService.PublicNetworkUrl && publicNetworkResponse))
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    else
                        return new HttpResponseMessage() { StatusCode = HttpStatusCode.NotFound };
                })
                .Verifiable();

            var result = await _serviceChecker.CheckServiceAsync(TestService);

            Assert.NotEmpty(result.Errors);
        }

        [Theory]
        [InlineData(true, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, true)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(false, false, false)]
        public async Task CheckServiceAsyncShouldReturnCheckResultWithErrorWhenAtLeastOneResponseWasntReceived(bool localResponse, bool localNetworkResponse, bool publicNetworkResponse)
        {
            _messageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                      ItExpr.IsAny<HttpRequestMessage>(),
                      ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    var requestUrl = request.RequestUri.ToString();

                    if ((requestUrl == TestService.LocalUrl && localResponse) ||
                        (requestUrl == TestService.LocalNetworkUrl && localNetworkResponse) ||
                        (requestUrl == TestService.PublicNetworkUrl && publicNetworkResponse))
                        return new HttpResponseMessage(HttpStatusCode.OK);
                    else
                        throw new HttpRequestException();
                })
                .Verifiable();

            var result = await _serviceChecker.CheckServiceAsync(TestService);

            Assert.NotEmpty(result.Errors);
        }

        [Theory]
        [MemberData(nameof(HttpMessageHandlerMockFunctions))]
        public async Task CheckServiceAsyncShouldReturnCheckResultThatContainsErrorDetailsAboutNotHealthyService(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> messageHandlerMock)
        {
            _messageHandlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                     ItExpr.IsAny<HttpRequestMessage>(),
                     ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(messageHandlerMock)
               .Verifiable();

            var result = await _serviceChecker.CheckServiceAsync(TestService);

            Assert.NotEmpty(result.Errors);

            foreach (var error in result.Errors)
            {
                Assert.False(string.IsNullOrWhiteSpace(error.Key));
                Assert.False(string.IsNullOrWhiteSpace(error.Value));
            }
        }

        [Fact]
        public async Task CheckServiceAsyncShouldCheckOnlyServiceUrlsThatNotEmpty()
        {
            var service = new Shared.Service
            {
                Name = "Service",
                LocalUrl = "http://localhost:1234",
                LocalNetworkUrl = "http://testlocal:1234",
                PublicNetworkUrl = ""
            };

            _messageHandlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                     ItExpr.IsAny<HttpRequestMessage>(),
                     ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
               .Verifiable();

            var result = await _serviceChecker.CheckServiceAsync(service);

            _messageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.Is<HttpRequestMessage>(request => 
                    !request.RequestUri.ToString().Contains(service.LocalUrl) &&
                    !request.RequestUri.ToString().Contains(service.LocalNetworkUrl)),
                ItExpr.IsAny<CancellationToken>());

            _messageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(request => request.RequestUri.ToString().Contains(service.LocalUrl)),
                ItExpr.IsAny<CancellationToken>());

            _messageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.AtLeastOnce(),
                ItExpr.Is<HttpRequestMessage>(request => request.RequestUri.ToString().Contains(service.LocalNetworkUrl)),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task CheckServiceAsyncShouldReturnCheckResultThatOnlyAffectedByNotEmptyUrlsResult()
        {
            var service = new Shared.Service
            {
                Name = "Service",
                LocalUrl = "http://localhost:1234",
                LocalNetworkUrl = "http://testlocal:1234",
                PublicNetworkUrl = ""
            };

            _messageHandlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                     ItExpr.IsAny<HttpRequestMessage>(),
                     ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
               .Verifiable();

            var result = await _serviceChecker.CheckServiceAsync(service);

            Assert.NotNull(result);
            Assert.Empty(result.Errors);
        }

        public static IEnumerable<object[]> HttpMessageHandlerMockFunctions => new List<object[]>
        {
            new[] { NotFoundMessageHandlerMock },
            new[] { UnreachableMessageHandlerMock }
        };

        public static Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> NotFoundMessageHandlerMock = new Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>(
            (request, token) =>
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        public static Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> UnreachableMessageHandlerMock = new Func<HttpRequestMessage, CancellationToken, HttpResponseMessage>(
            (request, token) =>
            {
                throw new HttpRequestException();
            });
    }
}
