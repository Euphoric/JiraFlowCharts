using System;
using Atlassian.Jira;

namespace Jira.Querying
{
    public interface IJiraIssue
    {
        DateTime? Updated { get; }
    }
}
