using Microsoft.EntityFrameworkCore;

namespace Jira.Querying.Sqlite
{
    class IssuesCacheContext : DbContext
    {
        public IssuesCacheContext(DbContextOptions options)
            :base(options)
        {
        }

        public DbSet<CachedIssueDb> Issues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CachedIssueDb>().OwnsMany(p => p.StatusChanges, a =>
            {
                a.HasKey(x => x.Id);
                a.HasForeignKey(x=>x.IssueKey);
            });
        }
    }
}
