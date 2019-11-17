using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Jira.FlowCharts.JiraUpdate;
using Jira.FlowCharts.StoryFiltering;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private TasksSource _tasksSource;

        public MainViewModel()
        {
            DisplayName = "Jira flow metrics";

            string dataPath = @"../../../Data";

            TasksSourceJiraCacheAdapter jiraCacheAdapter = new TasksSourceJiraCacheAdapter(Path.Combine(dataPath, @"issuesCache.db"));
            JsonStatesRepository statesRepository = new JsonStatesRepository(Path.Combine(dataPath, @"analysisSettings.json"));
            _tasksSource = new TasksSource(jiraCacheAdapter, statesRepository);
            
            Items.Add(new JiraUpdateViewModel(_tasksSource, new CurrentTime()));
            Items.Add(new StoryFilteringViewModel(_tasksSource));
            Items.Add(new CumulativeFlowViewModel(_tasksSource));
            Items.Add(new CycleTimeScatterplotViewModel(_tasksSource));
            Items.Add(new CycleTimeHistogramViewModel(_tasksSource));
            Items.Add(new CycleTimeHistogramSmoothViewModel(_tasksSource));
            Items.Add(new StoryPointCycleTimeViewModel(_tasksSource));
            Items.Add(new SimulationViewModel(_tasksSource));
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await _tasksSource.ReloadStates();
        }
    }
}
