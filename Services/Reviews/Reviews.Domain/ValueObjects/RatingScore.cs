using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

public class RatingScore : ValueObject
{
    public int Value { get; }

    public RatingScore(int value)
    {
        if (value is < 1 or > 5)
            throw new DomainException($"Rating score must be between 1 and 5. Provided: {value}");

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator int(RatingScore score) => score.Value;
    public static explicit operator RatingScore(int value) => new(value);

    public override string ToString() => Value.ToString();
}
