using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Atlassian.Jira;

namespace Jira.Querying
{
    public class JiraClient : IJiraClient
    {
        readonly Atlassian.Jira.Jira _jiraRestClient;

        public JiraClient(string site, string username, string password)
        {
            _jiraRestClient = Atlassian.Jira.Jira.CreateRestClient(site, username, password);
        }

        public Task<IJiraIssue[]> GetIssues(string projectName, DateTime lastUpdated, int takeCount, int skipCount)
        {
            // JIRA is only sensitive to the minute, so seconds are ignored

            var issues = _jiraRestClient.Issues.Queryable
                .Where(i => i.Project == projectName && i.Updated > lastUpdated)
                .OrderBy(i => i.Updated)
                .ThenBy(i => i.Key)
                .Skip(skipCount)
                .Take(takeCount)
                .ToArray();

            Console.WriteLine($"Retrieved {issues.Length} issues.");
            if (issues.Length > 0)
            {
                Console.WriteLine($"Issues last update time from {issues.Min(x=>x.Updated)} to {issues.Max(x=>x.Updated)}");
            }

            var innerIssues =
                issues
                .Select(iss=>new InnerJiraIssue(iss))
                .ToArray();

            return Task.FromResult<IJiraIssue[]>(innerIssues);
        }

        private class InnerJiraIssue : IJiraIssue
        {
            public InnerJiraIssue(Issue issue)
            {
                Issue = issue;
            }

            public DateTime? Updated => Issue.Updated;

            public Issue Issue { get; }
        }

        public Task<FlatIssue> RetrieveDetails(IJiraIssue issue)
        {
            return RetrieveTaskData(((InnerJiraIssue)issue).Issue);
        }

        private async Task<FlatIssue> RetrieveTaskData(Issue issue)
        {
            Console.WriteLine($"Retrieving issue {issue.Key.Value}");

            var changeLog = await issue.GetChangeLogsAsync();
            var statusChanges =
                changeLog
                    .SelectMany(log => log.Items.Select(item => new { log, item }))
                    .Where(x => x.item.FieldName == "status")
                    .Select(x => new FlatIssueStatusChange(x.log.CreatedDate, x.item.ToValue))
                    .OrderBy(x => x.ChangeTime)
                    .ToArray();

            string storyPointsStr = issue.CustomFields.SingleOrDefault(x => x.Name == "Story Points")?.Values
                ?.SingleOrDefault();

            int? storyPoints = null;
            if (storyPointsStr != null && int.TryParse(storyPointsStr, out var storyPointInt))
            {
                storyPoints = storyPointInt;
            }

            return new FlatIssue()
            {
                Key = issue.Key?.Value,
                Title = issue.Summary,
                Type = issue.Type?.Name,
                Resolution = issue.Resolution?.Name,
                Status = issue.Status?.Name,
                Created = issue.Created,
                Updated = issue.Updated,
                Resolved = issue.ResolutionDate,
                OriginalEstimate = issue.TimeTrackingData?.OriginalEstimateInSeconds,
                TimeSpent = issue.TimeTrackingData?.TimeSpentInSeconds,
                StatusChanges = new Collection<FlatIssueStatusChange>(statusChanges),
                StoryPoints = storyPoints,
            };
        }
    }
}
