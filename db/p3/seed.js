// Reviews bounded context idempotent seed data (MongoDB)

const targetDb = db.getSiblingDB("ReviewsServiceDb");

// 1. Seed discussion thread entity for cross-collection referencing
const sampleDiscussionId = ObjectId("66f00101a1b2c3d4e5f60001");

targetDb.discussions.updateOne(
    { _id: sampleDiscussionId },
    {
        $set: {
            _id: sampleDiscussionId,
            productId: 1,
            topic: "Обговорення сумісності Motul 8100 X-cess з допусками VW 502.00 / 505.00",
            status: "Open",
            messages: [
                {
                    author: "Олександр Коваленко",
                    messageText: "Чи підійде для мотора 2.0 TSI Gen3 на Passat B8?",
                    sentAt: new Date("2026-02-12T18:40:00Z")
                },
                {
                    author: "AutoParts Tech Support",
                    messageText: "Так, офіційний допуск VW 502 00 повністю відповідає регламенту обслуговування цього двигуна.",
                    sentAt: new Date("2026-02-12T19:05:00Z")
                }
            ],
            createdAt: new Date("2026-02-12T18:40:00Z")
        }
    },
    { upsert: true }
);

// 2. Review 1: Rich document demonstrating embedding (author, comments), referencing (discussionId), and photos
targetDb.reviews.updateOne(
    { productId: 1, "author.name": "Олександр Коваленко" },
    {
        $set: {
            productId: 1,
            productName: "Моторна олива Motul 8100 X-cess Gen2 5W-40 5л",
            rating: 5,
            headline: "Відмінна олива для навантажених турбомоторів TSI",
            content: "Двигун працює рівно, витрати оливи між інтервалами ТО немає. Зимовий пуск при -20 без нарікань.",
            author: {
                userId: "E5B26E83-9FA3-4D9A-8C21-998877660001",
                name: "Олександр Коваленко",
                email: "olexandr.kovalenko@example.com",
                isVerifiedBuyer: true
            },
            photos: [
                "https://cdn.autoparts.ua/reviews/motul_canister_qr.jpg",
                "https://cdn.autoparts.ua/reviews/motul_engine_bay.jpg"
            ],
            comments: [
                {
                    commentId: "c-101",
                    authorName: "Сергій Ткаченко",
                    commentText: "Який інтервал заміни витримуєте?",
                    createdAt: new Date("2026-02-13T09:15:00Z")
                },
                {
                    commentId: "c-102",
                    authorName: "Олександр Коваленко",
                    commentText: "Міняю кожні 7500 км або 250 мотогодин.",
                    createdAt: new Date("2026-02-13T09:40:00Z")
                }
            ],
            discussionId: sampleDiscussionId,
            createdAt: new Date("2026-02-12T18:35:00Z"),
            updatedAt: new Date("2026-02-12T18:35:00Z")
        }
    },
    { upsert: true }
);

// 3. Review 2: Flexible schema demonstration (minimal document: no photos array, empty comments, no discussionId)
targetDb.reviews.updateOne(
    { productId: 2, "author.name": "Ірина Шевченко" },
    {
        $set: {
            productId: 2,
            productName: "Комплект передніх гальмівних колодок Brembo P 85 020",
            rating: 5,
            headline: "Надійні гальма без стороннього шуму",
            content: "Встановлені на Skoda Octavia A7. Гальмування плавне, сторонній писк відсутній.",
            author: {
                userId: "E5B26E83-9FA3-4D9A-8C21-998877660002",
                name: "Ірина Шевченко",
                email: "iryna.shevchenko@example.com",
                isVerifiedBuyer: true
            },
            comments: [],
            discussionId: null,
            createdAt: new Date("2026-02-14T10:15:00Z"),
            updatedAt: new Date("2026-02-14T10:15:00Z")
        }
    },
    { upsert: true }
);

// 4. Seed aggregate rating summaries (updating both product_ratings_summary and ratings)
const ratingSummaries = [
    {
        productId: 1,
        averageRating: 5.0,
        totalRatings: 1,
        breakdown: { star1: 0, star2: 0, star3: 0, star4: 0, star5: 1 },
        updatedAt: new Date("2026-02-12T18:35:00Z")
    },
    {
        productId: 2,
        averageRating: 4.8,
        totalRatings: 5,
        breakdown: { star1: 0, star2: 0, star3: 0, star4: 1, star5: 4 },
        updatedAt: new Date("2026-02-14T10:15:00Z")
    }
];

for (const summary of ratingSummaries) {
    targetDb.product_ratings_summary.updateOne(
        { productId: summary.productId },
        { $set: summary },
        { upsert: true }
    );
    targetDb.ratings.updateOne(
        { productId: summary.productId },
        { $set: summary },
        { upsert: true }
    );
}
