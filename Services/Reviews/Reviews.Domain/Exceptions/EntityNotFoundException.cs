namespace Reviews.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string name, object key)
        : base($"Entity '{name}' with identifier '{key}' was not found.")
    {
    }

    public EntityNotFoundException(string message) : base(message)
    {
    }
}
