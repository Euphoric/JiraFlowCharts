using System;
using System.Collections.Generic;
using System.Linq;
using C5;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;

namespace Jira.FlowCharts.Simulation
{
    public class FlowSimulation
    {
        private readonly List<int> _storiesInProgressSample;
        private readonly System.Collections.Generic.HashSet<int> _storiesInProgress;
        private readonly List<double> _simulatedStoryCycleTimes;
        private readonly double _newStoryRate;
        private readonly double[] _storyCycleTimes;
        private readonly int _expectedCompletedStories;

        private int _storyIdCounter;

        public double AverageWorkInProgress { get; private set; }
        public double SimulationTime { get; private set; }

        public FlowSimulation(double newStoryRate, double[] storyCycleTimes, int expectedCompletedStories)
        {
            _newStoryRate = newStoryRate;
            _storyCycleTimes = storyCycleTimes;
            _expectedCompletedStories = expectedCompletedStories;

            _storyIdCounter = 0;
            _simulatedStoryCycleTimes = new List<double>();
            _storiesInProgress = new System.Collections.Generic.HashSet<int>();
            _storiesInProgressSample = new List<int>();
        }

        public void Run()
        {
            IPriorityQueue<Event> events = new IntervalHeap<Event>();

            SystemRandomSource random = new SystemRandomSource();
            Exponential nextStoryStartedDistribution = new Exponential(_newStoryRate, random);

            events.Add(new Event(0, 0, new EventValue(EventType.NewStory)));
            events.Add(new Event(0, 0, new EventValue(EventType.Sample)));

            SimulationTime = 0;

            int? stopOnCompletedStories = null;

            double simulationWarmupDays = 30;

            while (!events.IsEmpty)
            {
                if (stopOnCompletedStories == null && SimulationTime > simulationWarmupDays)
                {
                    stopOnCompletedStories = _simulatedStoryCycleTimes.Count + _expectedCompletedStories;
                }

                if (_simulatedStoryCycleTimes.Count >= stopOnCompletedStories)
                {
                    break;
                }

                var evnt = events.DeleteMin();

                SimulationTime = evnt.Time;

                var newEvents = ProcessEvent(evnt);

                foreach (var newEvent in newEvents)
                {
                    double timeOffset;
                    switch (newEvent.Distribution)
                    {
                        case Distribution.Immediate:
                            timeOffset = 0;
                            break;
                        case Distribution.Unit:
                            timeOffset = 1;
                            break;
                        case Distribution.NextStory:
                            timeOffset = nextStoryStartedDistribution.Sample();
                            break;
                        case Distribution.StoryCycleTime:
                            timeOffset = _storyCycleTimes[random.Next(0, _storyCycleTimes.Length)];
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    events.Add(new Event(SimulationTime + timeOffset, SimulationTime, newEvent.Value));
                }
            }

            SimulationTime -= simulationWarmupDays;
            AverageWorkInProgress = _storiesInProgressSample.Average();
        }

        private IEnumerable<NewEvent> ProcessEvent(Event evnt)
        {
            var eventValue = evnt.Value;

            List<NewEvent> newEvents = new List<NewEvent>();

            switch (eventValue.Type)
            {
                case EventType.NewStory:
                    newEvents.Add(new NewEvent(Distribution.NextStory, new EventValue(EventType.NewStory)));
                    var storyId = _storyIdCounter++;
                    _storiesInProgress.Add(storyId);
                    newEvents.Add(new NewEvent(Distribution.StoryCycleTime, new EventValue(EventType.StoryFinish, storyId)));
                    break;
                case EventType.StoryFinish:
                    var cycleTime = evnt.Time - evnt.StartTime;
                    _simulatedStoryCycleTimes.Add(cycleTime);
                    _storiesInProgress.Remove(eventValue.StoryId);
                    break;
                case EventType.Sample:
                    newEvents.Add(new NewEvent(Distribution.Unit, new EventValue(EventType.Sample)));
                    _storiesInProgressSample.Add(_storiesInProgress.Count);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return newEvents;
        }
    }
}
