using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Atlassian.Jira;

namespace Jira.Querying
{
    public class JiraLocalCache
    {
        private readonly IJiraClient _client;
        private readonly DateTime _startUpdateDate;

        public JiraLocalCache(IJiraClient client, DateTime startUpdateDate)
        {
            _client = client;
            _startUpdateDate = startUpdateDate;
        }

        public Collection<FlatIssue> Issues { get; private set; } = new Collection<FlatIssue>();

        public async Task Update()
        {
            int itemPaging = 0;
            while (true)
            {
                const int QueryLimit = 50;

                IJiraIssue[] issues = await _client.GetIssues(null, _startUpdateDate, QueryLimit, itemPaging);

                foreach (var issue in issues)
                {
                    FlatIssue flatIssue = await _client.RetrieveDetails(issue);

                    if (Issues.Any(x => x.Key == flatIssue.Key))
                        continue;

                    Issues.Add(flatIssue);
                }

                itemPaging += QueryLimit;

                if (issues.Length != QueryLimit)
                {
                    break;
                }
            }
        }

        private static DateTime? WithoutSeconds(DateTime? dateTime)
        {
            if (!dateTime.HasValue)
                return null;
            var d = dateTime.Value;
            return new DateTime(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0);
        }

        private async Task<List<FlatIssue>> RetrieveUpdatedIssues(DateTime lastUpdate)
        {
            Dictionary<string, FlatIssue> updatedIssues = new Dictionary<string, FlatIssue>();

            while (true)
            {
                Console.WriteLine("Querying for issues updated since " + lastUpdate);

                const string projectName = "AC";

                var lastUpdateCapture = lastUpdate;

                int issueTakeCount = 32;
                IJiraIssue[] allIssues;
                while (true)
                {
                    allIssues = await _client.GetIssues(projectName, lastUpdateCapture, issueTakeCount, 0);

                    if (allIssues.Length <= 1)
                    {
                        break;
                    }

                    var allChanges = allIssues.Select(x => x.Updated.Value).Select(date => new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, 0)).Distinct().ToArray();
                    if (allChanges.Length != 1)
                    {
                        break;
                    }

                    issueTakeCount *= 2;
                    Console.WriteLine($"Didn't retrieve enough issues to move last update date forward. Requerying with {issueTakeCount} issue limit");
                }

                foreach (var issue in allIssues)
                {
                    try
                    {
                        var flatIssue = await _client.RetrieveDetails(issue);
                        updatedIssues[flatIssue.Key] = flatIssue;

                        lastUpdate = issue.Updated.Value;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error retrieving issue");
                        Console.WriteLine(e);
                    }
                }

                if (allIssues.Length <= 1)
                {
                    break;
                }
            }

            return updatedIssues.Values.ToList();
        }
    }
}
