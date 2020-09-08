using Ardalis.GuardClauses;
using Lunitor.Health.Shared;
using MimeKit;
using MimeKit.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lunitor.Health.Server.Notification
{
    class EmailBuilder : IEmailBuilder
    {
        public MimeMessage BuildUnhealthyServicesEmail(IEnumerable<ServiceCheckResultDto> serviceCheckResults, string senderName, string senderEmail, string[] toEmails)
        {
            Guard.Against.NullOrEmpty(serviceCheckResults, nameof(serviceCheckResults));
            Guard.Against.NullOrEmpty(senderName, nameof(senderName));
            Guard.Against.NullOrEmpty(senderEmail, nameof(senderEmail));
            Guard.Against.NullOrEmpty(toEmails, nameof(toEmails));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.Bcc.AddRange(toEmails.Select(email => MailboxAddress.Parse(email)));
            message.Subject = "Unhealthy service(s)";
            message.Body = new TextPart(TextFormat.Plain)
            {
                Text = CreateEmailBodyFromCheckResults(serviceCheckResults),
            };

            return message;
        }

        private string CreateEmailBodyFromCheckResults(IEnumerable<ServiceCheckResultDto> serviceCheckResults)
        {
            var body = new StringBuilder();

            foreach (var serviceCheckResult in serviceCheckResults)
            {
                body.AppendLine($"{serviceCheckResult.Service.Name}");
                foreach (var error in serviceCheckResult.Errors)
                {
                    body.AppendLine($"{error.Key}: {error.Value}");
                }
                body.AppendLine();
            }

            return body.ToString();
        }
    }
}
