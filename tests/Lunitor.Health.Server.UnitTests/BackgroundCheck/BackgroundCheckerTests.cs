using Lunitor.Health.Server.BackgroundCheck;
using Lunitor.Health.Server.Notification;
using Lunitor.Health.Server.Service;
using Lunitor.Health.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lunitor.Health.Server.UnitTests.BackgroundCheck
{
    public class BackgroundCheckerTests
    {
        private BackgroundChecker backgroundChecker;

        private Mock<IServiceChecker> serviceCheckerMock;
        private Mock<IServiceStore> serviceStoreMock;
        private Mock<IOptions<BackgroundCheckerConfiguration>> backgroundCheckerConfigurationMock;
        private Mock<INotificationService> notificationServiceMock;
        private Mock<ILogger<BackgroundChecker>> logger;

        private const double TestPeriodicityInMinutes = 1.0 / 60.0 / 100.0;
        private readonly BackgroundCheckerConfiguration TestBackgroundCheckerConfiguration = new BackgroundCheckerConfiguration
        {
            PeriodicityInMinutes = TestPeriodicityInMinutes
        };

        private readonly Shared.Service TestService = TestDataProvider.TestService;

        public BackgroundCheckerTests()
        {
            serviceCheckerMock = new Mock<IServiceChecker>();

            serviceStoreMock = new Mock<IServiceStore>();
            serviceStoreMock.Setup(ss => ss.GetAll())
                .Returns(new List<Shared.Service>(){ TestService });

            backgroundCheckerConfigurationMock = new Mock<IOptions<BackgroundCheckerConfiguration>>();
            backgroundCheckerConfigurationMock.SetupGet(bcc => bcc.Value)
                .Returns(TestBackgroundCheckerConfiguration);

            notificationServiceMock = new Mock<INotificationService>();

            logger = new Mock<ILogger<BackgroundChecker>>();

            backgroundChecker = new BackgroundChecker(serviceCheckerMock.Object,
                serviceStoreMock.Object,
                backgroundCheckerConfigurationMock.Object,
                notificationServiceMock.Object,
                logger.Object);
        }

        [Theory]
        [MemberData(nameof(ContructorParameterTestData))]
        public void ConstructorShouldThrowArgumentExceptionWhenNullParameterGiven(
            IServiceChecker serviceChecker,
            IServiceStore serviceStore,
            IOptions<BackgroundCheckerConfiguration> configuration,
            INotificationService notificationService,
            ILogger<BackgroundChecker> logger)
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundChecker(serviceChecker,
                serviceStore,
                configuration,
                notificationService,
                logger));
        }

        [Fact]
        public async Task ExecuteAsyncShouldCallNotificationServiceSendErrorAsyncOnceIfServiceCheckFoundError()
        {
            serviceCheckerMock.Setup(sc => sc.CheckServiceAsync(It.IsAny<Shared.Service>()))
                .Returns(Task.FromResult(new ServiceCheckResultDto
                {
                    Errors = new Dictionary<string, string>
                    {
                        {TestService.LocalNetworkUrl, "timeout"},
                        {TestService.PublicNetworkUrl, "404"}
                    },
                    Service = TestService
                }));

            var stopToken = new CancellationToken();

            await backgroundChecker.StartAsync(stopToken);
            await Task.Delay((int)TestPeriodicityInMinutes);
            await backgroundChecker.StopAsync(stopToken);

            notificationServiceMock.Verify(ns =>
                ns.SendErrorsAsync(It.IsAny<IEnumerable<ServiceCheckResultDto>>()),
                Times.Once);
        }

        public static IEnumerable<object[]> ContructorParameterTestData => new List<object[]>
        {
            new object[] {
                null,
                new Mock<IServiceStore>().Object,
                new Mock<IOptions<BackgroundCheckerConfiguration>>().Object,
                new Mock<INotificationService>().Object,
                new Mock<ILogger<BackgroundChecker>>().Object},
            new object[] {
                new Mock<IServiceChecker>().Object,
                null,
                new Mock<IOptions<BackgroundCheckerConfiguration>>().Object,
                new Mock<INotificationService>().Object,
                new Mock<ILogger<BackgroundChecker>>().Object},
            new object[] {
                new Mock<IServiceChecker>().Object,
                new Mock<IServiceStore>().Object,
                null,
                new Mock<INotificationService>().Object,
                new Mock<ILogger<BackgroundChecker>>().Object},
            new object[] {
                new Mock<IServiceChecker>().Object,
                new Mock<IServiceStore>().Object,
                new Mock<IOptions<BackgroundCheckerConfiguration>>().Object,
                null,
                new Mock<ILogger<BackgroundChecker>>().Object},
            new object[] {
                new Mock<IServiceChecker>().Object,
                new Mock<IServiceStore>().Object,
                new Mock<IOptions<BackgroundCheckerConfiguration>>().Object,
                new Mock<INotificationService>().Object,
                null},
        };
    }
}
