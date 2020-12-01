using Microsoft.Extensions.Hosting;
using Prometheus;
using System.Threading;
using System.Threading.Tasks;

namespace IoTEdge.Extensions.Hosting
{
    public sealed class PrometheusService : IHostedService
    {
        private readonly MetricServer server = new MetricServer(port: 9600);


        public Task StartAsync(CancellationToken cancellationToken)
        {
            server.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return server.StopAsync();
        }
    }
}
