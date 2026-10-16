using Microsoft.EntityFrameworkCore;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the SQLite train store. The Domain entities stay pure (no EF attributes) —
/// all mapping is fluent here. Enums are stored as readable names, not ints.
/// </summary>
public sealed class TrackifyDbContext(DbContextOptions<TrackifyDbContext> options) : DbContext(options)
{
    public DbSet<Train> Trains => Set<Train>();

    public DbSet<TrackPart> TrackParts => Set<TrackPart>();

    public DbSet<TrackLink> TrackLinks => Set<TrackLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Train>().HasKey(train => train.Id);

        modelBuilder.Entity<TrackPart>(part =>
        {
            part.HasKey(entity => entity.Id);
            part.HasIndex(entity => entity.PlanOrder);

            // A point's configuration is one nested document, not a second table: it is only ever
            // read and written together with its part, and a rule list is far clearer as JSON than
            // as a join nobody queries.
            part.OwnsOne(entity => entity.Switch, config =>
            {
                config.ToJson();
                config.Ignore(switchConfig => switchConfig.IsMotorised);
                config.Ignore(switchConfig => switchConfig.HasSensor);
                config.OwnsMany(switchConfig => switchConfig.EntryRules);
            });
        });

        modelBuilder.Entity<TrackLink>(link =>
        {
            link.HasKey(entity => entity.Id);
            link.HasIndex(entity => entity.FromPartId);
            link.HasIndex(entity => entity.ToPartId);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        => configurationBuilder.Properties<Enum>().HaveConversion<string>();
}
