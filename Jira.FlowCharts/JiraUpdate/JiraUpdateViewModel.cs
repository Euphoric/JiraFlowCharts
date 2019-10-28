using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace Jira.FlowCharts.JiraUpdate
{
    public class JiraUpdateViewModel : ReactiveScreen
    {
        private readonly TasksSource _tasksSource;
        private int _cachedIssuesCount;
        private DateTime? _lastUpdatedIssue;
        private string _updateError;

        public JiraUpdateViewModel(TasksSource tasksSource)
        {
            _tasksSource = tasksSource;
            DisplayName = "Jira update";

            UpdateCommand = ReactiveCommand.CreateFromTask(UpdateJira);
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

        public ReactiveCommand<Unit, Unit> UpdateCommand { get; }

        public string UpdateError
        {
            get => _updateError;
            private set => Set(ref _updateError, value);
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
            try
            {
                var view = GetView() as IJiraUpdateView;

                if (view == null)
                {
                    throw new InvalidOperationException("Attached view was not correct");
                }

                var jiraLoginParameters = view.GetLoginParameters();

                await _tasksSource.UpdateIssues(jiraLoginParameters);

                await UpdateDisplay();
            }
            catch (Exception e)
            {
                UpdateError = e.ToString();
            }
        }
    }
}
