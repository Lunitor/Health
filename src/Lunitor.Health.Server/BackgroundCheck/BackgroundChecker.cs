using Ardalis.GuardClauses;
using Lunitor.Health.Server.Notification;
using Lunitor.Health.Server.Service;
using Lunitor.Health.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lunitor.Health.Server.BackgroundCheck
{
    public class BackgroundChecker : BackgroundService
    {
        private readonly IServiceChecker _serviceChecker;
        private readonly IServiceStore _serviceStore;
        private readonly BackgroundCheckerConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private readonly ILogger<BackgroundChecker> _logger;

        public BackgroundChecker(IServiceChecker serviceChecker,
            IServiceStore serviceStore,
            IOptions<BackgroundCheckerConfiguration> configuration,
            INotificationService notificationService,
            ILogger<BackgroundChecker> logger)
        {
            Guard.Against.Null(serviceChecker, nameof(serviceChecker));
            Guard.Against.Null(serviceStore, nameof(serviceStore));
            Guard.Against.Null(configuration, nameof(configuration));
            Guard.Against.Null(notificationService, nameof(notificationService));
            Guard.Against.Null(logger, nameof(logger));

            _serviceChecker = serviceChecker;
            _serviceStore = serviceStore;
            _configuration = configuration.Value;
            _notificationService = notificationService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(MinutesToMiliSeconds(_configuration.InitialDelayInMinutes));

            var services = _serviceStore.GetAll();
            var previousCheckNotFoundErrors = true;

            _logger.LogInformation($"Start periodic service checking with {_configuration.PeriodicityInMinutes} minute periodicity.");
            while (!stoppingToken.IsCancellationRequested)
            {
                var aggregateErros = new List<ServiceCheckResultDto>();

                _logger.LogInformation("Checking services...");
                await CheckServices(services, aggregateErros);
                _logger.LogInformation("Checking services finished");

                if (aggregateErros.Count != 0 && previousCheckNotFoundErrors)
                {
                    _logger.LogInformation("Sending notification about unhealthy services...");
                    await _notificationService.SendErrorsAsync(aggregateErros);
                }

                previousCheckNotFoundErrors = aggregateErros.Count == 0;

                await Task.Delay(MinutesToMiliSeconds(_configuration.PeriodicityInMinutes), stoppingToken);
            }
            _logger.LogInformation("Stop periodic service checking.");
        }

        private async Task CheckServices(IEnumerable<Shared.Service> services, List<ServiceCheckResultDto> aggregateErros)
        {
            foreach (var service in services)
            {
                var checkResult = await _serviceChecker.CheckServiceAsync(service);
                if (checkResult.Errors.Count != 0)
                    aggregateErros.Add(checkResult);
            }
        }

        private int MinutesToMiliSeconds(double minutes)
        {
            return (int)(minutes * 60 * 1000);
        }
    }
}
