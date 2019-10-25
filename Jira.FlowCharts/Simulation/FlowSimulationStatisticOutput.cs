using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace Jira.FlowCharts.Simulation
{
    public class FlowSimulationStatisticOutput
    {
        public int[] HistogramValues { get; private set; }
        public double[] HistogramLabels { get; private set; }
        public double Percentile50 { get; private set; }
        public double Percentile75 { get; private set; }
        public double Percentile85 { get; private set; }
        public double Percentile95 { get; private set; }
        public double Percentile99 { get; private set; }

        public static FlowSimulationStatisticOutput CreateOutput(List<double> simulationTimes)
        {
            FlowSimulationStatisticOutput output = new FlowSimulationStatisticOutput();

            var orderedSimulationTimes = simulationTimes.OrderBy(x => x).ToArray();

            var min = (int)Math.Floor(orderedSimulationTimes[0]);
            var max = (int)Math.Ceiling(orderedSimulationTimes[orderedSimulationTimes.Length * 9995 / 10000]); // limit the histogram to 99.95% of times, to hide extremes and shorten the tail
            var buckets = (max - min);
            Histogram hist = new Histogram(simulationTimes, buckets, min, max);
            
            output.HistogramValues = new int[hist.BucketCount];
            output.HistogramLabels = new double[hist.BucketCount];

            for (int i = 0; i < hist.BucketCount; i++)
            {
                var bucket = hist[i];
                output.HistogramValues[i] = (int)bucket.Count;
                output.HistogramLabels[i] = bucket.LowerBound;
            }

            output.Percentile50 = orderedSimulationTimes[orderedSimulationTimes.Length * 50 / 100];
            output.Percentile75 = orderedSimulationTimes[orderedSimulationTimes.Length * 75 / 100];
            output.Percentile85 = orderedSimulationTimes[orderedSimulationTimes.Length * 85 / 100];
            output.Percentile95 = orderedSimulationTimes[orderedSimulationTimes.Length * 95 / 100];
            output.Percentile99 = orderedSimulationTimes[orderedSimulationTimes.Length * 99 / 1000];

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
