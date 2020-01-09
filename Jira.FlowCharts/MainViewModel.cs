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
        private readonly TasksSource _tasksSource;

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
            _tasksSource = new TasksSource(jiraCacheAdapter, statesRepository);
            _tasksSource.IssuesFrom = DateTime.Now.AddYears(-1);

            Items.Add(new JiraUpdateViewModel(_tasksSource, new CurrentTime()));
            Items.Add(new StoryFilteringViewModel(_tasksSource));
            Items.Add(new IssuesGridViewModel(_tasksSource));
            Items.Add(new CumulativeFlowViewModel(_tasksSource));
            Items.Add(new CycleTimeScatterplotViewModel(_tasksSource));
            Items.Add(new CycleTimeHistogramViewModel(_tasksSource));
            Items.Add(new CycleTimeHistoryViewModel(_tasksSource));
            // not shown now
            //Items.Add(new CycleTimeHistogramSmoothViewModel(_tasksSource));
            Items.Add(new StoryPointCycleTimeViewModel(_tasksSource));
            Items.Add(new SimulationViewModel(_tasksSource));
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
            await _tasksSource.ReloadStates();
        }
    }
}
