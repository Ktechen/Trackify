using Trackify.Application.Common;

namespace Trackify.Application.Trains;

/// <summary>
/// Repository for the planned track segments — mirrors <see cref="ITrainRepository"/>: the default
/// CRUD from <see cref="IBaseRepository{T}"/> is all the front-ends need today.
/// </summary>
public interface ITrackSegmentRepository : IBaseRepository<TrackSegment>;
