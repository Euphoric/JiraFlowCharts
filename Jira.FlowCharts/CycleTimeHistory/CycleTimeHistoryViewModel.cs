using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Jira.FlowCharts
{
    public class CycleTimeHistoryViewModel : Screen
    {
        private readonly TasksSource _tasksSource;
        private readonly IStateFilteringProvider _stateFilteringProvider;
        private readonly ICurrentProject _currentProject;
        private string[] _labels;
        private SeriesCollection _seriesCollection;

        public SeriesCollection SeriesCollection
        {
            get => _seriesCollection;
            private set => Set(ref _seriesCollection, value);
        }

        public string[] Labels
        {
            get => _labels;
            set => Set(ref _labels, value);
        }

        public CycleTimeHistoryViewModel(TasksSource tasksSource, IStateFilteringProvider stateFilteringProvider, ICurrentProject currentProject)
        {
            _tasksSource = tasksSource;
            _stateFilteringProvider = stateFilteringProvider;
            _currentProject = currentProject;
            DisplayName = "Cycle time and throughput history";
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var stateFilteringParameter = await _stateFilteringProvider.GetStateFilteringParameter();
            var latestFinishedStories = await _tasksSource.GetFinishedStories(_currentProject.ProjectKey, stateFilteringParameter);

            var orderedStories = latestFinishedStories.OrderBy(x => x.Ended).ToArray();

            TimeSpan historyWindow = TimeSpan.FromDays(-3*30);

            var endDate = orderedStories.Last().Ended.Date;
            var startDate = endDate.AddYears(-1);

            List<double> percentiles50 = new List<double>();
            List<double> percentiles70 = new List<double>();
            List<double> percentiles85 = new List<double>();
            List<double> percentiles95 = new List<double>();
            List<double> issuesCounts = new List<double>();
            List<string> labels = new List<string>();

            for (DateTime currentDate = startDate; currentDate <= endDate; currentDate += TimeSpan.FromDays(1))
            {
                var fromDate = currentDate.Add(historyWindow);
                var durationsInWindow =
                    orderedStories
                        .Where(x => x.Ended > fromDate && x.Ended <= currentDate)
                        .Select(x=>x.DurationDays)
                        .ToArray();

                if (durationsInWindow.Length == 0)
                    continue;

                labels.Add(currentDate.ToShortDateString());

                var dp = new DurationPercentiles(durationsInWindow);

                issuesCounts.Add(durationsInWindow.Length);
                percentiles50.Add(dp.DurationAtPercentile(0.50));
                percentiles70.Add(dp.DurationAtPercentile(0.70));
                percentiles85.Add(dp.DurationAtPercentile(0.85));
                percentiles95.Add(dp.DurationAtPercentile(0.95));
            }


            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<double>(percentiles50),
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    Title = "Percentile 50%"
                },
                new LineSeries
                {
                    Values = new ChartValues<double>(percentiles70),
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    Title = "Percentile 70%"
                },
                new LineSeries
                {
                    Values = new ChartValues<double>(percentiles85),
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    Title = "Percentile 85%"
                },
                new LineSeries
                {
                    Values = new ChartValues<double>(percentiles95),
                    Fill = Brushes.Transparent,
                    PointGeometry = null,
                    Title = "Percentile 95%"
                },
                new LineSeries
                {
                    Values = new ChartValues<double>(issuesCounts),
                    Fill = Brushes.Transparent,
                    StrokeDashArray = new DoubleCollection(new double[]{ 4, 2 }),
                    PointGeometry = null,
                    Title = "Issues count",
                    ScalesYAt = 1
                },
            };

            Labels = labels.ToArray();
        }
    }
}