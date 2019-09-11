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
            string projectName = "AC"; // TODO : Parametrize project

            int itemPaging = 0;
            while (true)
            {
                const int QueryLimit = 50;

                IJiraIssue[] issues = await _client.GetIssues(projectName, _startUpdateDate, QueryLimit, itemPaging);

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
    }
}
