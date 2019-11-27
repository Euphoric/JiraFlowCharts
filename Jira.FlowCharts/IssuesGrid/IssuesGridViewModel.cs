using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Mappers;
using Caliburn.Micro;
using DynamicData;
using TimeSpan = System.TimeSpan;

namespace Jira.FlowCharts.IssuesGrid
{
    class IssuesGridViewModel : Screen
    {
        private readonly TasksSource _tasksSource;

        public IssuesGridViewModel(TasksSource tasksSource)
        {
            _tasksSource = tasksSource;
            DisplayName = "Issues grid";

            Issues = new ObservableCollection<dynamic>();
        }

        public ObservableCollection<dynamic> Issues { get; }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            Issues.Clear();

            var allIssues = await _tasksSource.GetAllIssues();

            var mapper = new Mapper(new MapperConfiguration(cfg => { }));
            Issues.AddRange(allIssues.Select(issue => ToDynamicRow(issue, mapper)));
        }

        private dynamic ToDynamicRow(AnalyzedIssue issue, Mapper mapper)
        {
            dynamic row = mapper.Map<ExpandoObject>(issue);

            row.OriginalEstimate = issue.OriginalEstimate.HasValue ? TimeSpan.FromMinutes(issue.OriginalEstimate.Value).TotalDays : (double?)null;
            row.TimeSpent = issue.TimeSpent.HasValue ? TimeSpan.FromMinutes(issue.TimeSpent.Value).TotalDays : (double?)null;
            row.IsValid = TasksSource.IsValidIssue(issue);
            row.DurationDays = issue.Duration?.TotalDays;

            return row;
        }
    }
}
