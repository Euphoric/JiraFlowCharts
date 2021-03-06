﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Wpf;
using MathNet.Numerics.Statistics;

namespace Jira.FlowCharts
{
    public class CycleTimeHistogramViewModel : Screen
    {
        private readonly TasksSource _taskSource;
        private SeriesCollection _seriesCollection;
        private string[] _labels;
        private Func<double, string> _formatter;
        private readonly DateTime _issuesFrom;
        private readonly IStateFilteringProvider _stateFilteringProvider;
        private readonly ICurrentProject _currentProject;

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

        public CycleTimeHistogramViewModel(TasksSource taskSource, DateTime issuesFrom, IStateFilteringProvider stateFilteringProvider, ICurrentProject currentProject)
        {
            _taskSource = taskSource;
            _issuesFrom = issuesFrom;
            _stateFilteringProvider = stateFilteringProvider;
            _currentProject = currentProject;
            DisplayName = "Cycle time histogram";

            _currentProject.ProjectKeyChanged += async (sender, args) =>
            {
                if (IsActive)
                    await Update();
            };
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await Update();
        }

        private async Task Update()
        {
            var stateFilteringParameter = await _stateFilteringProvider.GetStateFilteringParameter();
            var finishedStories = await _taskSource.GetLatestFinishedStories(_currentProject.ProjectKey,
                new IssuesFromParameters(_issuesFrom), stateFilteringParameter);

            var durations = finishedStories.Select(x => x.DurationDays).OrderBy(x => x).ToArray();

            int max = (int) Math.Ceiling(durations[durations.Length * 99 / 100]);

            Histogram hist = new Histogram(durations, max, 0, max);

            ChartValues<double> chartValues = new ChartValues<double>();
            List<string> labels = new List<string>();

            for (int i = 0; i < hist.BucketCount; i++)
            {
                var bucket = hist[i];

                chartValues.Add(bucket.Count);
                labels.Add(bucket.LowerBound.ToString("N0"));
            }

            SeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Story count",
                    Values = chartValues
                }
            };

            Labels = labels.ToArray();
            Formatter = value => value.ToString("N0");
        }
    }

}
