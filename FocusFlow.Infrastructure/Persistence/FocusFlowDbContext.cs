using FocusFlow.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FocusFlow.Infrastructure.Persistence
{
    public sealed class FocusFlowDbContext : DbContext
    {
        public FocusFlowDbContext(DbContextOptions<FocusFlowDbContext> options) : base(options) { }

        public DbSet<Email> Emails => Set<Email>();
        public DbSet<Summary> Summaries => Set<Summary>();
        public DbSet<FocusTask> Tasks => Set<FocusTask>();
        public DbSet<Reminder> Reminders => Set<Reminder>();
        public DbSet<EmailAccount> EmailAccounts => Set<EmailAccount>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);


            b.Entity<Email>(cfg =>
            {
                cfg.ToTable("Emails");
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.From).HasMaxLength(320);
                cfg.Property(x => x.Subject).HasMaxLength(500);
                cfg.Property(x => x.Provider).HasConversion<string>().HasMaxLength(50);
                cfg.Property(x => x.ExternalMessageId).HasMaxLength(256);
                cfg.HasIndex(x => x.ReceivedUtc);
                cfg.HasIndex(x => x.EmailAccountId);
                cfg.HasIndex(x => x.ExternalMessageId);
                cfg.HasOne(x => x.EmailAccount)
                   .WithMany(a => a.Emails)
                   .HasForeignKey(x => x.EmailAccountId)
                   .OnDelete(DeleteBehavior.SetNull);
            });


            b.Entity<Summary>(cfg =>
            {
                cfg.ToTable("Summaries");
                cfg.HasKey(x => x.Id);
                cfg.HasIndex(x => x.EmailId).IsUnique(false);
                cfg.Property(x => x.Text).HasColumnType("TEXT");
            });


            b.Entity<FocusTask>(cfg =>
            {
                cfg.ToTable("Tasks");
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.Title).HasMaxLength(300);
                cfg.HasIndex(x => new { x.IsDone, x.DueUtc });
            });

            b.Entity<Reminder>(cfg =>
            {
                cfg.ToTable("Reminders");
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.Title).HasMaxLength(300);
                cfg.HasIndex(x => new { x.Fired, x.FireAtUtc });
            });

            b.Entity<EmailAccount>(cfg =>
            {
                cfg.ToTable("EmailAccounts");
                cfg.HasKey(x => x.Id);
                cfg.Property(x => x.EmailAddress).HasMaxLength(320).IsRequired();
                cfg.Property(x => x.DisplayName).HasMaxLength(200);
                cfg.Property(x => x.Provider).HasConversion<string>().HasMaxLength(50);
                cfg.Property(x => x.AccessToken).HasColumnType("TEXT");
                cfg.Property(x => x.RefreshToken).HasColumnType("TEXT");
                cfg.HasIndex(x => x.EmailAddress);
                cfg.HasIndex(x => x.Provider);
            });
        }
    }
}

