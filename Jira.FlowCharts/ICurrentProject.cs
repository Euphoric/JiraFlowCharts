using System;

namespace Jira.FlowCharts
{
    public interface ICurrentProject
    {
        string ProjectKey { get; }

        event EventHandler<EventArgs> ProjectKeyChanged;
    }
}