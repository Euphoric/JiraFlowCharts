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
        public MainViewModel()
        {
            DisplayName = "Jira flow metrics";

            TasksSource source = new TasksSource(new TasksSourceJiraCacheAdapter());
            
            Items.Add(new JiraUpdateViewModel(source, new CurrentTime()));
            Items.Add(new StoryFilteringViewModel(source));
            Items.Add(new CumulativeFlowViewModel(source));
            Items.Add(new CycleTimeScatterplotViewModel(source));
            Items.Add(new CycleTimeHistogramViewModel(source));
            Items.Add(new StoryPointCycleTimeViewModel(source));
            Items.Add(new SimulationViewModel(source));
        }
    }
}
