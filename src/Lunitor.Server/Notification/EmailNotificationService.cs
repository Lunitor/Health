using Ardalis.GuardClauses;
using Lunitor.Health.Server.Notification.Configurations;
using Lunitor.Health.Shared;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lunitor.Health.Server.Notification
{
    class EmailNotificationService : INotificationService
    {
        private readonly SmtpConfiguration _smtpConfiguration;
        private readonly ISmtpClient _smtpClient;
        private readonly IEmailBuilder _emailBuilder;
        private readonly NotificationConfiguration _notificationConfiguration;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(IOptions<SmtpConfiguration> smtpConfiguration,
            ISmtpClient smtpClient,
            IEmailBuilder emailBuilder,
            IOptions<NotificationConfiguration> notificationConfiguration,
            ILogger<EmailNotificationService> logger)
        {
            Guard.Against.Null(smtpConfiguration, nameof(smtpConfiguration));
            Guard.Against.Null(smtpClient, nameof(smtpClient));
            Guard.Against.Null(emailBuilder, nameof(emailBuilder));
            Guard.Against.Null(notificationConfiguration, nameof(notificationConfiguration));
            Guard.Against.Null(logger, nameof(logger));

            _smtpConfiguration = smtpConfiguration.Value;
            _smtpClient = smtpClient;
            _emailBuilder = emailBuilder;
            _notificationConfiguration = notificationConfiguration.Value;
            _logger = logger;
        }

        public async Task SendErrorsAsync(IEnumerable<ServiceCheckResultDto> serviceCheckResults)
        {
            Guard.Against.NullOrEmpty(serviceCheckResults, nameof(serviceCheckResults));

            try
            {
                var message = _emailBuilder.BuildUnhealthyServicesEmail(serviceCheckResults,
                    _smtpConfiguration.SenderName,
                    _smtpConfiguration.SenderEmail,
                    _notificationConfiguration.NotificationEmails);

                await _smtpClient.ConnectAsync(_smtpConfiguration.SmtpServer, _smtpConfiguration.SmtpPort);
                await _smtpClient.AuthenticateAsync(_smtpConfiguration.SenderUserName, _smtpConfiguration.SenderPassword);
                await _smtpClient.SendAsync(message);
                await _smtpClient.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send email about unhealthy services", ex);
            }
        }
    }
}
