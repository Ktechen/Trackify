using Microsoft.EntityFrameworkCore;

namespace Trackify.Infrastructure.Persistence;

/// <summary>
/// EF Core + SQLite repository for the planned track segments — the default CRUD comes from
/// <see cref="BaseRepository{TContext, T}"/>, mirroring <see cref="SqliteTrainRepository"/>. Same
/// <c>trackify.db</c> as the trains, so both are readable from the CLI and the app.
/// </summary>
public sealed class SqliteTrackSegmentRepository(IDbContextFactory<TrackifyDbContext> dbContextFactory)
    : BaseRepository<TrackifyDbContext, TrackSegment>(dbContextFactory), ITrackSegmentRepository;
