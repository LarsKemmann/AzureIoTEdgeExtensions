using Prometheus;
using System;
using System.Linq;

namespace IoTEdge.Extensions.Hosting
{
    public sealed class PrometheusModuleMetrics : IModuleMetrics
    {
        private readonly string[] baseLabelNames;
        private readonly string[] baseLabelValues;


        public PrometheusModuleMetrics()
        {
            // Annotate all metrics with the device and module IDs.
            var deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");
            var moduleId = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new InvalidOperationException($"IOTEDGE_DEVICEID environment variable is null or blank.");
            if (string.IsNullOrWhiteSpace(moduleId))
                throw new InvalidOperationException($"IOTEDGE_MODULEID environment variable is null or blank.");
            baseLabelNames = new[] { "device_id", "module_id" };
            baseLabelValues = new[] { deviceId, moduleId };
        }


        public IHistogram CreateHistogram(string name, string help, double[] buckets = null, params string[] additionalLabels)
        {
            return Metrics.CreateHistogram(name, help,
                new HistogramConfiguration
                {
                    Buckets = buckets,
                    LabelNames = baseLabelNames.Concat(additionalLabels).ToArray(),
                    SuppressInitialValue = true
                })
                .WithLabels(baseLabelValues);
        }

        public IHistogram CreateDurationHistogram(string name, string help, params string[] additionalLabels)
        {
            // Duration buckets are logarithmic (power-of-10) and range from <0.1ms to >10s
            return CreateHistogram(name, help, Histogram.ExponentialBuckets(start: .0001, factor: 10, 7), additionalLabels);
        }

        public IGauge CreateGauge(string name, string help, params string[] additionalLabels)
        {
            return Metrics.CreateGauge(name, help,
                new GaugeConfiguration
                {
                    LabelNames = baseLabelNames.Concat(additionalLabels).ToArray(),
                    SuppressInitialValue = true
                }).WithLabels(baseLabelValues);
        }
    }
}
