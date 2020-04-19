using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Jira.FlowCharts.IssuesGrid;
using Jira.FlowCharts.JiraUpdate;
using Jira.FlowCharts.ProjectSelector;
using Jira.FlowCharts.StoryFiltering;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public ProjectSelectorViewModel ProjectSelector { get; }

        public MainViewModel()
        {
            DisplayName = "Jira flow metrics";

            var dataPath = GetPathToData();

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            TasksSourceJiraCacheAdapter jiraCacheAdapter = new TasksSourceJiraCacheAdapter(Path.Combine(dataPath, @"issuesCache.db"));
            JsonStatesRepository statesRepository = new JsonStatesRepository(Path.Combine(dataPath, @"analysisSettings.json"));
            var stateFilteringProvider = new StateFilteringProvider(jiraCacheAdapter, statesRepository);
            var tasksSource = new TasksSource(jiraCacheAdapter);

            var issuesFrom = DateTime.Now.AddYears(-1);

            ProjectSelector = new ProjectSelectorViewModel(tasksSource);

            Items.Add(new JiraUpdateViewModel(tasksSource, new CurrentTime()));
            Items.Add(new StoryFilteringViewModel(stateFilteringProvider));
            Items.Add(new IssuesGridViewModel(tasksSource, stateFilteringProvider, ProjectSelector));
            Items.Add(new CumulativeFlowViewModel(tasksSource, stateFilteringProvider));
            Items.Add(new CycleTimeScatterplotViewModel(tasksSource, issuesFrom, stateFilteringProvider));
            Items.Add(new CycleTimeHistogramViewModel(tasksSource, issuesFrom, stateFilteringProvider));
            Items.Add(new CycleTimeHistoryViewModel(tasksSource, stateFilteringProvider));
            // not shown now
            //Items.Add(new CycleTimeHistogramSmoothViewModel(tasksSource, issuesFrom, stateFilteringProvider));
            Items.Add(new StoryPointCycleTimeViewModel(tasksSource, issuesFrom, stateFilteringProvider));
            Items.Add(new SimulationViewModel(tasksSource, issuesFrom, stateFilteringProvider));
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

            await base.OnDeactivateAsync(close, cancellationToken);
        }
    }
}
