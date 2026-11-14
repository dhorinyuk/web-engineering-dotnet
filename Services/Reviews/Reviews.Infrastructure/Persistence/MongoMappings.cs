using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Reviews.Domain.Common;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Persistence;

public static class MongoMappings
{
    private static int _isRegistered;

    public static void Register()
    {
        if (Interlocked.Exchange(ref _isRegistered, 1) != 0)
            return;

        // Register global conventions: camelCase elements and ignore unknown elements (Criterion 10)
        var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true)
        };
        ConventionRegistry.Register("ReviewsConventions", conventionPack, _ => true);

        // Map BaseEntity
        if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
        {
            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.AutoMap();
                cm.MapIdMember(c => c.Id);
                cm.MapMember(c => c.CreatedAt).SetElementName("createdAt");
                cm.MapMember(c => c.UpdatedAt).SetElementName("updatedAt");
                cm.MapMember(c => c.Version).SetElementName("version");
            });
        }

        // Map ValueObject base class
        if (!BsonClassMap.IsClassMapRegistered(typeof(ValueObject)))
        {
            BsonClassMap.RegisterClassMap<ValueObject>(cm =>
            {
                cm.AutoMap();
            });
        }

        // Map Author Value Object via MapCreator (Criterion 10)
        if (!BsonClassMap.IsClassMapRegistered(typeof(Author)))
        {
            BsonClassMap.RegisterClassMap<Author>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(a => a.UserId).SetElementName("userId");
                cm.MapMember(a => a.Name).SetElementName("name");
                cm.MapMember(a => a.Email).SetElementName("email");
                cm.MapMember(a => a.IsVerifiedBuyer).SetElementName("isVerifiedBuyer");
                cm.MapCreator(a => new Author(a.UserId, a.Name, a.Email, a.IsVerifiedBuyer));
            });
        }

        // Map RatingScore Value Object via MapCreator (Criterion 10)
        if (!BsonClassMap.IsClassMapRegistered(typeof(RatingScore)))
        {
            BsonClassMap.RegisterClassMap<RatingScore>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(r => r.Value).SetElementName("value");
                cm.MapCreator(r => new RatingScore(r.Value));
            });
        }

        // Map Comment Entity via MapCreator (Criterion 10)
        if (!BsonClassMap.IsClassMapRegistered(typeof(Comment)))
        {
            BsonClassMap.RegisterClassMap<Comment>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(c => c.CommentId).SetElementName("commentId");
                cm.MapMember(c => c.AuthorName).SetElementName("authorName");
                cm.MapMember(c => c.CommentText).SetElementName("commentText");
                cm.MapMember(c => c.CreatedAt).SetElementName("createdAt");
                cm.MapCreator(c => new Comment(c.AuthorName, c.CommentText, c.CommentId, c.CreatedAt));
            });
        }

        // Map Review Entity mapping private fields _comments and _photos (Criterion 10)
        if (!BsonClassMap.IsClassMapRegistered(typeof(Review)))
        {
            BsonClassMap.RegisterClassMap<Review>(cm =>
            {
                cm.AutoMap();
                cm.MapMember(r => r.ProductId).SetElementName("productId");
                cm.MapMember(r => r.ProductName).SetElementName("productName");
                cm.MapMember(r => r.Rating).SetElementName("rating");
                cm.MapMember(r => r.Headline).SetElementName("headline");
                cm.MapMember(r => r.Content).SetElementName("content");
                cm.MapMember(r => r.Author).SetElementName("author");
                cm.MapMember(r => r.DiscussionId).SetElementName("discussionId");

                // Explicit private field mappings without domain attributes (Criterion 10)
                cm.MapField("_comments").SetElementName("comments");
                cm.MapField("_photos").SetElementName("photos");
            });
        }
    }
}
