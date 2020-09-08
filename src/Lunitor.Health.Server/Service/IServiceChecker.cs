using Lunitor.Health.Shared;
using System.Threading.Tasks;

namespace Lunitor.Health.Server.Service
{
    public interface IServiceChecker
    {
        Task<ServiceCheckResultDto> CheckServiceAsync(Shared.Service service);
    }
}