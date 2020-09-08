using System.Collections.Generic;

namespace Lunitor.Health.Server.Service
{
    public interface IServiceStore
    {
        IEnumerable<Shared.Service> GetAll();
    }
}