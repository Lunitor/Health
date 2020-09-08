using Lunitor.Health.Server.Notification;
using Lunitor.Health.Server.Notification.Configurations;
using Lunitor.Health.Shared;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Lunitor.Health.Server.UnitTests.Notification
{
    public class EmailNotificationServiceTests
    {
        private EmailNotificationService emailNotificationService;

        private Mock<IOptions<SmtpConfiguration>> _smtpConfigurationMock;
        private Mock<ISmtpClient> _smtpClientMock;
        private Mock<IEmailBuilder> _emailBuilderMock;
        private Mock<IOptions<NotificationConfiguration>> _notificationConfigurationMock;
        private Mock<ILogger<EmailNotificationService>> _loggerMock;

        private readonly SmtpConfiguration TestSmtpConfiguration = new SmtpConfiguration
        {
            SmtpServer = "test.server.net",
            SmtpPort = 587,
            SenderName = "Test",
            SenderEmail = "test@test.net",
            SenderUserName = "testuser",
            SenderPassword = "password"
        };

        private readonly NotificationConfiguration TestNotificationConfigurationWithOneEmail = new NotificationConfiguration
        {
            NotificationEmails = new[] { "test@test.net" }
        };

        private readonly List<ServiceCheckResultDto> TestServiceErrors = TestDataProvider.TestServiceErrors;

        public EmailNotificationServiceTests()
        {
            _smtpConfigurationMock = new Mock<IOptions<SmtpConfiguration>>();
            _smtpConfigurationMock.SetupGet(conf => conf.Value)
                .Returns(TestSmtpConfiguration);

            _smtpClientMock = new Mock<ISmtpClient>();

            _emailBuilderMock = new Mock<IEmailBuilder>();

            _notificationConfigurationMock = new Mock<IOptions<NotificationConfiguration>>();
            _notificationConfigurationMock.SetupGet(conf => conf.Value)
                .Returns(TestNotificationConfigurationWithOneEmail);

            _loggerMock = new Mock<ILogger<EmailNotificationService>>();

            emailNotificationService = new EmailNotificationService(_smtpConfigurationMock.Object,
                _smtpClientMock.Object,
                _emailBuilderMock.Object,
                _notificationConfigurationMock.Object,
                _loggerMock.Object);
        }

        [Theory]
        [MemberData(nameof(NullConstructorParameterTestData))]
        public void ContructorShouldThrowArgumentNullExceptionWhenNullParameterGiven(IOptions<SmtpConfiguration> smtpConfiguration,
            ISmtpClient smtpClient,
            IEmailBuilder emailBuilder,
            IOptions<NotificationConfiguration> notificationConfiguration)
        {
            Assert.Throws<ArgumentNullException>(() => new EmailNotificationService(smtpConfiguration,
                smtpClient,
                emailBuilder,
                notificationConfiguration,
                _loggerMock.Object));
        }

        [Theory]
        [MemberData(nameof(NullEmptyParameterTestData))]
        public async Task SendErrorsAsyncShouldThrowArgumentExceptionWhenNullOrEmptyParameterGiven(IEnumerable<ServiceCheckResultDto> errors)
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(() => emailNotificationService.SendErrorsAsync(errors));
        }

        [Fact]
        public async Task SendErrorsAsyncShouldDisconnectConnectionAfterSend()
        {
            await emailNotificationService.SendErrorsAsync(TestServiceErrors);

            _smtpClientMock.Verify(client => client.DisconnectAsync(
                It.Is<bool>(quit => quit == true),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        public static IEnumerable<object[]> NullConstructorParameterTestData => new List<object[]>
        {
            new object[] {
                null,
                new Mock<ISmtpClient>().Object,
                new Mock<IEmailBuilder>().Object,
                new Mock<IOptions<NotificationConfiguration>>().Object},
            new object[] {
                new Mock<IOptions<SmtpConfiguration>>().Object,
                null,
                new Mock<IEmailBuilder>().Object,
                new Mock<IOptions<NotificationConfiguration>>().Object},
            new object[] {
                new Mock<IOptions<SmtpConfiguration>>().Object,
                new Mock<ISmtpClient>().Object,
                null,
                new Mock<IOptions<NotificationConfiguration>>().Object},
            new object[] {
                new Mock<IOptions<SmtpConfiguration>>().Object,
                new Mock<ISmtpClient>().Object,
                new Mock<IEmailBuilder>().Object,
                null}
        };

        public static IEnumerable<object[]> NullEmptyParameterTestData => new List<object[]>
        {
            new object[] { null },
            new object[] { new List<ServiceCheckResultDto>() }
        };
    }
}
