﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;

namespace Jira.FlowCharts
{
    internal class StoryPointCycleTimeViewModel
    {
        public StoryPointCycleTimeViewModel(FlowIssue[] flowIssues)
        {
            var storyPointGrouped = flowIssues
                .Where(x => x.StoryPoints.HasValue && x.Duration.HasValue)
                .Where(x=>x.StoryPoints.Value > 0)
                .GroupBy(x => x.StoryPoints.Value)
                .Select(grp => new
                {
                    StoryPoints = grp.Key,
                    Min = grp.Min(x => x.Duration.Value),
                    Percentile05 = Percentile(grp.Select(x => x.Duration.Value), 0.05),
                    Percentile25 = Percentile(grp.Select(x => x.Duration.Value), 0.25),
                    Percentile75 = Percentile(grp.Select(x => x.Duration.Value), 0.75),
                    Percentile95 = Percentile(grp.Select(x => x.Duration.Value), 0.95),
                    Max = grp.Max(x => x.Duration.Value),
                    Average = grp.Average(x=>x.Duration.Value),
                    Median = Percentile(grp.Select(x => x.Duration.Value), 0.5),
                    Count = grp.Count()
                })
                .OrderBy(x=>x.StoryPoints)
                .ToArray();


            SeriesCollection = new SeriesCollection
            {
                new OhlcSeries()
                {
                    Values =
                        new ChartValues<OhlcPoint>(
                            storyPointGrouped
                                .Select(grp => new OhlcPoint(grp.Percentile25, grp.Max, grp.Min, grp.Percentile75))
                                .ToArray())
                },
                new LineSeries
                {
                    Values = new ChartValues<double>(storyPointGrouped.Select(x=>(double)x.Median)),
                    Fill = Brushes.Transparent
                }
            };

            Labels = storyPointGrouped.Select(x => x.StoryPoints.ToString()).ToArray();
        }

        private double Percentile(IEnumerable<double> values, double percentile)
        {
            var ordered = values.OrderBy(x => x).ToArray();
            return ordered[(int) (ordered.Length * percentile)];
        }

        public SeriesCollection SeriesCollection { get; set; }

        private string[] _labels;
        public string[] Labels
        {
            get { return _labels; }
            set
            {
                _labels = value;
                OnPropertyChanged("Labels");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}