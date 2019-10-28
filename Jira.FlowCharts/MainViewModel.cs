using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;
using Jira.FlowCharts.JiraUpdate;
using Jira.Querying;
using Jira.Querying.Sqlite;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        public MainViewModel()
        {
            var adapter = new TasksSourceJiraCacheAdapter(
                () => new SqliteJiraLocalCacheRepository(@"../../../Data/issuesCache.db"),
                jlp => new JiraClient(jlp));
            TasksSource source = new TasksSource(adapter);

            Items.Add(new JiraUpdateViewModel(source));
            Items.Add(new CumulativeFlowViewModel(source));
            Items.Add(new CycleTimeScatterplotViewModel(source));
            Items.Add(new CycleTimeHistogramViewModel(source));
            Items.Add(new StoryPointCycleTimeViewModel(source));
            Items.Add(new SimulationViewModel(source));
        }
    }
}
