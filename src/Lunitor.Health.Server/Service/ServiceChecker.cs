using Lunitor.Health.Shared;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lunitor.Health.Server.Service
{
    public class ServiceChecker : IServiceChecker
    {
        private readonly HttpClient _httpClient;

        public ServiceChecker(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ServiceCheckResultDto> CheckServiceAsync(Shared.Service service)
        {
            var requestTasks = new List<Task<KeyValuePair<string, string>>>();

            if(!string.IsNullOrWhiteSpace(service.LocalUrl))
                requestTasks.Add(CheckUrl(service.LocalUrl));
            if (!string.IsNullOrWhiteSpace(service.LocalNetworkUrl))
                requestTasks.Add(CheckUrl(service.LocalNetworkUrl));
            if (!string.IsNullOrWhiteSpace(service.PublicNetworkUrl))
                requestTasks.Add(CheckUrl(service.PublicNetworkUrl));

            var checkResult = new ServiceCheckResultDto { Service = service };

            var responses = await Task.WhenAll(requestTasks);

            foreach (var response in responses)
            {
                if (response.Value != string.Empty)
                    checkResult.Errors.Add(response);
            }

            return checkResult;
        }

        private async Task<KeyValuePair<string, string>> CheckUrl(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                    return new KeyValuePair<string, string>(url, string.Empty);
                else
                    return new KeyValuePair<string, string>(url, response.StatusCode.ToString());
            }
            catch (HttpRequestException exception)
            {
                return new KeyValuePair<string, string>(url, exception.Message);
            }
        }
    }
}
