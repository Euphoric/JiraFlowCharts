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
        private readonly List<double> _storyCycleTimes;
        private readonly double _newStoryRate;

        private int _storyIdCounter;

        public double AverageWorkInProgress { get; private set; }
        public double SimulationTime { get; private set; }

        public FlowSimulation(double newStoryRate)
        {
            _newStoryRate = newStoryRate;
            _storyIdCounter = 0;
            _storyCycleTimes = new List<double>();
            _storiesInProgress = new System.Collections.Generic.HashSet<int>();
            _storiesInProgressSample = new List<int>();
        }

        public void Run()
        {
            IPriorityQueue<Event> events = new IntervalHeap<Event>();

            SystemRandomSource random = new SystemRandomSource();
            Exponential nextStoryStartedDistribution = new Exponential(_newStoryRate, random);
            LogNormal storyCycleTimeDistribution = new LogNormal(2, 1, random);

            events.Add(new Event(0, 0, new EventValue(EventType.NewStory)));
            events.Add(new Event(0, 0, new EventValue(EventType.Sample)));

            int expectedFinishedStories = 20;

            SimulationTime = 0;

            while (!events.IsEmpty)
            {
                //if (currentTime >= 30 * 3)
                //{
                //    break;
                //}

                if (_storyCycleTimes.Count >= expectedFinishedStories)
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
                            timeOffset = storyCycleTimeDistribution.Sample();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    events.Add(new Event(SimulationTime + timeOffset, SimulationTime, newEvent.Value));
                }
            }

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
                    _storyCycleTimes.Add(cycleTime);
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
