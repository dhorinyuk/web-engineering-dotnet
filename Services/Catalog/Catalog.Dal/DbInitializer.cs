using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal;

public static class DbInitializer
{
    public static async Task SeedAsync(CatalogDbContext db)
    {
        // Programmatic seeding idempotent guard (Criterion 9)
        if (await db.Products.AnyAsync())
        {
            return;
        }

        // Product 1: Motul 8100 X-cess Gen2 5W-40 5L (BrandId = 1)
        var p1 = new Product(
            brandId: 1,
            sku: "MOT-8100-5W40-5L",
            slug: "motul-8100-xcess-gen2-5w40-5l",
            name: "Моторна олива Motul 8100 X-cess Gen2 5W-40 5л",
            price: 1850.00m,
            costPrice: 1350.00m,
            weightKg: 4.650m,
            stock: 45,
            isAvailable: true);

        p1.SetProductDetails(new ProductDetails(
            productId: 0,
            technicalDescription: "100% синтетична преміальна моторна олива для бензинових і дизельних двигунів стандарту Euro 4, 5, 6.",
            warrantyMonths: 60,
            countryOfOrigin: "France",
            dimensions: "30x20x12 cm",
            installationGuideUrl: "https://motul.com/tech/8100.pdf"));

        p1.AddImage("https://cdn.autoparts.ua/products/motul-8100-1.jpg", "Motul 8100 5W40 Каністра 5л", displayOrder: 1, isPrimary: true);
        p1.AddImage("https://cdn.autoparts.ua/products/motul-8100-back.jpg", "Зворотна сторона каністри", displayOrder: 2, isPrimary: false);
        p1.AssignCategory(categoryId: 2, isPrimary: true); // Engine oils
        p1.AssignCategory(categoryId: 1, isPrimary: false); // Oils & fluids

        // Product 2: Brembo P 85 020 Brake Pads (BrandId = 2)
        var p2 = new Product(
            brandId: 2,
            sku: "BRM-P85020",
            slug: "brembo-p-85-020-brake-pads",
            name: "Комплект передніх гальмівних колодок Brembo P 85 020",
            price: 2800.00m,
            costPrice: 2050.00m,
            weightKg: 1.820m,
            stock: 20,
            isAvailable: true);

        p2.SetProductDetails(new ProductDetails(
            productId: 0,
            technicalDescription: "Високоефективні керамічні гальмівні колодки з датчиком зношування для VAG платформ.",
            warrantyMonths: 24,
            countryOfOrigin: "Italy",
            dimensions: "15x10x8 cm",
            installationGuideUrl: "https://bremboparts.com/manuals/p85020.pdf"));

        p2.AddImage("https://cdn.autoparts.ua/products/brembo-p85020.jpg", "Гальмівні колодки Brembo P 85 020", displayOrder: 1, isPrimary: true);
        p2.AssignCategory(categoryId: 4, isPrimary: true); // Brake pads
        p2.AssignCategory(categoryId: 3, isPrimary: false); // Brakes

        // Product 3: Toptul Professional Socket Set 94 pcs (BrandId = 3)
        var p3 = new Product(
            brandId: 3,
            sku: "TOP-GCAI9401",
            slug: "toptul-gcai9401-socket-set-94pcs",
            name: "Набір торцевих головок Toptul 1/4\" та 1/2\" 94 од.",
            price: 4950.00m,
            costPrice: 3600.00m,
            weightKg: 6.500m,
            stock: 15,
            isAvailable: true);

        p3.SetProductDetails(new ProductDetails(
            productId: 0,
            technicalDescription: "Професійний набір слюсарного інструменту зі сталі Cr-V у міцному кейсі.",
            warrantyMonths: 120,
            countryOfOrigin: "Taiwan",
            dimensions: "40x30x10 cm",
            installationGuideUrl: "https://toptul.com/manuals/gcai9401.pdf"));

        p3.AddImage("https://cdn.autoparts.ua/products/toptul-case.jpg", "Набір головок Toptul у кейсі", displayOrder: 1, isPrimary: true);
        p3.AssignCategory(categoryId: 5, isPrimary: true); // Tools

        // Product 4: Bosch S4 008 74Ah Battery (BrandId = 4)
        var p4 = new Product(
            brandId: 4,
            sku: "BOS-0092S40080",
            slug: "bosch-s4-008-74ah-680a",
            name: "Акумулятор Bosch S4 Silver 74Ah 680A",
            price: 3650.00m,
            costPrice: 2800.00m,
            weightKg: 17.500m,
            stock: 12,
            isAvailable: true);

        p4.SetProductDetails(new ProductDetails(
            productId: 0,
            technicalDescription: "Необслуговуваний свинцево-кислотний акумулятор із технологією срібного легування PowerFrame.",
            warrantyMonths: 24,
            countryOfOrigin: "Germany",
            dimensions: "278x175x190 mm",
            installationGuideUrl: "https://bosch-automotive.com/s4-guide.pdf"));

        p4.AddImage("https://cdn.autoparts.ua/products/bosch-s4-008.jpg", "Акумулятор Bosch S4 74Ah", displayOrder: 1, isPrimary: true);
        p4.AssignCategory(categoryId: 6, isPrimary: true); // Batteries

        await db.Products.AddRangeAsync(p1, p2, p3, p4);
        await db.SaveChangesAsync();
    }
}
