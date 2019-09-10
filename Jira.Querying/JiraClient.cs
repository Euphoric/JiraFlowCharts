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

        public async Task<IJiraIssue[]> GetIssues(string projectName, DateTime lastUpdated, int takeCount, int skipCount)
        {
            // JIRA is only sensitive to the minute, so seconds are ignored

            // use LINQ syntax to retrieve issues
            var issuesQuery = from i in _jiraRestClient.Issues.Queryable
                                     where i.Project == projectName && i.Updated > lastUpdated
                                     orderby i.Updated
                                     select i;

            var issues =
                issuesQuery
                .Skip(skipCount)
                .Take(takeCount)
                .ToArray()
                .Select(iss=>new InnerJiraIssue(iss))
                .ToArray();
                ;

            return issues;
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

            int? storyPoints = storyPointsStr == null ? 0 : int.Parse(storyPointsStr);

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
