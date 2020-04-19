using System;
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
        private readonly TasksSource _taskSource;
        private double _storyCreationRate;
        private int _simulatedStoriesCount;

        public double StoryCreationRate { get => _storyCreationRate; private set => this.RaiseAndSetIfChanged(ref _storyCreationRate, value); }

        private readonly ObservableAsPropertyHelper<Simulation.FlowSimulationStatisticOutput> _simulationOutput;
        public Simulation.FlowSimulationStatisticOutput SimulationOutput => _simulationOutput.Value;

        private readonly ObservableAsPropertyHelper<SeriesCollection> _seriesCollection;
        public SeriesCollection SeriesCollection => _seriesCollection.Value;

        private readonly ObservableAsPropertyHelper<string[]> _labels;
        private DateTime _issuesFrom;
        private readonly IStateFilteringProvider _stateFilteringProvider;

        public string[] Labels => _labels.Value;

        public ReactiveCommand<Unit, Simulation.FlowSimulationStatisticOutput> RunSimulation { get; }

        public int SimulatedStoriesCount
        {
            get => _simulatedStoriesCount;
            set => this.RaiseAndSetIfChanged(ref _simulatedStoriesCount, value);
        }

        public SimulationViewModel(TasksSource taskSource, DateTime issuesFrom, IStateFilteringProvider stateFilteringProvider)
        {
            _taskSource = taskSource;
            _issuesFrom = issuesFrom;
            _stateFilteringProvider = stateFilteringProvider;
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
            var stateFilteringParameter = await _stateFilteringProvider.GetStateFilteringParameter();
            var finishedStories = await _taskSource.GetLatestFinishedStories(new IssuesFromParameters(_issuesFrom), stateFilteringParameter);

            var from = finishedStories.Min(x => x.Ended);
            var to = finishedStories.Max(x => x.Ended);

            StoryCreationRate = finishedStories.Count() / (to - from).TotalDays;
            var cycleTimes = finishedStories.Select(x => x.DurationDays).ToArray();

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
                    Values = new ChartValues<double>(simStats.HistogramValues)
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
