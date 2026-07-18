namespace Trackify.Domain.Common;

/// <summary>
/// Base for persisted entities: a GUID key and created/updated timestamps (Unix ms). A record so
/// derived entities keep value equality across the whole property set.
/// </summary>
public abstract record BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public long DateCreated { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public long DateUpdated { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
