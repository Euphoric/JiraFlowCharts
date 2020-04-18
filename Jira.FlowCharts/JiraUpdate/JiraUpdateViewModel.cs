using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
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

            _projects = new ObservableCollection<JiraProjectViewModel>();
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

        private string _lastUpdatedKey;
        public string LastUpdatedKey
        {
            get => _lastUpdatedKey;
            private set => Set(ref _lastUpdatedKey, value);
        }

        double _updateProgress;
        public double UpdateProgress
        {
            get => _updateProgress;
            private set => Set(ref _updateProgress, value);
        }

        private readonly ObservableCollection<JiraProjectViewModel> _projects;
        public ReadOnlyObservableCollection<JiraProjectViewModel> Projects => new ReadOnlyObservableCollection<JiraProjectViewModel>(_projects);

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await UpdateDisplay();
        }

        private async Task UpdateDisplay()
        {
            var allIssues = (await _tasksSource.GetAllIssues()).ToList();
            CachedIssuesCount = allIssues.Count;
            LastUpdatedIssue = allIssues.Max(x => x.Updated);

            _projects.Clear();

            var projectKeys = allIssues.Select(x=>x.Key.Split('-')[0]).Distinct();
            foreach (var projectKey in projectKeys)
            {
                _projects.Add(new JiraProjectViewModel(projectKey));
            }
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
                LastUpdatedKey = "Done";
            }
            catch (Exception e)
            {
                UpdateError = e.ToString();
            }
        }

        void ICacheUpdateProgress.UpdatedIssue(string key, DateTime updated)
        {
            LastUpdatedKey = key;

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
