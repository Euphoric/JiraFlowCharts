using System.Security;

namespace Jira.FlowCharts.JiraUpdate
{
    public interface IJiraUpdateView
    {
        SecureString GetLoginPassword();
    }
}