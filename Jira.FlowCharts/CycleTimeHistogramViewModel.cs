﻿using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using LiveCharts;
using LiveCharts.Wpf;

namespace Jira.FlowCharts
{
    public class CycleTimeHistogramViewModel : Screen
    {
        public CycleTimeHistogramViewModel(FlowIssue[] flowIssues)
        {
            DisplayName = "Cycle time histogram";

            var histogramNonzero =
                flowIssues
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
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }


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
