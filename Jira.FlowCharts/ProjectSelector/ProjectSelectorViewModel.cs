using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;

namespace Jira.FlowCharts.ProjectSelector
{
    public class ProjectSelectorViewModel : ReactiveScreen, ICurrentProject
    {
        private readonly TasksSource _tasksSource;
        private string _selectedProjectKey;

        public ProjectSelectorViewModel(TasksSource tasksSource)
        {
            _tasksSource = tasksSource;
            ProjectKeys = new ObservableCollection<string>();
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);

            var projectStatistics = await _tasksSource.GetProjectsStatistics();

            ProjectKeys.Clear();
            ProjectKeys.AddRange(projectStatistics.Select(x => x.Key));

            if (SelectedProjectKey == null)
            {
                SelectedProjectKey = ProjectKeys.FirstOrDefault();
            }
        }

        public ObservableCollection<string> ProjectKeys { get; }

        public string SelectedProjectKey
        {
            get => _selectedProjectKey;
            set
            {
                var changed = Set(ref _selectedProjectKey, value);
                if (changed)
                {
                    ProjectKeyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        string ICurrentProject.ProjectKey => SelectedProjectKey;

        public event EventHandler<EventArgs> ProjectKeyChanged;
    }
}