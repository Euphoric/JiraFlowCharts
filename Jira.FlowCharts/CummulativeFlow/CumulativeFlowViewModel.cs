﻿using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Jira.FlowCharts
{

    public class CumulativeFlowViewModel : Screen
    {
        private readonly TasksSource _source;
        private readonly StateFiltering _stateFiltering;

        public CumulativeFlowViewModel(TasksSource source, StateFiltering stateFiltering)
        {
            _source = source;
            _stateFiltering = stateFiltering;
            DisplayName = "Cumulative flow";

            SeriesCollection = new SeriesCollection();
            XFormatter = val => new DateTime((long)val).ToShortDateString();
        }

        public SeriesCollection SeriesCollection { get; private set; }
        public Func<double, string> XFormatter { get; private set; }

        protected override async Task OnActivateAsync(CancellationToken token)
        {
            var fromDate = DateTime.Now.AddMonths(-3);

            var cfa = new CumulativeFlowAnalysis(await _source.GetStories(), _stateFiltering.FilteredStates.ToArray(), fromDate);

            SeriesCollection.Clear();

            foreach (var state in cfa.States)
            {
                SeriesCollection.Add(new StackedAreaSeries
                {
                    Title = state,
                    Values = new ChartValues<DateTimePoint>(),
                    LineSmoothness = 0
                });
            }

            foreach (var change in cfa.Changes)
            {
                for (int i = 0; i < cfa.States.Length; i++)
                {
                    SeriesCollection[i].Values.Add(new DateTimePoint(change.Date, change.StateCounts[i]));
                }
            }
        }
    }
}