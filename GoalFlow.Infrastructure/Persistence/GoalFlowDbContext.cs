using System;
using GoalFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace GoalFlow.Infrastructure.Persistence;

/// <summary>
/// The Entity Framework Core DbContext for GoalFlow.
/// </summary>
/// <remarks>
/// Inherits from <see cref="IdentityDbContext"/> to include ASP.NET Core Identity schema
/// (users, roles, claims). Adds DbSets for domain entities such as Goals, ProgressLogs,
/// Reminders, and RefreshTokens. Fluent configuration is applied in <see cref="OnModelCreating"/>
/// to enforce constraints, conversions, and relationships.
/// </remarks>
public class GoalFlowDbContext : IdentityDbContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="GoalFlowDbContext"/>.
    /// </summary>
    /// <param name="options">The options used to configure this DbContext instance.</param>
    public GoalFlowDbContext(DbContextOptions<GoalFlowDbContext> options) : base(options) { }

    /// <summary>
    /// Table for storing user-defined goals.
    /// </summary>
    public DbSet<Goal> Goals => Set<Goal>();

    /// <summary>
    /// Table for storing progress logs linked to goals.
    /// </summary>
    public DbSet<ProgressLog> ProgressLogs => Set<ProgressLog>();

    /// <summary>
    /// Table for storing reminders linked to goals.
    /// </summary>
    public DbSet<Reminder> Reminders => Set<Reminder>();

    /// <summary>
    /// Table for storing refresh tokens associated with users.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>
    /// Applies entity configuration and constraints.
    /// </summary>
    /// <param name="b">The model builder used to configure entities.</param>
    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---- Goal -----------------------------------------------------------
        b.Entity<Goal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).IsRequired().HasMaxLength(120);
            e.Property(x => x.Priority).HasConversion<string>(); // store enums as string
            e.Property(x => x.Status).HasConversion<string>();
        });

        // ---- ProgressLog ----------------------------------------------------
        b.Entity<ProgressLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasOne<Goal>()
                .WithMany() // optional: later can add ICollection<ProgressLog> on Goal
                .HasForeignKey(x => x.GoalId)
                .OnDelete(DeleteBehavior.Cascade); // cascade delete with parent goal
        });

        // ---- Reminder -------------------------------------------------------
        b.Entity<Reminder>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Channel).HasConversion<string>(); // enum → string
            e.Property(x => x.CronExpr).IsRequired().HasMaxLength(100);
            e.HasOne<Goal>()
                .WithMany()
                .HasForeignKey(x => x.GoalId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.IsActive, x.NextRun }); // query optimization
        });

        // ---- RefreshToken ---------------------------------------------------
        b.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TokenHash).IsRequired().HasMaxLength(256);
            e.HasIndex(x => new { x.UserId, x.TokenHash }).IsUnique();
        });
    }
}
