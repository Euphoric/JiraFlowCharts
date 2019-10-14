using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Jira.Querying.Sqlite;
using KellermanSoftware.CompareNetObjects;
using Xunit;

namespace Jira.Querying
{
    public class SqliteJiraLocalCacheRepositoryTest
    {
        [Fact]
        public async Task Creates_database_file_when_initialized()
        {
            var databaseFile = "file.db";
            if (File.Exists(databaseFile))
            {
                File.Delete(databaseFile);
            }
            using (var repo = new SqliteJiraLocalCacheRepository(databaseFile))
            {
                await repo.Initialize();
            }

            Assert.True(File.Exists(databaseFile));
        }

        [Theory, AutoData]
        public async Task Opens_existing_database(CachedIssue issue)
        {
            var databaseFile = "file2.db";
            if (File.Exists(databaseFile))
            {
                File.Delete(databaseFile);
            }

            using (var repo = new SqliteJiraLocalCacheRepository(databaseFile))
            {
                await repo.Initialize();
                await repo.AddOrReplaceCachedIssue(issue);
            }

            using (var repo = new SqliteJiraLocalCacheRepository(databaseFile))
            {
                await repo.Initialize();
                var retrievedIssue = (await repo.GetIssues()).SingleOrDefault();

                issue.ShouldCompare(retrievedIssue);
            }
        }
    }
}