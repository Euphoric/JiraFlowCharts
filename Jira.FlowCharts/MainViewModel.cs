using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private async Task InitializeAsync()
        {
            TasksSource source = new TasksSource();

            FlowIssue[] finishedStories = await source.GetFinishedTasks();

            Items.Add(new CumulativeFlowViewModel(await source.GetAllTasks(), source.States));
            Items.Add(new CycleTimeScatterplotViewModel(finishedStories));
            Items.Add(new CycleTimeHistogramViewModel(finishedStories));
            Items.Add(new StoryPointCycleTimeViewModel(finishedStories));
            Items.Add(new SimulationViewModel(finishedStories));
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
        }
    }
}
