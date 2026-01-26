using FocusFlow.Core.Application.Contracts.Services;
using FocusFlow.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FocusFlow.Infrastructure.Persistence
{
    public sealed class FocusFlowDbContext : DbContext
    {
        private readonly ValueConverter<string, string> _tokenConverter;

        public FocusFlowDbContext(
            DbContextOptions<FocusFlowDbContext> options,
            IEncryptionService encryptionService)
            : base(options)
        {
            _tokenConverter = new ValueConverter<string, string>(
                plain => string.IsNullOrEmpty(plain) ? string.Empty : encryptionService.Encrypt(plain),
                cipher => string.IsNullOrEmpty(cipher) ? string.Empty : encryptionService.Decrypt(cipher));
        }

        public DbSet<Email> Emails => Set<Email>();
        public DbSet<Summary> Summaries => Set<Summary>();
        public DbSet<FocusTask> Tasks => Set<FocusTask>();
        public DbSet<Reminder> Reminders => Set<Reminder>();
        public DbSet<EmailAccount> EmailAccounts => Set<EmailAccount>();

        public DbSet<AppSettings> AppSettings => Set<AppSettings>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<Email>(cfg =>
            {
                cfg.ToTable("Emails");
                cfg.HasKey(x => x.Id);

                cfg.Property(x => x.From).HasMaxLength(320);
                cfg.Property(x => x.Subject).HasMaxLength(500);

                cfg.Property(x => x.Category).HasMaxLength(100);
                cfg.Property(x => x.SuggestedAction).HasMaxLength(200);

                cfg.Property(x => x.Provider)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                cfg.Property(x => x.ExternalMessageId)
                   .HasMaxLength(256);

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

                cfg.Property(x => x.EmailAddress)
                   .HasMaxLength(320)
                   .IsRequired();

                cfg.Property(x => x.DisplayName)
                   .HasMaxLength(200);

                cfg.Property(x => x.Provider)
                   .HasConversion<string>()
                   .HasMaxLength(50);

                cfg.Property(x => x.AccessToken)
                   .HasColumnType("TEXT")
                   .HasConversion(_tokenConverter);

                cfg.Property(x => x.RefreshToken)
                   .HasColumnType("TEXT")
                   .HasConversion(_tokenConverter);

                cfg.HasIndex(x => x.EmailAddress);
                cfg.HasIndex(x => x.Provider);
            });

            b.Entity<AppSettings>(cfg =>
            {
                cfg.ToTable("AppSettings");
                cfg.HasKey(x => x.Id);

                cfg.Property(x => x.BriefingTasksHours).HasDefaultValue(48);
                cfg.Property(x => x.BriefingRemindersHours).HasDefaultValue(24);
                cfg.Property(x => x.BriefingEmailsDays).HasDefaultValue(2);

                cfg.Property(x => x.NotificationsEnabled).HasDefaultValue(true);
                cfg.Property(x => x.NotificationTickSeconds).HasDefaultValue(60);
                cfg.Property(x => x.ReminderUpcomingWindowMinutes).HasDefaultValue(5);
                cfg.Property(x => x.BriefingNotificationsEnabled).HasDefaultValue(true);

                cfg.Property(x => x.BriefingTimeLocal)
                   .HasDefaultValue("09:00")
                   .HasMaxLength(10);
            });
        }
    }
}
