using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Wpf;

namespace Jira.FlowCharts
{
    public class CycleTimeHistogramViewModel : Screen
    {
        private readonly FlowIssue[] _flowIssues;
        private SeriesCollection _seriesCollection;
        private string[] _labels;
        private Func<double, string> _formatter;

        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            private set => Set(ref _seriesCollection, value);
        }

        public string[] Labels
        {
            get => _labels;
            private set => Set(ref _labels, value);
        }

        public Func<double, string> Formatter
        {
            get => _formatter;
            private set => Set(ref _formatter, value);
        }

        public CycleTimeHistogramViewModel(FlowIssue[] flowIssues)
        {
            _flowIssues = flowIssues;
            DisplayName = "Cycle time histogram";
        }

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var histogramNonzero =
                _flowIssues
                    .GroupBy(x => (int)x.Duration + 1)
                    .Select(grp => new { Days = grp.Key, Counts = grp.Count() })
                    .ToDictionary(x => x.Days, x => x.Counts);

            var maxDay = histogramNonzero.Keys.Max();

            var histogram =
                Enumerable.Range(1, maxDay)
                    .Select(x => new { Days = x, Count = HistogramValue(histogramNonzero, x) })
                    .ToArray();

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Story count",
                    Values = new ChartValues<double>(histogram.Select(x=>(double)x.Count))
                }
            };

            Labels = histogram.Select(x => x.Days.ToString()).ToArray();
            Formatter = value => value.ToString("N0");

            return base.OnActivateAsync(cancellationToken);
        }

        private static int HistogramValue<TKey>(Dictionary<TKey, int> histogramNonzero, TKey key)
        {
            if (histogramNonzero.TryGetValue(key, out int value))
            {
                return value;
            }

            return 0;
        }
    }

}
