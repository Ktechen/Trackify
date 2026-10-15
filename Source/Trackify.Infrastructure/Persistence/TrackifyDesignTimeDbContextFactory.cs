using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations add</c> build the model without a startup project, which a class
/// library otherwise has no way to provide.
///
/// Design-time only. The connection string below is never opened — generating a migration needs the
/// SQLite provider's conventions, not a database — so the real path still comes from
/// <c>AddTrackifyInfrastructure</c> (and TRACKIFY_STORE) at runtime.
/// </summary>
public sealed class TrackifyDesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrackifyDbContext>
{
    public TrackifyDbContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<TrackifyDbContext>()
            .UseSqlite("Data Source=trackify.design-time.db")
            .Options);
}
