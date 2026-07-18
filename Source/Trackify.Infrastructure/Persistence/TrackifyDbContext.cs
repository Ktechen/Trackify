using Microsoft.EntityFrameworkCore;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the SQLite train store. The Domain entity <see cref="TrainConfig"/> stays pure
/// (no EF attributes) — all mapping is fluent here. Enums are stored as readable names, not ints.
/// </summary>
public sealed class TrackifyDbContext(DbContextOptions<TrackifyDbContext> options) : DbContext(options)
{
    public DbSet<TrainConfig> Trains => Set<TrainConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.Entity<TrainConfig>().HasKey(train => train.Id);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<Enum>().HaveConversion<string>();
}
