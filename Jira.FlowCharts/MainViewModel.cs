using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Jira.FlowCharts.IssuesGrid;
using Jira.FlowCharts.JiraUpdate;
using Jira.FlowCharts.StoryFiltering;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private readonly StateFiltering _stateFiltering;

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

            _stateFiltering = new StateFiltering(jiraCacheAdapter, statesRepository);
            var tasksSource = new TasksSource(jiraCacheAdapter, _stateFiltering);
            var issuesFrom = DateTime.Now.AddYears(-1);

            var stateFilteringProvider = new StateFilteringProvider(_stateFiltering);

            Items.Add(new JiraUpdateViewModel(tasksSource, new CurrentTime()));
            Items.Add(new StoryFilteringViewModel(_stateFiltering));
            Items.Add(new IssuesGridViewModel(tasksSource, stateFilteringProvider));
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

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await _stateFiltering.ReloadStates();
        }
    }
}
