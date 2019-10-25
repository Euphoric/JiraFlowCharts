using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jira.FlowCharts.JiraUpdate
{
    class JiraUpdateViewModel : ReactiveScreen
    {
        private readonly TasksSource _tasksSource;
        private int _cachedIssuesCount;
        private DateTime? _lastUpdatedIssue;

        public JiraUpdateViewModel(TasksSource tasksSource)
        {
            _tasksSource = tasksSource;
            DisplayName = "Jira update";
        }

        public int CachedIssuesCount
        {
            get => _cachedIssuesCount;
            private set => Set(ref _cachedIssuesCount, value);
        }

        public DateTime? LastUpdatedIssue
        {
            get => _lastUpdatedIssue;
            private set => Set(ref _lastUpdatedIssue, value);
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            var allTasks = (await _tasksSource.GetAllTasks()).ToList();
            CachedIssuesCount = allTasks.Count;
            LastUpdatedIssue = allTasks.Max(x => x.Updated);
        }
    }
}
