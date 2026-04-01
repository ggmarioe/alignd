using Alignd.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Alignd.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room>        Rooms        => Set<Room>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<VotingRound> VotingRounds => Set<VotingRound>();
    public DbSet<Vote>        Votes        => Set<Vote>();
    public DbSet<TaskItem>    TaskItems    => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
