using System;

namespace Jira.FlowCharts
{
    public class IssuesFromParameters
    {
        public DateTime? IssuesFrom { get; }

        public IssuesFromParameters(DateTime? issuesFrom)
        {
            IssuesFrom = issuesFrom;
        }
    }
}