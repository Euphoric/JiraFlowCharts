using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace Jira.FlowCharts.Simulation
{
    public class FlowSimulationStatisticOutput
    {
        public double[] HistogramValues { get; private set; }
        public double[] HistogramLabels { get; private set; }
        public double Percentile50 { get; private set; }
        public double Percentile75 { get; private set; }
        public double Percentile85 { get; private set; }
        public double Percentile95 { get; private set; }
        public double Percentile99 { get; private set; }

        public static FlowSimulationStatisticOutput CreateOutput(List<double> simulationTimes)
        {
            FlowSimulationStatisticOutput output = new FlowSimulationStatisticOutput();

            var dp = new DurationPercentiles(simulationTimes);

            var min = (int)Math.Floor(dp.Durations.First());
            var max = (int)Math.Ceiling(dp.DurationAtPercentile(0.9995)); // limit the histogram to 99.95% of times, to hide extremes and shorten the tail
            var buckets = (max - min);
            Histogram hist = new Histogram(simulationTimes, buckets, min, max);
            
            output.HistogramValues = new double[hist.BucketCount];
            output.HistogramLabels = new double[hist.BucketCount];

            double simulationsCount = simulationTimes.Count;
            for (int i = 0; i < hist.BucketCount; i++)
            {
                var bucket = hist[i];
                output.HistogramValues[i] = (bucket.Count / simulationsCount) * 100; // % of times the simulation ended at this time
                output.HistogramLabels[i] = bucket.LowerBound;
            }

            output.Percentile50 = dp.DurationAtPercentile(0.50);
            output.Percentile75 = dp.DurationAtPercentile(0.75);
            output.Percentile85 = dp.DurationAtPercentile(0.85);
            output.Percentile95 = dp.DurationAtPercentile(0.95);
            output.Percentile99 = dp.DurationAtPercentile(0.99);

            return output;
        }

        public Dictionary<int, double> NormalizedHistogram()
        {
            double total = HistogramValues.Sum();

            return Enumerable
                .Zip(HistogramLabels, HistogramValues, (label, value) => new {label = (int) label, value})
                .ToDictionary(x => x.label, x => x.value / total);
        }
    }
}
