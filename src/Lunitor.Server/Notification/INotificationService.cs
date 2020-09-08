using Lunitor.Health.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lunitor.Health.Server.Notification
{
    public interface INotificationService
    {
        Task SendErrorsAsync(IEnumerable<ServiceCheckResultDto> serviceCheckResults);
    }
}
