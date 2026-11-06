using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public class Author : ValueObject
{
    public string? UserId { get; }
    public string Name { get; }
    public string DisplayName => Name;
    public string Email { get; }
    public bool IsVerifiedBuyer { get; }

    public Author(string? userId, string name, string email, bool isVerifiedBuyer = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Author name cannot be empty.");

        if (name.Trim().Length is < 2 or > 100)
            throw new DomainException("Author name must be between 2 and 100 characters.");

        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Author email cannot be empty.");

        if (!email.Contains('@') || email.Length > 255)
            throw new DomainException("Author email is invalid.");

        UserId = string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        IsVerifiedBuyer = isVerifiedBuyer;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return Name;
        yield return Email;
        yield return IsVerifiedBuyer;
    }
}
