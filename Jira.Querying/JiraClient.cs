﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Atlassian.Jira;

namespace Jira.Querying
{
    public class JiraClient : IJiraClient
    {
        readonly Atlassian.Jira.Jira _jiraRestClient;

        public JiraClient(JiraLoginParameters loginParameters)
            :this(loginParameters.JiraUrl, loginParameters.JiraUsername, loginParameters.PasswordAsNakedString())
        {
        }

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
                .Select(iss=>(IJiraIssue)new InnerJiraIssue(iss))
                .ToArray();

            return Task.FromResult(innerIssues);
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

        public Task<CachedIssue> RetrieveDetails(IJiraIssue issue)
        {
            return RetrieveTaskData(((InnerJiraIssue)issue).Issue);
        }

        /// <summary>
        /// feature flag for loading labels from issues as status changes
        /// </summary>
        private static bool LoadLabels = false;

        private async Task<CachedIssue> RetrieveTaskData(Issue issue)
        {
            Console.WriteLine($"Retrieving issue {issue.Key.Value}");

            var changeLog = (await issue.GetChangeLogsAsync()).ToList();
            var issueChanges =
                changeLog
                    .SelectMany(log => log.Items.Select(item => new {log, item}))
                    .Where(x => x.item.FieldName == "status" )
                    .Select(x => new CachedIssueStatusChange(x.log.CreatedDate, x.item.ToValue))
                    .ToArray();

            if (LoadLabels)
            {
                var labelChanges =
                    changeLog
                        .SelectMany(log => log.Items.Select(item => new {log, item}))
                        .Where(x => x.item.FieldName == "labels")
                        .SelectMany(x => ParseLabelChange(x.item.FromValue, x.item.ToValue).Select(lb => new {x.log, lb}))
                        .Select(x => new CachedIssueStatusChange(x.log.CreatedDate, x.lb))
                        .ToArray();

                issueChanges =
                    issueChanges
                        .Concat(labelChanges)
                        .ToArray();
            }

            issueChanges = issueChanges.OrderBy(x => x.ChangeTime).ToArray();

            string storyPointsStr = issue.CustomFields.SingleOrDefault(x => x.Name == "Story Points")?.Values
                ?.SingleOrDefault();

            int? storyPoints = null;
            if (storyPointsStr != null && int.TryParse(storyPointsStr, out var storyPointInt))
            {
                storyPoints = storyPointInt;
            }

            return new CachedIssue
            {
                Project = issue.Project,
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
                StatusChanges = new Collection<CachedIssueStatusChange>(issueChanges),
                StoryPoints = storyPoints,
            };
        }

        private IEnumerable<string> ParseLabelChange(string fromValue, string toValue)
        {
            var fromLabels = fromValue?.Split(' ') ?? new string[0];
            var toLabels = toValue?.Split(' ') ?? new string[0];

            var removedLabels = fromLabels.Except(toLabels).Select(x => "Remove_" + x).ToArray();
            var addedLabels = toLabels.Except(fromLabels).Select(x => "Add_" + x).ToArray();

            return removedLabels.Concat(addedLabels);
        }
    }
}
