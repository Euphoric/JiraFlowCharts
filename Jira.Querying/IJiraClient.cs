using System;
using System.Threading.Tasks;

namespace Jira.Querying
{
    public interface IJiraClient
    {
        Task<IJiraIssue[]> GetIssues(string project, DateTime lastUpdated, int count, int skipCount);
        Task<FlatIssue> RetrieveDetails(IJiraIssue issue);
    }
}
