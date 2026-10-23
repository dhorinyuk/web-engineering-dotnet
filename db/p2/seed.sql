-- Catalog bounded context seed data (Microsoft SQL Server)

SET NOCOUNT ON;

-- Seed manufacturer brands
IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'MOTUL')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country)
    VALUES (N'Motul', N'MOTUL', N'France');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'BREMBO')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country)
    VALUES (N'Brembo', N'BREMBO', N'Italy');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'TOPTUL')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country)
    VALUES (N'Toptul', N'TOPTUL', N'Taiwan');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'BOSCH')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country)
    VALUES (N'Bosch', N'BOSCH', N'Germany');
END;

-- Seed category hierarchy
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'oils-fluids')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, DisplayOrder)
    VALUES (NULL, N'Мастила та автохімія', N'oils-fluids', 10);
END;

DECLARE @OilsRootId INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'oils-fluids');
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'engine-oils')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, DisplayOrder)
    VALUES (@OilsRootId, N'Моторні оливи', N'engine-oils', 11);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'brakes')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, DisplayOrder)
    VALUES (NULL, N'Гальмівна система', N'brakes', 20);
END;

DECLARE @BrakesRootId INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'brakes');
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'brake-pads')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, DisplayOrder)
    VALUES (@BrakesRootId, N'Гальмівні колодки', N'brake-pads', 21);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'tools')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, DisplayOrder)
    VALUES (NULL, N'Автоінструмент', N'tools', 30);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'batteries')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, DisplayOrder)
    VALUES (NULL, N'Акумулятори', N'batteries', 40);
END;

-- Seed product catalog with explicit surrogate keys matching distributed context references
DECLARE @MotulId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'MOTUL');
DECLARE @BremboId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'BREMBO');
DECLARE @ToptulId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'TOPTUL');
DECLARE @BoschId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'BOSCH');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, Sku, Name, Price, CostPrice, WeightKg, IsAvailable)
    VALUES (1, @MotulId, N'MOT-8100-5W40-5L', N'Моторна олива Motul 8100 X-cess Gen2 5W-40 5л', 1850.00, 1350.00, 4.650, 1);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, Dimensions, InstallationGuideUrl)
    VALUES (1, N'100% синтетична преміальна моторна олива для бензинових і дизельних двигунів.', 60, N'France', N'30x20x12 cm', N'https://motul.com/tech/8100.pdf');

    DECLARE @CatEngineOils INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'engine-oils');
    DECLARE @CatOilsAll INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'oils-fluids');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary)
    VALUES (1, @CatEngineOils, 1), (1, @CatOilsAll, 0);

    INSERT INTO dbo.ProductReviewsSummary (ProductId, AverageRating, TotalReviewsCount, LastReviewedAt)
    VALUES (1, 5.00, 1, '2026-02-12T18:35:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 2)
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, Sku, Name, Price, CostPrice, WeightKg, IsAvailable)
    VALUES (2, @BremboId, N'BRM-P85020', N'Комплект передніх гальмівних колодок Brembo P 85 020', 2800.00, 2050.00, 1.820, 1);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, Dimensions, InstallationGuideUrl)
    VALUES (2, N'Передні дискові гальмівні колодки для легкових авто платформи MQB (VAG).', 24, N'Italy', N'16x10x8 cm', N'https://brembo.com/p85020.pdf');

    DECLARE @CatBrakePads INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'brake-pads');
    DECLARE @CatBrakesAll INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'brakes');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary)
    VALUES (2, @CatBrakePads, 1), (2, @CatBrakesAll, 0);

    INSERT INTO dbo.ProductReviewsSummary (ProductId, AverageRating, TotalReviewsCount, LastReviewedAt)
    VALUES (2, 4.80, 5, '2026-02-14T10:15:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 3)
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, Sku, Name, Price, CostPrice, WeightKg, IsAvailable)
    VALUES (3, @ToptulId, N'TOP-ANAM1621', N'Ключ динамометричний Toptul 1/2" 40-210 Нм (ANAM1621)', 3750.00, 2600.00, 2.100, 1);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, Dimensions, InstallationGuideUrl)
    VALUES (3, N'Професійний клацаючий ключ для контрольованої затяжки з похибкою не більше +-4%.', 36, N'Taiwan', N'55x7x6 cm', N'https://toptul.com/anam1621.pdf');

    DECLARE @CatTools INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'tools');
    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary)
    VALUES (3, @CatTools, 1);

    INSERT INTO dbo.ProductReviewsSummary (ProductId, AverageRating, TotalReviewsCount, LastReviewedAt)
    VALUES (3, 5.00, 3, '2026-02-19T09:10:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 4)
BEGIN
    SET IDENTITY_INSERT dbo.Products ON;
    INSERT INTO dbo.Products (Id, BrandId, Sku, Name, Price, CostPrice, WeightKg, IsAvailable)
    VALUES (4, @BoschId, N'BOS-S4-60AH', N'Акумулятор автомобільний Bosch S4 005 Silver 60Ah 540A', 4300.00, 3200.00, 14.800, 1);
    SET IDENTITY_INSERT dbo.Products OFF;

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, Dimensions, InstallationGuideUrl)
    VALUES (4, N'Необслуговуваний свинцево-кислотний акумулятор преміум серії S4 Silver.', 24, N'Germany', N'24x17x19 cm', N'https://bosch.com/s4-battery.pdf');

    DECLARE @CatBatteries INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'batteries');
    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary)
    VALUES (4, @CatBatteries, 1);

    INSERT INTO dbo.ProductReviewsSummary (ProductId, AverageRating, TotalReviewsCount, LastReviewedAt)
    VALUES (4, 4.50, 2, '2026-02-10T11:00:00');
END;
GO
