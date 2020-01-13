using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using ReactiveUI;

namespace Jira.FlowCharts
{
    public class DurationPercentiles
    {
        public DurationPercentiles(IEnumerable<double> durations)
        {
            Durations = durations.OrderBy(x => x).ToImmutableArray();

            if (!Durations.Any())
                throw new ArgumentException(@"Durations must have at least one element", nameof(durations));
        }

        public IReadOnlyCollection<double> Durations { get; }

        public double DurationAtPercentile(double percentile)
        {
            int elementIndex = (int) Math.Round(percentile * (Durations.Count - 1));

            return Durations.ElementAt(elementIndex);
        }

        public double PercentileAtDuration(double duration)
        {
            var firstItemGreaterThanDuration = Durations.Select((x, i) => Tuple.Create(i, x)).FirstOrDefault(t => t.Item2 > duration);
            double indexOfDuration = firstItemGreaterThanDuration?.Item1 - 1 ?? Durations.Count - 1;
            return indexOfDuration / (double) (Durations.Count - 1);
        }
    }
}
