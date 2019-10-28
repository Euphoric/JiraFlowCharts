using System;

namespace Jira.FlowCharts.JiraUpdate
{
    public interface ICurrentTime
    {
        DateTime UtcNow { get; }
    }
}
