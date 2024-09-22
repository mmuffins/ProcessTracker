using Microsoft.EntityFrameworkCore;
using ProcessTrackerService.Core.Entities;

namespace ProcessTrackerService.Infrastructure.Data
{
    public class PTServiceContext : DbContext
    {
        public PTServiceContext(DbContextOptions<PTServiceContext> options)
            : base(options)
        {
        }

        // table names
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Filter> Filters { get; set; }
        public DbSet<TagSession> TagSessions { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<TagSessionSummary> TagSessionSummary { get; set; }

        // table columns defination and descrition 
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Name)
                    .HasMaxLength(200);

                entity.Property(e => e.Inactive).HasDefaultValue(false);
            });


            modelBuilder.Entity<Filter>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(e => e.Id)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.TagId);

                entity.Property(e => e.FilterType)
                .HasMaxLength(50);

                entity.Property(e => e.FieldType)
                .HasMaxLength(50);

                entity.Property(e => e.FieldValue)
                .HasMaxLength(500);

                entity.Property(e => e.Inactive).HasDefaultValue(false);

                entity.HasOne(d => d.Tag)
               .WithMany(p => p.Filters)
               .HasForeignKey(d => d.TagId);
            });

            modelBuilder.Entity<TagSession>(entity =>
            {
                entity.HasKey(x => x.SessionId);

                entity.Property(e => e.SessionId)
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.TagId);

                entity.Property(e => e.StartTime);

                entity.Property(e => e.LastUpdateTime);

                entity.Property(e => e.EndTime);

                entity.HasOne(d => d.Tag)
                .WithMany(p => p.TagSessions)
                .HasForeignKey(d => d.TagId);
            });

            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasKey(x => x.SettingName);

                entity.Property(e => e.SettingName).ValueGeneratedNever().HasMaxLength(100);

                entity.Property(e => e.Value).HasMaxLength(100).IsRequired();
            });

            modelBuilder.Entity<TagSessionSummary>(entity =>
            {
                entity.HasKey(x => x.SummaryId);

                entity.Property(e => e.SummaryId).ValueGeneratedOnAdd();

                entity.Property(e => e.Day);

                entity.Property(e => e.TagId);

                entity.Property(e => e.Seconds);

                entity.HasOne(d => d.Tag)
                .WithMany(p => p.TagSessionSummaries)
                .HasForeignKey(d => d.TagId);
            });
        }
    }
}
