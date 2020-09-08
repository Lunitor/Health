using Castle.DynamicProxy.Contributors;
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

namespace Lunitor.Health.Server.IntegrationTests.BackgroundCheck
{
    public class BackgroundCheckerTests
    {
        private BackgroundChecker backgroundChecker;

        private Mock<IServiceChecker> serviceCheckerMock;
        private Mock<IServiceStore> serviceStoreMock;
        private Mock<IOptions<BackgroundCheckerConfiguration>> backgroundCheckerConfigurationMock;
        private Mock<INotificationService> notificationServiceMock;
        private Mock<ILogger<BackgroundChecker>> logger;

        private const double TestPeriodicityInMinutes = 1.0/60.0;
        private const double TestDelayInMinutes = 0.0;
        private readonly BackgroundCheckerConfiguration TestBackgroundCheckerConfiguration = new BackgroundCheckerConfiguration
        {
            PeriodicityInMinutes = TestPeriodicityInMinutes,
            InitialDelayInMinutes = TestDelayInMinutes
        };

        private readonly Shared.Service TestService = new Shared.Service
        {
            Name = "test",
            LocalUrl = "http://localhost:1234",
            LocalNetworkUrl = "http://test:1234",
            PublicNetworkUrl = "http://public.test.net",
        };
        private readonly ServiceCheckResultDto ResultWithErrors;
        private readonly ServiceCheckResultDto ResultWithoutErrors;

        public BackgroundCheckerTests()
        {
            ResultWithErrors = new ServiceCheckResultDto
            {
                Errors = new Dictionary<string, string>
                    {
                                    {TestService.LocalNetworkUrl, "timeout"},
                                    {TestService.PublicNetworkUrl, "404"}
                    },
                Service = TestService
            };
            ResultWithoutErrors = new ServiceCheckResultDto
            {
                Errors = new Dictionary<string, string>(),
                Service = TestService
            };

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

        [Fact]
        public async Task ExecuteAsyncShouldCheckServicesInConfiguredIntervals()
        {
            var invocationTimeStamps = new List<DateTime>();

            serviceCheckerMock.Setup(sc => sc.CheckServiceAsync(It.IsAny<Shared.Service>()))
                .Returns((Shared.Service service) =>
                {
                    invocationTimeStamps.Add(DateTime.Now);

                    return Task.FromResult(new ServiceCheckResultDto());
                });

            var stopToken = new CancellationToken();

            await backgroundChecker.StartAsync(stopToken);
            await Task.Delay(MinutesToMiliSeconds(TestPeriodicityInMinutes * 2));
            await backgroundChecker.StopAsync(stopToken);


            for (int i = 1; i < invocationTimeStamps.Count; i++)
            {
                var elapsedTime = invocationTimeStamps[i] - invocationTimeStamps[i - 1];
                Assert.InRange(elapsedTime,
                    TimeSpan.FromSeconds(TestPeriodicityInMinutes * 0.9 * 60),
                    TimeSpan.FromSeconds(TestPeriodicityInMinutes * 1.1 * 60));
            }
        }

        [Fact]
        public async Task ExecuteAsyncShouldCallNotificationServiceSendErrorAsyncAtTheFirstCheckThatHasErrors()
        {
            serviceCheckerMock.Setup(sc => sc.CheckServiceAsync(It.IsAny<Shared.Service>()))
                .Returns(Task.FromResult(ResultWithErrors));

            var stopToken = new CancellationToken();

            await backgroundChecker.StartAsync(stopToken);
            await Task.Delay(MinutesToMiliSeconds(TestPeriodicityInMinutes * 2));
            await backgroundChecker.StopAsync(stopToken);

            notificationServiceMock.Verify(ns =>
                ns.SendErrorsAsync(It.IsAny<IEnumerable<ServiceCheckResultDto>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsyncShouldCallNotificationServiceSendErrorAsyncAfterThereWasCheckWithoutErrorBetweenTwoErrorContainedChecks()
        {
            int invocationCount = 0;
            serviceCheckerMock.Setup(sc => sc.CheckServiceAsync(It.IsAny<Shared.Service>()))
                .Returns((Shared.Service service) =>
                {
                    invocationCount++;
                    if (invocationCount == 2)
                        return Task.FromResult(ResultWithoutErrors);
                    else
                        return Task.FromResult(ResultWithErrors);
                });

            var stopToken = new CancellationToken();

            await backgroundChecker.StartAsync(stopToken);
            await Task.Delay(MinutesToMiliSeconds(TestPeriodicityInMinutes * 3));
            await backgroundChecker.StopAsync(stopToken);

            notificationServiceMock.Verify(ns =>
                ns.SendErrorsAsync(It.IsAny<IEnumerable<ServiceCheckResultDto>>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task ExecuteAsyncShouldWaitForConfiguredAmountOfTimeBeforeCallStartCheckingServicesHealth()
        {
            var initialDelayInMinutes = 1.0 / 60.0;
            TestBackgroundCheckerConfiguration.InitialDelayInMinutes = initialDelayInMinutes;

            var firstServiceCheck = true;
            var firstServiceCheckTimeStamp = DateTime.Now.AddHours(-1);
            serviceCheckerMock.Setup(sc => sc.CheckServiceAsync(It.IsAny<Shared.Service>()))
                .Returns((Shared.Service service) =>
                {
                    if (firstServiceCheck)
                    {
                        firstServiceCheckTimeStamp = DateTime.Now;
                        firstServiceCheck = false;
                    }

                    return Task.FromResult(ResultWithoutErrors);
                });

            var stopToken = new CancellationToken();

            var startTimeStamp = DateTime.Now;
            await backgroundChecker.StartAsync(stopToken);
            await Task.Delay(MinutesToMiliSeconds(initialDelayInMinutes * 2));
            await backgroundChecker.StopAsync(stopToken);

            var actualWaitTime = firstServiceCheckTimeStamp - startTimeStamp;
            Assert.InRange(actualWaitTime,
                TimeSpan.FromMilliseconds(MinutesToMiliSeconds(initialDelayInMinutes) * 0.9),
                TimeSpan.FromMilliseconds(MinutesToMiliSeconds(initialDelayInMinutes) * 1.1));
        }

        private static int MinutesToMiliSeconds(double minutes)
        {
            return (int)(minutes * 60 * 1000);
        }

    }
}
