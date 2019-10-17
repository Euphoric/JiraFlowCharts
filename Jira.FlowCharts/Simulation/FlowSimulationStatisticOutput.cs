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
        public double percentile50 { get; private set; }
        public double percentile75 { get; private set; }
        public double percentile85 { get; private set; }
        public double percentile95 { get; private set; }
        public double percentile99 { get; private set; }

        public static FlowSimulationStatisticOutput CreateOutput(List<double> simulationTimes)
        {
            FlowSimulationStatisticOutput output = new FlowSimulationStatisticOutput();

            var orderedSimulationTimes = simulationTimes.OrderBy(x => x).ToArray();

            var min = (int)Math.Floor(orderedSimulationTimes[0]);
            var max = (int)Math.Ceiling(orderedSimulationTimes[orderedSimulationTimes.Length * 995 / 1000]);
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

            output.percentile50 = orderedSimulationTimes[orderedSimulationTimes.Length * 50 / 100];
            output.percentile75 = orderedSimulationTimes[orderedSimulationTimes.Length * 75 / 100];
            output.percentile85 = orderedSimulationTimes[orderedSimulationTimes.Length * 85 / 100];
            output.percentile95 = orderedSimulationTimes[orderedSimulationTimes.Length * 95 / 100];
            output.percentile99 = orderedSimulationTimes[orderedSimulationTimes.Length * 99 / 100];
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
