namespace Jira.FlowCharts.Simulation
{
    public class EventValue
    {
        public EventType Type { get; }
        public int StoryId { get; }

        public EventValue(EventType type, int storyId = -1)
        {
            Type = type;
            StoryId = storyId;
        }
    }
}
