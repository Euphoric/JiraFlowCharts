using System;
using Jira.FlowCharts.JiraUpdate;

namespace Jira.FlowCharts
{
    public class CurrentTime : ICurrentTime
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
