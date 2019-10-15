namespace Jira.FlowCharts.Simulation
{
    public class NewEvent
    {
        public EventValue Value { get; }
        public Distribution Distribution { get; }

        public NewEvent(Distribution distribution, EventValue value)
        {
            Value = value;
            Distribution = distribution;
        }
    }
}
