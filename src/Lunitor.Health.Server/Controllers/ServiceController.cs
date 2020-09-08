using Lunitor.Health.Server.Service;
using Lunitor.Health.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lunitor.Health.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceStore _serviceStore;
        private readonly IServiceChecker _serviceChecker;

        public ServiceController(IServiceStore serviceStore, IServiceChecker serviceChecker)
        {
            _serviceStore = serviceStore;
            _serviceChecker = serviceChecker;
        }

        [HttpGet]
        public async Task<IEnumerable<ServiceCheckResultDto>> Get()
        {
            var serviceCheckTasks = _serviceStore.GetAll()
                                                 .Select(s => _serviceChecker.CheckServiceAsync(s));

            var serviceChecks = await Task.WhenAll(serviceCheckTasks);
            return serviceChecks;
        }
    }
}
