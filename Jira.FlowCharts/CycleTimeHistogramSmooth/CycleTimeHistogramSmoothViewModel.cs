using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Wpf;

namespace Jira.FlowCharts
{
    public class CycleTimeHistogramSmoothViewModel : Screen
    {
        private readonly TasksSource _taskSource;
        private SeriesCollection _seriesCollection;
        private string[] _labels;
        private Func<double, string> _formatter;
        private DateTime _issuesFrom;
        private readonly IStateFilteringProvider _stateFilteringProvider;

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

        public CycleTimeHistogramSmoothViewModel(TasksSource taskSource, DateTime issuesFrom, IStateFilteringProvider stateFilteringProvider)
        {
            _taskSource = taskSource;
            _issuesFrom = issuesFrom;
            _stateFilteringProvider = stateFilteringProvider;
            DisplayName = "Cycle time smooth histogram";
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var finishedStories = await _taskSource.GetLatestFinishedStories(new IssuesFromParameters(_issuesFrom), _stateFilteringProvider.GetStateFilteringParameter());

            var durations = finishedStories.Select(x => x.DurationDays).OrderBy(x=>x).ToArray();

            int max = (int)Math.Ceiling(durations[durations.Length * 99 / 100]);

            ChartValues<double> chartValues = new ChartValues<double>();
            List<string> labels = new List<string>();

            var gaussConst = 1 / Math.Sqrt(2 * Math.PI);
            var kernelScale = 5;

            for (double x = 0; x < max; x += 0.2)
            {
                double height = 0;
                for (int i = 0; i < durations.Length; i++)
                {
                    var dist = durations[i] - x;

                    var h = gaussConst * Math.Exp(-1 / 2.0 * dist * dist * kernelScale);

                    height += h;
                }

                chartValues.Add(height);
                labels.Add(x.ToString("N0"));
            }

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Story count",
                    Values = chartValues,
                    PointGeometry = Geometry.Empty
                }
            };

            Labels = labels.ToArray();
            Formatter = value => value.ToString("N1");
        }
    }

}
