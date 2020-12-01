using Prometheus;

namespace IoTEdge.Extensions.Hosting
{
    public interface IModuleMetrics
    {
        IHistogram CreateHistogram(string name, string help, double[] buckets = null, params string[] additionalLabels);

        IHistogram CreateDurationHistogram(string name, string help, params string[] additionalLabels);

        IGauge CreateGauge(string name, string help, params string[] additionalLabels);
    }
}
