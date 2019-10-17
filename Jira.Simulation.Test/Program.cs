using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jira.FlowCharts.Simulation;

namespace Jira.Simulation.Test
{
    class Program
    {
        static void Main(string[] args)
        {

            List<FlowSimulationStatisticOutput> runsStats = new List<FlowSimulationStatisticOutput>();
            for (int i = 0; i < 100; i++)
            {
                if (i%1000 == 0)
                    Console.WriteLine(i);

                FlowSimulationStatisticOutput stats = FlowSimulationStatistics.RunSimulationStatistic(1, new double[] { 1, 2, 2, 3 }, 1000, 10);
                runsStats.Add(stats);
            }

            var runsHistograms = runsStats.Select(x => x.NormalizedHistogram()).ToArray();

            var minLabel = runsHistograms.Min(x => x.Keys.Min());
            var maxLabel = runsHistograms.Max(x => x.Keys.Max());

            var totalHistogram = AddHistograms(runsHistograms, minLabel, maxLabel);

            var sum = totalHistogram.Values.Sum();

            totalHistogram = totalHistogram.ToDictionary(x => x.Key, x => x.Value / sum);

            List<double> distances = new List<double>();

            foreach (var runsHistogram in runsHistograms)
            {
                var distance = HistogramDistance(runsHistogram, totalHistogram);
                distances.Add(distance);
            }

            var orderedDistances = distances.OrderBy(x => x).ToArray();
            var maxDist = orderedDistances[orderedDistances.Length * 99 / 100];

            File.WriteAllLines("out.csv", distances.Select(x=>x.ToString(CultureInfo.InvariantCulture)));

            FlowSimulationStatisticOutput statsTest = FlowSimulationStatistics.RunSimulationStatistic(1, new double[] { 1, 2, 2, 3 }, 10000, 10);

            var testDistance = HistogramDistance(totalHistogram, statsTest.NormalizedHistogram());

            Console.WriteLine("Is valid: "+(testDistance < maxDist));

            //Dictionary<int, double> referenceHistogram = runsStats.NormalizedHistogram();
        }

        private static double HistogramDistance(Dictionary<int, double> runsHistogram, Dictionary<int, double> totalHistogram)
        {
            var minLabelX = Math.Min(runsHistogram.Min(x => x.Key), totalHistogram.Min(x => x.Key));
            var maxLabelX = Math.Max(runsHistogram.Max(x => x.Key), totalHistogram.Max(x => x.Key));

            double distSquared = 0;

            for (int i = minLabelX; i < maxLabelX + 1; i++)
            {
                runsHistogram.TryGetValue(i, out var runVal);
                totalHistogram.TryGetValue(i, out var totalVal);

                distSquared += (runVal - totalVal) * (runVal - totalVal);
            }

            var distance = Math.Sqrt(distSquared);
            return distance;
        }

        private static Dictionary<int, double> AddHistograms(Dictionary<int, double>[] runsHistograms, int minLabel, int maxLabel)
        {
            var sum = new Dictionary<int, double>();

            foreach (var histogram in runsHistograms)
            {
                foreach (KeyValuePair<int, double> pair in histogram)
                {
                    sum.TryGetValue(pair.Key, out var sumVal);

                    sum[pair.Key] = sumVal + pair.Value;
                }
            }

            return sum;
        }
    }
}
