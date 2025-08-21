using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;
using Proyecto_Apuestas.Models;

namespace Proyecto_Apuestas.Data;

public partial class apuestasDbContext : DbContext
{
    public apuestasDbContext()
    {
    }

    public apuestasDbContext(DbContextOptions<apuestasDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bet> Bets { get; set; }
    public virtual DbSet<ApiBet> ApiBets { get; set; }
    public virtual DbSet<Competition> Competitions { get; set; }
    public virtual DbSet<Event> Events { get; set; }
    public virtual DbSet<EventHasTeam> EventHasTeams { get; set; }
    public virtual DbSet<Image> Images { get; set; }
    public virtual DbSet<LoginAttempt> LoginAttempts { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<OddsHistory> OddsHistories { get; set; }
    public virtual DbSet<PaymentMethod> PaymentMethods { get; set; }
    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public virtual DbSet<ReportLog> ReportLogs { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<Sport> Sports { get; set; }
    public virtual DbSet<Team> Teams { get; set; }
    public virtual DbSet<UserAccount> UserAccounts { get; set; }
    public virtual DbSet<vw_ActiveUsersStat> vw_ActiveUsersStats { get; set; }
    public virtual DbSet<vw_UpcomingEvent> vw_UpcomingEvents { get; set; }

    // NEW: tabla de unión ApiBetUserAccount
    public virtual DbSet<ApiBetUserAccount> ApiBetUserAccounts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("name=DefaultConnection", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.41-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Bet>(entity =>
        {
            entity.HasKey(e => e.BetId).HasName("PRIMARY");

            entity.Property(e => e.BetStatus)
                .HasDefaultValueSql("'P'")
                .IsFixedLength();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.Event).WithMany(p => p.Bets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bet_Event");

            entity.HasOne(d => d.PaymentTransaction).WithMany(p => p.Bets).HasConstraintName("FK_Bet_PaymentTxn");
        });

        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasKey(e => e.CompetitionId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.Sport).WithMany(p => p.Competitions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Competition_Sport");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
        });

        modelBuilder.Entity<EventHasTeam>(entity =>
        {
            entity.HasKey(e => new { e.EventId, e.TeamId })
                .HasName("PRIMARY")
                .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

            entity.HasOne(d => d.Event).WithMany(p => p.EventHasTeams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventHasTeam_Event");

            entity.HasOne(d => d.Team).WithMany(p => p.EventHasTeams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EventHasTeam_Team");
        });

        modelBuilder.Entity<Image>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.Competition).WithMany(p => p.Images).HasConstraintName("FK_Image_Competition");

            entity.HasOne(d => d.Team).WithMany(p => p.Images).HasConstraintName("FK_Image_Team");
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("PRIMARY");

            entity.Property(e => e.AttemptTime).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.User).WithMany(p => p.LoginAttempts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LoginAttempt_UserAccount");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_UserAccount");
        });

        modelBuilder.Entity<OddsHistory>(entity =>
        {
            entity.HasKey(e => e.OddsId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.Event).WithMany(p => p.OddsHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OddsHistory_Event");

            entity.HasOne(d => d.Team).WithMany(p => p.OddsHistories)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OddsHistory_Team");
        });

        modelBuilder.Entity<PaymentMethod>(entity =>
        {
            entity.HasKey(e => e.PaymentMethodId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentMethods)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentMethod_UserAccount");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.PaymentMethod).WithMany(p => p.PaymentTransactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransaction_PaymentMethod");

            entity.HasOne(d => d.User).WithMany(p => p.PaymentTransactions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PaymentTransaction_UserAccount");
        });

        modelBuilder.Entity<ReportLog>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PRIMARY");

            entity.Property(e => e.GeneratedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.User).WithMany(p => p.ReportLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReportLog_UserAccount");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PRIMARY");
        });

        modelBuilder.Entity<Sport>(entity =>
        {
            entity.HasKey(e => e.SportId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
        });

        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(e => e.TeamId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.Sport).WithMany(p => p.Teams)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Team_Sport");
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
            entity.Property(e => e.IsActive).HasDefaultValueSql("'1'");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            entity.HasOne(d => d.Role).WithMany(p => p.UserAccounts)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserAccount_Role");

            // many-to-many existente: UserAccount <-> Bet (UserAccountHasBet)
            entity.HasMany(d => d.Bets).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserAccountHasBet",
                    r => r.HasOne<Bet>().WithMany()
                        .HasForeignKey("BetId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserAccountHasBet_Bet"),
                    l => l.HasOne<UserAccount>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_UserAccountHasBet_UserAccount"),
                    j =>
                    {
                        j.HasKey("UserId", "BetId")
                            .HasName("PRIMARY")
                            .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });
                        j.ToTable("UserAccountHasBet");
                        j.HasIndex(new[] { "BetId" }, "FK_UserAccountHasBet_Bet");
                    });
        });

        // NEW: configuración explícita de la tabla de unión ApiBetUserAccount
        modelBuilder.Entity<ApiBetUserAccount>(e =>
        {
            e.HasKey(x => new { x.ApiBetId, x.UserId });

            e.HasOne(x => x.ApiBet)
             .WithMany(b => b.ApiBetUsers)
             .HasForeignKey(x => x.ApiBetId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.User)
             .WithMany(u => u.ApiBetUsers)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // NEW: skip navigations para UserAccount.ApiBets <-> ApiBet.Users usando la MISMA tabla ApiBetUserAccount
        modelBuilder.Entity<UserAccount>()
            .HasMany(u => u.ApiBets)
            .WithMany(b => b.Users)
            .UsingEntity<ApiBetUserAccount>(
                j => j.HasOne(x => x.ApiBet)
                      .WithMany(b => b.ApiBetUsers)
                      .HasForeignKey(x => x.ApiBetId),
                j => j.HasOne(x => x.User)
                      .WithMany(u => u.ApiBetUsers)
                      .HasForeignKey(x => x.UserId),
                j =>
                {
                    j.ToTable("ApiBetUserAccount");
                    j.HasKey(x => new { x.ApiBetId, x.UserId });
                });

        modelBuilder.Entity<vw_ActiveUsersStat>(entity =>
        {
            entity.ToView("vw_ActiveUsersStats");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(3)");
        });

        modelBuilder.Entity<vw_UpcomingEvent>(entity =>
        {
            entity.ToView("vw_UpcomingEvents");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
