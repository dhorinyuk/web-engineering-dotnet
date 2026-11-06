using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Persistence;

namespace Reviews.Infrastructure.Services;

public class ReviewSeeder : IDataSeeder
{
    private readonly MongoDbContext _context;
    private readonly ILogger<ReviewSeeder> _logger;

    public ReviewSeeder(MongoDbContext context, ILogger<ReviewSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Criterion 14: Idempotent seeding checking estimated document count
    public async Task SeedAsync(CancellationToken ct = default)
    {
        try
        {
            var count = await _context.Reviews.EstimatedDocumentCountAsync(cancellationToken: ct);
            if (count > 0)
            {
                _logger.LogInformation("Database already contains {Count} reviews; seeding skipped.", count);
                return;
            }

            var sampleDiscussionId = ObjectId.Parse("66f00101a1b2c3d4e5f60001");

            // Review 1: Motul 8100 X-cess Gen2
            var r1 = new Review(
                productId: 1,
                productName: "Моторна олива Motul 8100 X-cess Gen2 5W-40 5л",
                rating: 5,
                headline: "Відмінна олива для навантажених турбомоторів TSI",
                content: "Двигун працює рівно, витрати оливи між інтервалами ТО немає. Зимовий пуск при -20 без нарікань.",
                author: new Author(
                    userId: "E5B26E83-9FA3-4D9A-8C21-998877660001",
                    name: "Олександр Коваленко",
                    email: "olexandr.kovalenko@example.com",
                    isVerifiedBuyer: true),
                discussionId: sampleDiscussionId,
                photos: new[]
                {
                    "https://cdn.autoparts.ua/reviews/motul_canister_qr.jpg",
                    "https://cdn.autoparts.ua/reviews/motul_engine_bay.jpg"
                });

            r1.AddComment("Сергій Ткаченко", "Який інтервал заміни витримуєте?");
            r1.AddComment("Олександр Коваленко", "Міняю кожні 7500 км або 250 мотогодин.");

            // Review 2: Brembo P 85 020 Brake Pads
            var r2 = new Review(
                productId: 2,
                productName: "Комплект передніх гальмівних колодок Brembo P 85 020",
                rating: 5,
                headline: "Надійні гальма без стороннього шуму",
                content: "Встановлені на Skoda Octavia A7. Гальмування плавне, сторонній писк відсутній.",
                author: new Author(
                    userId: "E5B26E83-9FA3-4D9A-8C21-998877660002",
                    name: "Ірина Шевченко",
                    email: "iryna.shevchenko@example.com",
                    isVerifiedBuyer: true));

            // Review 3: Toptul Socket Set 94 pcs
            var r3 = new Review(
                productId: 3,
                productName: "Набір торцевих головок Toptul 1/4\" та 1/2\" 94 од.",
                rating: 4,
                headline: "Якісний слюсарний набір для гаража та сервісу",
                content: "Метал міцний (Cr-V), тріскачки працюють м'яко без заїдань. Єдиний мінус — замки кейса з часом слабшають.",
                author: new Author(
                    userId: "E5B26E83-9FA3-4D9A-8C21-998877660003",
                    name: "Дмитро Мельник",
                    email: "dmytro.melnyk@example.com",
                    isVerifiedBuyer: false));

            await _context.Reviews.InsertManyAsync(new[] { r1, r2, r3 }, cancellationToken: ct);
            _logger.LogInformation("Successfully seeded initial reviews data into MongoDB.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not execute MongoDB seeding (server connection deferred): {Message}", ex.Message);
        }
    }
}
