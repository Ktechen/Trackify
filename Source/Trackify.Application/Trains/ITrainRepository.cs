using Trackify.Application.Common;

namespace Trackify.Application.Trains;

/// <summary>
/// Repository for the configured trains — the default CRUD from <see cref="IBaseRepository{T}"/> is
/// all the front-ends need today; train-specific queries can be added here later. The concrete
/// implementation (SQLite via EF Core) is an Infrastructure concern selected at composition time.
/// </summary>
public interface ITrainRepository : IBaseRepository<Train>;
