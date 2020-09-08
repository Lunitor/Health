using Lunitor.Health.Shared;
using MimeKit;
using System.Collections.Generic;

namespace Lunitor.Health.Server.Notification
{
    public interface IEmailBuilder
    {
        MimeMessage BuildUnhealthyServicesEmail(IEnumerable<ServiceCheckResultDto> serviceCheckResults, string senderName, string senderEmail, string[] toEmails);
    }
}