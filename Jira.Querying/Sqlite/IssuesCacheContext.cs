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
