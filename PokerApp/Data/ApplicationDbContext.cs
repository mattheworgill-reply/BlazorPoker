// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PokerApp.Models;

namespace PokerApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PokerTable> PokerTables { get; set; } = default!;
    public DbSet<PokerGame> PokerGames { get; set; } = default!;
    public DbSet<GamePlayer> GamePlayers { get; set; } = default!;
    public DbSet<GameHistory> GameHistories { get; set; } = default!;
    public DbSet<GameHistoryPlayer> GameHistoryPlayers { get; set; } = default!;
    public DbSet<UserStats> UserStats { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure entity relationships

        // PokerTable to Owner (ApplicationUser)
        builder.Entity<PokerTable>()
            .HasOne(t => t.Owner)
            .WithMany(u => u.OwnedTables)
            .HasForeignKey(t => t.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PokerGame>()
            .HasOne(g => g.GameTable)
            .WithOne(t => t.Game)
            .HasForeignKey<PokerGame>(g => g.TableId)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PokerGame>()
            .Property(g => g.State)
            .HasConversion<int>();

        // GamePlayer relationships
        builder.Entity<GamePlayer>()
            .HasOne(p => p.User)
            .WithMany(u => u.GamePlayers)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GamePlayer>()
            .HasOne(p => p.PokerTable)
            .WithMany(t => t.Players)
            .HasForeignKey(p => p.PokerTableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GamePlayer>()
            .Property(p => p.PokerPosition)
            .HasConversion<int>();

        // GameHistory relationships
        builder.Entity<GameHistory>()
            .HasOne(h => h.PokerTable)
            .WithMany(t => t.GameHistories)
            .HasForeignKey(h => h.PokerTableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GameHistory>()
            .HasOne(h => h.Winner)
            .WithMany()
            .HasForeignKey(h => h.WinnerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // GameHistoryPlayer relationships
        builder.Entity<GameHistoryPlayer>()
            .HasOne(p => p.GameHistory)
            .WithMany(h => h.Players)
            .HasForeignKey(p => p.GameHistoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GameHistoryPlayer>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // UserStats relationship
        builder.Entity<UserStats>()
            .HasOne(s => s.User)
            .WithOne(u => u.Stats)
            .HasForeignKey<UserStats>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}