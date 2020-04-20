using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Jira.FlowCharts.IssuesGrid;
using Jira.FlowCharts.JiraUpdate;
using Jira.FlowCharts.ProjectSelector;
using Jira.FlowCharts.StoryFiltering;
using Jira.Querying.Sqlite;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private readonly SqliteJiraLocalCacheRepository _cacheRepository;
        public ProjectSelectorViewModel ProjectSelector { get; }

        public MainViewModel()
        {
            DisplayName = "Jira flow metrics";

            var dataPath = GetPathToData();

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            string databaseFile = Path.Combine(dataPath, @"issuesCache.db");
            _cacheRepository = new SqliteJiraLocalCacheRepository(databaseFile);
            TasksSourceJiraCacheAdapter jiraCacheAdapter = new TasksSourceJiraCacheAdapter(_cacheRepository);
            JsonStatesRepository statesRepository = new JsonStatesRepository(Path.Combine(dataPath, @"analysisSettings.json"));
            var stateFilteringProvider = new StateFilteringProvider(jiraCacheAdapter, statesRepository);
            var tasksSource = new TasksSource(jiraCacheAdapter);

            var issuesFrom = DateTime.Now.AddYears(-1);

            ProjectSelector = new ProjectSelectorViewModel(tasksSource);

            Items.Add(new JiraUpdateViewModel(tasksSource, new CurrentTime()));
            Items.Add(new StoryFilteringViewModel(stateFilteringProvider));
            Items.Add(new IssuesGridViewModel(tasksSource, stateFilteringProvider, ProjectSelector));
            Items.Add(new CumulativeFlowViewModel(tasksSource, stateFilteringProvider, ProjectSelector));
            Items.Add(new CycleTimeScatterplotViewModel(tasksSource, issuesFrom, stateFilteringProvider, ProjectSelector));
            Items.Add(new CycleTimeHistogramViewModel(tasksSource, issuesFrom, stateFilteringProvider, ProjectSelector));
            Items.Add(new CycleTimeHistogramSmoothViewModel(tasksSource, issuesFrom, stateFilteringProvider, ProjectSelector));
            Items.Add(new CycleTimeHistoryViewModel(tasksSource, stateFilteringProvider, ProjectSelector));
            Items.Add(new StoryPointCycleTimeViewModel(tasksSource, issuesFrom, stateFilteringProvider, ProjectSelector));
            Items.Add(new SimulationViewModel(tasksSource, issuesFrom, stateFilteringProvider, ProjectSelector));
        }

        private static string GetPathToData()
        {
#if DEBUG
            return @"../../../Data";
#else
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "JiraFlowMetrics");
#endif
        }

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);

            await ProjectSelector.ActivateAsync();
        }

        protected override async Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            await ProjectSelector.DeactivateAsync(close);

            if (close)
            {
                _cacheRepository.Dispose();
            }

            await base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
