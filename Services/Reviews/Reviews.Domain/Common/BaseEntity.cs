using MongoDB.Bson;

namespace Reviews.Domain.Common;

public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; } = ObjectId.GenerateNewId();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public int Version { get; protected set; } = 1;

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public void BumpVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }
}
