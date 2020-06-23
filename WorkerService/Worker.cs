using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkerService
{
    public class Worker : IHostedService
    {
        private readonly IBusControl _busControl;
        private readonly ILogger _log;

        public Worker(IBusControl busControl, ILoggerFactory loggerFactory)
        {
            _busControl = busControl;
            _log = loggerFactory.CreateLogger<Worker>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Starting bus...");
            return _busControl.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation("Stopping bus...");
            return _busControl.StopAsync(cancellationToken);
        }
    }
}