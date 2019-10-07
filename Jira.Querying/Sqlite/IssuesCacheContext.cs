using Microsoft.EntityFrameworkCore;

namespace Jira.Querying.Sqlite
{
    class IssuesCacheContext : DbContext
    {
        public DbSet<CachedIssueDb> Issues { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("DataSource=:memory:"); // TODO: use connection string from constructor

            base.OnConfiguring(optionsBuilder);
        }
    }
}
