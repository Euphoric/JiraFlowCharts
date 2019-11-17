using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Jira.FlowCharts.JiraUpdate;
using Jira.FlowCharts.StoryFiltering;
using Jira.Querying;
using Jira.Querying.Sqlite;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private TasksSource _tasksSource;

        public MainViewModel()
        {
            DisplayName = "Jira flow metrics";

            string[] filteredStates = new[] { "Ready For Dev", "In Dev", "Ready for Peer Review", "Ready for QA", "In QA", "Ready for Done", "Done" };
            string[] resetStates = new[] { "On Hold", "Not Started", "Withdrawn" };

            _tasksSource = new TasksSource(new TasksSourceJiraCacheAdapter(), new MemoryStatesRepository(filteredStates, resetStates));
            
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
