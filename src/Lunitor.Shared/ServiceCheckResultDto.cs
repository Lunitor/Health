using System.Collections.Generic;

namespace Lunitor.Health.Shared
{
    public class ServiceCheckResultDto
    {
        public Service Service { get; set; }
        public IDictionary<string, string> Errors { get; set; }

        public ServiceCheckResultDto()
        {
            Errors = new Dictionary<string, string>();
        }

        public ServiceCheckResultDto(Service service, IDictionary<string, string> errors)
        {
            Service = service;
            Errors = errors;
        }
    }
}
