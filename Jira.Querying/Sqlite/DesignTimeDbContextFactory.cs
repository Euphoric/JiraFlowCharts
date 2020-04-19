using Microsoft.EntityFrameworkCore.Design;

namespace Jira.Querying.Sqlite
{
    /// <summary>
    /// Factory used by EF to create migrations code.
    /// </summary>
    // ReSharper disable once UnusedType.Global
    internal class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<IssuesCacheContext>
    {
        public IssuesCacheContext CreateDbContext(string[] args)
        {

            var dbContextOptions = SqliteJiraLocalCacheRepository.CreateSqliteOptions(null);
            return new IssuesCacheContext(dbContextOptions);
        }
    }
}