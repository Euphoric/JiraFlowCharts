using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using ReactiveUI;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Jira.FlowCharts
{
    public class SimulationViewModel : ReactiveScreen
    {
        private FlowIssue[] _flowTasks;
        private double storyCreationRate;
        private int simulatedStoriesCount;

        public double StoryCreationRate { get => storyCreationRate; private set => this.RaiseAndSetIfChanged(ref storyCreationRate, value); }

        private readonly ObservableAsPropertyHelper<Simulation.FlowSimulationStatisticOutput> _simulationOutput;
        public Simulation.FlowSimulationStatisticOutput SimulationOutput => _simulationOutput.Value;

        private readonly ObservableAsPropertyHelper<SeriesCollection> _seriesCollection;
        public SeriesCollection SeriesCollection => _seriesCollection.Value;

        private readonly ObservableAsPropertyHelper<string[]> _labels;
        public string[] Labels => _labels.Value;

        public ReactiveCommand<Unit, Simulation.FlowSimulationStatisticOutput> RunSimulation { get; }

        public int SimulatedStoriesCount
        {
            get => simulatedStoriesCount;
            set => this.RaiseAndSetIfChanged(ref simulatedStoriesCount, value);
        }

        public SimulationViewModel(FlowIssue[] flowTasks)
        {
            _flowTasks = flowTasks;

            DisplayName = "Simulation";

            SimulatedStoriesCount = 10;

            RunSimulation = ReactiveCommand.CreateFromTask(RunSimulationInner);
            RunSimulation.ToProperty(this, nameof(SimulationOutput), out _simulationOutput);

            this
                .WhenAnyValue(x => x.SimulationOutput)
                .Select(SeriesCollectionFromHistogram)
                .ToProperty(this, nameof(SeriesCollection), out _seriesCollection);

            this.WhenAnyValue(x => x.SimulationOutput)
                .Select(LabelsFromHistogram)
                .ToProperty(this, nameof(Labels), out _labels);
        }

        private async Task<Simulation.FlowSimulationStatisticOutput> RunSimulationInner()
        {
            var finishedStories = _flowTasks.Where(x => x.IsDone).ToArray();

            var startTime = finishedStories.Max(x => x.End).AddMonths(-6);

            var stories =
                finishedStories.Where(x => x.Start > startTime)
                .ToArray();

            var from = stories.Min(x => x.Start);
            var to = stories.Max(x => x.End);

            StoryCreationRate = stories.Count() / (to - from).TotalDays;
            var cycleTimes = stories.Select(x => x.Duration).ToArray();

            Simulation.FlowSimulationStatisticOutput simStats = await Task.Run(() => Simulation.FlowSimulationStatistics.RunSimulationStatistic(StoryCreationRate, cycleTimes, 20000, SimulatedStoriesCount));

            return simStats;
        }

        private SeriesCollection SeriesCollectionFromHistogram(Simulation.FlowSimulationStatisticOutput simStats)
        {
            if (simStats == null)
                return null;

            return new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Final time count",
                    Values = new ChartValues<double>(simStats.HistogramValues.Select(x=>(double)x))
                }
            };
        }

        private string[] LabelsFromHistogram(Simulation.FlowSimulationStatisticOutput simStats)
        {
            if (simStats == null)
                return null;

            return simStats.HistogramLabels.Select(x => x.ToString("F0")).ToArray();
        }
    }
}
