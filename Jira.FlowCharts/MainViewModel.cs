using System.Threading;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace Jira.FlowCharts
{
    public class MainViewModel : Conductor<IScreen>.Collection.OneActive
    {
        private Task InitializeAsync()
        {
            TasksSource source = new TasksSource();

            Items.Add(new CumulativeFlowViewModel(source));
            Items.Add(new CycleTimeScatterplotViewModel(source));
            Items.Add(new CycleTimeHistogramViewModel(source));
            Items.Add(new StoryPointCycleTimeViewModel(source));
            Items.Add(new SimulationViewModel(source));

            return Task.CompletedTask;
        }

        protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            await InitializeAsync();
        }
    }
}
