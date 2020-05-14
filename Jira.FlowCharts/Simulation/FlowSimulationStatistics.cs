using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;

namespace Jira.FlowCharts.Simulation
{
    public static class FlowSimulationStatistics
    {
        public static FlowSimulationStatisticOutput RunSimulationStatistic(double newStoryRate, double[] storyCycleTimes, int simulationRuns, int expectedCompletedStoriesMin, int expectedCompletedStoriesMax)
        {
            List<double> simulationTimes = new List<double>();
            List<double> avgWorkInProgress = new List<double>();
            DiscreteUniform completedStoriesDistribution = new DiscreteUniform(expectedCompletedStoriesMin, expectedCompletedStoriesMax);

            for (int i = 0; i < simulationRuns; i++)
            {
                var simulation = new FlowSimulation(newStoryRate, storyCycleTimes, completedStoriesDistribution.Sample());
                simulation.Run();

                simulationTimes.Add(simulation.SimulationTime);
                avgWorkInProgress.Add(simulation.AverageWorkInProgress);
            }

            FlowSimulationStatisticOutput output = FlowSimulationStatisticOutput.CreateOutput(simulationTimes);

            return output;
        }

        private static void WriteSimulationTimeHistogram(FlowSimulationStatisticOutput output)
        {
            Console.WriteLine("Simulation Time");

            for (int i = 0; i < output.HistogramValues.Length; i++)
            {
                Console.WriteLine($"{output.HistogramLabels[i]:F1} : {output.HistogramValues[i]}");
            }

            Console.WriteLine($"Percentile 50% : {output.Percentile50:F2}");
            Console.WriteLine($"Percentile 70% : {output.Percentile70:F2}");
            Console.WriteLine($"Percentile 85% : {output.Percentile85:F2}");
            Console.WriteLine($"Percentile 95% : {output.Percentile95:F2}");
            Console.WriteLine($"Percentile 99% : {output.Percentile99:F2}");
        }

        private static void WriteWorkInProgressHistogram(List<double> averageWorkInProgres)
        {
            Console.WriteLine("Average work in progress");

            var min = (int)Math.Floor(averageWorkInProgres.Min());
            var max = (int)Math.Ceiling(averageWorkInProgres.Max());
            var buckets = (max - min);
            Histogram hist = new Histogram(averageWorkInProgres, buckets, min, max);

            for (int i = 0; i < hist.BucketCount; i++)
            {
                var bucket = hist[i];
                Console.WriteLine($"{bucket.LowerBound:F1} : {bucket.Count}");
            }
        }
    }
}
