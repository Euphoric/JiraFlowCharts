using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jira.Querying;
using ReactiveUI;

namespace Jira.FlowCharts.JiraUpdate
{
    public class JiraUpdateViewModel : ReactiveScreen, ICacheUpdateProgress
    {
        private readonly TasksSource _tasksSource;
        private readonly ICurrentTime _currentTime;
        private int _cachedIssuesCount;
        private DateTime? _lastUpdatedIssue;
        private string _updateError;
        DateTime? _updateProgressReportStartTime;

        public JiraUpdateViewModel(TasksSource tasksSource, ICurrentTime currentTime)
        {
            _tasksSource = tasksSource;
            _currentTime = currentTime;

            DisplayName = "Jira update";

            UpdateCommand = ReactiveCommand.CreateFromTask(UpdateJira);
            UpdateProgress = -1;
        }

        public string JiraUrl { get; set; }

        public string JiraUsername { get; set; }

        public string ProjectKey { get; set; }

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

        public ReactiveCommand<Unit, Unit> UpdateCommand { get; }

        public string UpdateError
        {
            get => _updateError;
            private set => Set(ref _updateError, value);
        }

        double _updateProgress;
        public double UpdateProgress
        {
            get => _updateProgress;
            private set => Set(ref _updateProgress, value);
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await UpdateDisplay();
        }

        private async Task UpdateDisplay()
        {
            var allTasks = (await _tasksSource.GetAllIssues()).ToList();
            CachedIssuesCount = allTasks.Count;
            LastUpdatedIssue = allTasks.Max(x => x.Updated);
        }

        private async Task UpdateJira()
        {
            UpdateError = null;
            UpdateProgress = 0;
            _updateProgressReportStartTime = null;
            try
            {
                var view = GetView() as IJiraUpdateView;

                if (view == null)
                {
                    throw new InvalidOperationException("Attached view was not correct");
                }

                var jiraLoginParameters = new JiraLoginParameters(JiraUrl, JiraUsername, view.GetLoginPassword());

                await _tasksSource.UpdateIssues(jiraLoginParameters, ProjectKey, this);

                await UpdateDisplay();

                UpdateProgress = 100;
            }
            catch (Exception e)
            {
                UpdateError = e.ToString();
            }
        }

        void ICacheUpdateProgress.UpdatedIssue(string key, DateTime updated)
        {
            if (!_updateProgressReportStartTime.HasValue)
            {
                UpdateProgress = 1;
                _updateProgressReportStartTime = updated;
            }
            else
            {
                var maxTimeSpan = _currentTime.UtcNow - _updateProgressReportStartTime.Value;
                var actualTimeSpan = _currentTime.UtcNow - updated;
                UpdateProgress = (1 - (actualTimeSpan.TotalDays / maxTimeSpan.TotalDays)) * 99 + 1;
            }
        }
    }
}
