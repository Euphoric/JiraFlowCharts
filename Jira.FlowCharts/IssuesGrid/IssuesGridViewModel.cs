using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using DynamicData;

namespace Jira.FlowCharts.IssuesGrid
{
    class IssuesGridViewModel : Screen
    {
        private readonly TasksSource _tasksSource;

        public IssuesGridViewModel(TasksSource tasksSource)
        {
            _tasksSource = tasksSource;
            DisplayName = "Issues grid";

            Issues = new ObservableCollection<AnalyzedIssue>();
        }

        public ObservableCollection<AnalyzedIssue> Issues { get; }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            Issues.Clear();

            Issues.AddRange(await _tasksSource.GetAllIssues());
        }
    }
}
