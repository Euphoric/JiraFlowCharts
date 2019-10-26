using Jira.Querying;

namespace Jira.FlowCharts.JiraUpdate
{
    public interface IJiraUpdateView
    {
        JiraLoginParameters GetLoginParameters();
    }
}