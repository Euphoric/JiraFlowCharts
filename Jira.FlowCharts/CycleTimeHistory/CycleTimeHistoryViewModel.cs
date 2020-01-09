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

        public CycleTimeHistoryViewModel(TasksSource tasksSource)
        {
            _tasksSource = tasksSource;
            DisplayName = "Cycle time history";
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            var latestFinishedStories = (await _tasksSource.GetLatestFinishedStories());

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
                var storiesInWindow =
                    orderedStories
                        .Where(x => x.Ended > fromDate && x.Ended <= currentDate)
                        .OrderBy(x=>x.DurationDays)
                        .ToArray();

                if (storiesInWindow.Length == 0)
                    continue;

                labels.Add(currentDate.ToShortDateString());

                issuesCounts.Add(storiesInWindow.Length);
                percentiles50.Add(storiesInWindow[(int) (storiesInWindow.Length * 50 / 100)].DurationDays);
                percentiles70.Add(storiesInWindow[(int) (storiesInWindow.Length * 70 / 100)].DurationDays);
                percentiles85.Add(storiesInWindow[(int) (storiesInWindow.Length * 85 / 100)].DurationDays);
                percentiles95.Add(storiesInWindow[(int) (storiesInWindow.Length * 95 / 100)].DurationDays);
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
                    PointGeometry = null,
                    Title = "Issues count",
                    ScalesYAt = 1
                },
            };

            Labels = labels.ToArray();
        }
    }
}