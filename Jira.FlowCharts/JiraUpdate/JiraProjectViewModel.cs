namespace Jira.FlowCharts.JiraUpdate
{
    public class JiraProjectViewModel
    {
        public JiraProjectViewModel(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }
}