using System.Security;

namespace Jira.FlowCharts.JiraUpdate
{
    public class JiraLoginParameters
    {
        public string JiraUrl { get; }
        public string JiraUsername { get; }
        public SecureString JiraPassword { get; }

        public JiraLoginParameters(string jiraUrl, string jiraUsername, SecureString jiraPassword)
        {
            JiraUrl = jiraUrl;
            JiraUsername = jiraUsername;
            JiraPassword = jiraPassword;
        }
    }
}