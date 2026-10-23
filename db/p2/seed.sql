-- ============================================================================
-- Проєкт №2: Ідемпотентне наповнення тестовими даними (Seed Data)
-- Каталог автотоварів та складський облік (Catalog & Inventory Context)
-- СУБД: Microsoft SQL Server (для майбутнього Entity Framework Core)
-- ============================================================================

SET NOCOUNT ON;

-- ----------------------------------------------------------------------------
-- 1. Бренди / Виробники (Brands)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'MOTUL')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country, WebsiteUrl, IsActive, CreatedAt)
    VALUES (N'Motul', N'MOTUL', N'France', N'https://www.motul.com', 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'BREMBO')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country, WebsiteUrl, IsActive, CreatedAt)
    VALUES (N'Brembo', N'BREMBO', N'Italy', N'https://www.brembo.com', 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'TOPTUL')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country, WebsiteUrl, IsActive, CreatedAt)
    VALUES (N'Toptul', N'TOPTUL', N'Taiwan', N'https://www.toptul.com', 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'BOSCH')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country, WebsiteUrl, IsActive, CreatedAt)
    VALUES (N'Bosch', N'BOSCH', N'Germany', N'https://www.bosch-automotive.com', 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'MANN')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country, WebsiteUrl, IsActive, CreatedAt)
    VALUES (N'MANN-FILTER', N'MANN', N'Germany', N'https://www.mann-filter.com', 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Brands WHERE Code = N'NGK')
BEGIN
    INSERT INTO dbo.Brands (Name, Code, Country, WebsiteUrl, IsActive, CreatedAt)
    VALUES (N'NGK Spark Plug', N'NGK', N'Japan', N'https://www.ngkntk.com', 1, '2026-01-01T00:00:00');
END;

-- ----------------------------------------------------------------------------
-- 2. Категорії каталогу (Categories: кореневі та підкатегорії)
-- ----------------------------------------------------------------------------

-- Кореневі категорії
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'oils-fluids')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES (NULL, N'Мастила та технічні рідини', N'oils-fluids', N'Моторні, трансмісійні оливи, гальмівні рідини та антифризи', 10, 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'brake-system')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES (NULL, N'Гальмівна система', N'brake-system', N'Гальмівні колодки, диски, шланги та супорти', 20, 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'tools-equipment')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES (NULL, N'Інструменти та обладнання', N'tools-equipment', N'Професійний слюсарний та динамометричний інструмент для автосервісу', 30, 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'auto-electronics')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES (NULL, N'Автоелектроніка та акумулятори', N'auto-electronics', N'Стартерні акумуляторні батареї, датчики та реле', 40, 1, '2026-01-01T00:00:00');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'maintenance-parts')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES (NULL, N'Запчастини для ТО', N'maintenance-parts', N'Фільтри, свічки запалювання та комплектуючі регулярного сервісу', 50, 1, '2026-01-01T00:00:00');
END;

-- Дочірні підкатегорії
DECLARE @OilsCatId INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'oils-fluids');
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'engine-oils')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES (@OilsCatId, N'Моторні оливи', N'engine-oils', N'Синтетичні, напівсинтетичні та мінеральні моторні оливи', 11, 1, '2026-01-01T00:00:00');
END;

DECLARE @BrakeCatId INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'brake-system');
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'brake-pads')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES (@BrakeCatId, N'Гальмівні колодки', N'brake-pads', N'Передні та задні дискові гальмівні колодки', 21, 1, '2026-01-01T00:00:00');
END;

DECLARE @ToolCatId INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'tools-equipment');
IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = N'torque-wrenches')
BEGIN
    INSERT INTO dbo.Categories (ParentCategoryId, Name, Slug, Description, DisplayOrder, IsActive, CreatedAt)
    VALUES (@ToolCatId, N'Динамометричні ключі', N'torque-wrenches', N'Точний моментний інструмент з сертифікатом калібрування', 31, 1, '2026-01-01T00:00:00');
END;

-- ----------------------------------------------------------------------------
-- 3. Товари (Products), Деталі (ProductDetails), Категорії (ProductCategories)
--    та Залишки (InventoryStocks)
-- ----------------------------------------------------------------------------

-- Товар 1: Моторна олива Motul 8100 X-cess Gen2 5W-40 5л
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'MOT-8100-5W40-5L')
BEGIN
    DECLARE @MotulBrandId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'MOTUL');
    DECLARE @Prod1 INT;

    INSERT INTO dbo.Products (BrandId, SKU, Name, Slug, Price, CostPrice, WeightKg, IsAvailable, CreatedAt)
    VALUES (@MotulBrandId, N'MOT-8100-5W40-5L', N'Моторна олива Motul 8100 X-cess Gen2 5W-40 5л', N'motul-8100-x-cess-gen2-5w-40-5l', 1850.00, 1350.00, 4.650, 1, '2026-01-10T10:00:00');

    SET @Prod1 = SCOPE_IDENTITY();

    -- 1:1 ProductDetails
    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, PackageDimensions, EAN13, InstallationGuideUrl, SafetyInstructions, CreatedAt)
    VALUES (@Prod1, N'100% синтетична моторна олива спеціально розроблена для сучасних бензинових та дизельних двигунів великої потужності.', 60, N'France', N'30x20x12 cm', N'3374650293114', N'https://motul.com/guides/8100-gen2.pdf', N'Уникати тривалого контакту зі шкірою. Утилізувати у спеціалізованих пунктах прийому.', '2026-01-10T10:00:00');

    -- M:N ProductCategories (Основна категорія: Моторні оливи, Додаткова: Мастила та рідини)
    DECLARE @CatEngineOils INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'engine-oils');
    DECLARE @CatOilsAll INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'oils-fluids');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary, AssignedAt)
    VALUES 
        (@Prod1, @CatEngineOils, 1, '2026-01-10T10:00:00'),
        (@Prod1, @CatOilsAll, 0, '2026-01-10T10:00:00');

    -- 1:N InventoryStocks
    INSERT INTO dbo.InventoryStocks (ProductId, WarehouseCode, QuantityOnHand, ReservedQuantity, ReorderThreshold, BinLocation, LastRestockedAt, CreatedAt)
    VALUES 
        (@Prod1, N'WH-KYIV-MAIN', 120, 15, 20, N'OIL-A-01', '2026-02-01T08:00:00', '2026-01-10T10:00:00'),
        (@Prod1, N'WH-LVIV-WEST', 45, 5, 10, N'OIL-B-14', '2026-01-28T09:30:00', '2026-01-10T10:00:00');
END;

-- Товар 2: Гальмівні колодки Brembo P 85 020
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'BRM-P85020')
BEGIN
    DECLARE @BremboBrandId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'BREMBO');
    DECLARE @Prod2 INT;

    INSERT INTO dbo.Products (BrandId, SKU, Name, Slug, Price, CostPrice, WeightKg, IsAvailable, CreatedAt)
    VALUES (@BremboBrandId, N'BRM-P85020', N'Комплект передніх гальмівних колодок Brembo P 85 020', N'brembo-brake-pads-p85020', 2800.00, 2050.00, 1.820, 1, '2026-01-12T11:00:00');

    SET @Prod2 = SCOPE_IDENTITY();

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, PackageDimensions, EAN13, InstallationGuideUrl, SafetyInstructions, CreatedAt)
    VALUES (@Prod2, N'Оригінальні дискові гальмівні колодки преміум-серії для автомобілів групи VAG (Volkswagen, Audi, Skoda, Seat). Сертифікат ECE-R90.', 24, N'Italy', N'16x10x8 cm', N'8020584058145', N'https://brembo.com/manuals/P85020.pdf', N'Монтаж виконувати згідно з інструкцією автовиробника із заміною монтажних пружин.', '2026-01-12T11:00:00');

    DECLARE @CatBrakePads INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'brake-pads');
    DECLARE @CatBrakeAll INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'brake-system');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary, AssignedAt)
    VALUES 
        (@Prod2, @CatBrakePads, 1, '2026-01-12T11:00:00'),
        (@Prod2, @CatBrakeAll, 0, '2026-01-12T11:00:00');

    INSERT INTO dbo.InventoryStocks (ProductId, WarehouseCode, QuantityOnHand, ReservedQuantity, ReorderThreshold, BinLocation, LastRestockedAt, CreatedAt)
    VALUES 
        (@Prod2, N'WH-KYIV-MAIN', 60, 8, 10, N'BRK-C-05', '2026-02-05T12:00:00', '2026-01-12T11:00:00'),
        (@Prod2, N'WH-ODESA-SOUTH', 25, 2, 5, N'BRK-A-02', '2026-01-30T15:00:00', '2026-01-12T11:00:00');
END;

-- Товар 3: Динамометричний ключ Toptul 1/2" 40-210 Нм (ANAM1621)
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'TOP-ANAM1621')
BEGIN
    DECLARE @ToptulBrandId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'TOPTUL');
    DECLARE @Prod3 INT;

    INSERT INTO dbo.Products (BrandId, SKU, Name, Slug, Price, CostPrice, WeightKg, IsAvailable, CreatedAt)
    VALUES (@ToptulBrandId, N'TOP-ANAM1621', N'Ключ динамометричний Toptul 1/2" 40-210 Нм (ANAM1621)', N'toptul-torque-wrench-anam1621-1-2', 3750.00, 2600.00, 2.100, 1, '2026-01-15T15:00:00');

    SET @Prod3 = SCOPE_IDENTITY();

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, PackageDimensions, EAN13, InstallationGuideUrl, SafetyInstructions, CreatedAt)
    VALUES (@Prod3, N'Професійний клацаючий ключ граничного типу з храповим механізмом на 24 зуби. Похибка вимірювання не більше +-4%. У комплекті калібрувальний паспорт.', 36, N'Taiwan', N'55x7x6 cm', N'4711671562914', N'https://toptul.com/docs/ANAM1621_manual.pdf', N'Після завершення робіт обов''язково повертати регулятор на мінімальну позначку шкали.', '2026-01-15T15:00:00');

    DECLARE @CatTorque INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'torque-wrenches');
    DECLARE @CatToolsAll INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'tools-equipment');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary, AssignedAt)
    VALUES 
        (@Prod3, @CatTorque, 1, '2026-01-15T15:00:00'),
        (@Prod3, @CatToolsAll, 0, '2026-01-15T15:00:00');

    INSERT INTO dbo.InventoryStocks (ProductId, WarehouseCode, QuantityOnHand, ReservedQuantity, ReorderThreshold, BinLocation, LastRestockedAt, CreatedAt)
    VALUES 
        (@Prod3, N'WH-KYIV-MAIN', 35, 4, 8, N'TOOL-D-12', '2026-02-10T10:00:00', '2026-01-15T15:00:00');
END;

-- Товар 4: Акумулятор автомобільний Bosch S4 005 Silver 60Ah 540A
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'BOS-S4-60AH')
BEGIN
    DECLARE @BoschBrandId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'BOSCH');
    DECLARE @Prod4 INT;

    INSERT INTO dbo.Products (BrandId, SKU, Name, Slug, Price, CostPrice, WeightKg, IsAvailable, CreatedAt)
    VALUES (@BoschBrandId, N'BOS-S4-60AH', N'Акумулятор автомобільний Bosch S4 005 Silver 60Ah 540A', N'bosch-s4-005-silver-60ah-540a', 4300.00, 3200.00, 14.800, 1, '2026-01-18T09:30:00');

    SET @Prod4 = SCOPE_IDENTITY();

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, PackageDimensions, EAN13, InstallationGuideUrl, SafetyInstructions, CreatedAt)
    VALUES (@Prod4, N'Необслуговувана кальцієва акумуляторна батарея серії S4 з технологією срібного легування пластин PowerFrame. Полярність: права (+ справа).', 24, N'Germany', N'24x17x19 cm', N'4047023479267', N'https://bosch-automotive.com/s4-battery.pdf', N'Містить їдку сірчану кислоту. Берегти від вогню та іскор.', '2026-01-18T09:30:00');

    DECLARE @CatElectronics INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'auto-electronics');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary, AssignedAt)
    VALUES (@Prod4, @CatElectronics, 1, '2026-01-18T09:30:00');

    INSERT INTO dbo.InventoryStocks (ProductId, WarehouseCode, QuantityOnHand, ReservedQuantity, ReorderThreshold, BinLocation, LastRestockedAt, CreatedAt)
    VALUES 
        (@Prod4, N'WH-KYIV-MAIN', 40, 6, 12, N'BAT-A-01', '2026-02-08T11:00:00', '2026-01-18T09:30:00'),
        (@Prod4, N'WH-LVIV-WEST', 18, 2, 5, N'BAT-B-03', '2026-02-02T14:00:00', '2026-01-18T09:30:00');
END;

-- Товар 5: Масляний фільтр MANN-FILTER W 712/94
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'FLT-MAN-W712')
BEGIN
    DECLARE @MannBrandId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'MANN');
    DECLARE @Prod5 INT;

    INSERT INTO dbo.Products (BrandId, SKU, Name, Slug, Price, CostPrice, WeightKg, IsAvailable, CreatedAt)
    VALUES (@MannBrandId, N'FLT-MAN-W712', N'Масляний фільтр MANN-FILTER W 712/94', N'mann-filter-w-712-94', 600.00, 390.00, 0.380, 1, '2026-01-20T12:00:00');

    SET @Prod5 = SCOPE_IDENTITY();

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, PackageDimensions, EAN13, InstallationGuideUrl, SafetyInstructions, CreatedAt)
    VALUES (@Prod5, N'Оригінальний накручуваний масляний фільтр для бензинових двигунів TSI/TFSI групи VAG з антидренажним клапаном.', 12, N'Germany', N'8x8x10 cm', N'4011558021671', N'https://mann-filter.com/w712-94.pdf', N'Перед встановленням змастити ущільнювальне гумове кільце моторним мастилом.', '2026-01-20T12:00:00');

    DECLARE @CatParts INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'maintenance-parts');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary, AssignedAt)
    VALUES (@Prod5, @CatParts, 1, '2026-01-20T12:00:00');

    INSERT INTO dbo.InventoryStocks (ProductId, WarehouseCode, QuantityOnHand, ReservedQuantity, ReorderThreshold, BinLocation, LastRestockedAt, CreatedAt)
    VALUES (@Prod5, N'WH-KYIV-MAIN', 250, 30, 40, N'FLT-C-02', '2026-02-12T16:00:00', '2026-01-20T12:00:00');
END;

-- Товар 6: Свічки запалювання NGK Laser Iridium (комплект 4 шт.)
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'NGK-LFR6AIX')
BEGIN
    DECLARE @NgkBrandId INT = (SELECT Id FROM dbo.Brands WHERE Code = N'NGK');
    DECLARE @Prod6 INT;

    INSERT INTO dbo.Products (BrandId, SKU, Name, Slug, Price, CostPrice, WeightKg, IsAvailable, CreatedAt)
    VALUES (@NgkBrandId, N'NGK-LFR6AIX', N'Свічки запалювання NGK Laser Iridium (комплект 4 шт.)', N'ngk-laser-iridium-lfr6aix-set4', 1200.00, 820.00, 0.220, 1, '2026-01-22T14:30:00');

    SET @Prod6 = SCOPE_IDENTITY();

    INSERT INTO dbo.ProductDetails (ProductId, TechnicalDescription, WarrantyMonths, CountryOfOrigin, PackageDimensions, EAN13, InstallationGuideUrl, SafetyInstructions, CreatedAt)
    VALUES (@Prod6, N'Високоефективні іридієві свічки запалювання з лазерною наваркою іридієвого наконечника для максимальної стабільності іскроутворення.', 24, N'Japan', N'10x9x3 cm', N'0872951667710', N'https://ngkntk.com/plugs/lfr6aix.pdf', N'Затягувати рекомендованим моментом 25-30 Нм з використанням динамометричного ключа.', '2026-01-22T14:30:00');

    DECLARE @CatParts6 INT = (SELECT Id FROM dbo.Categories WHERE Slug = N'maintenance-parts');

    INSERT INTO dbo.ProductCategories (ProductId, CategoryId, IsPrimary, AssignedAt)
    VALUES (@Prod6, @CatParts6, 1, '2026-01-22T14:30:00');

    INSERT INTO dbo.InventoryStocks (ProductId, WarehouseCode, QuantityOnHand, ReservedQuantity, ReorderThreshold, BinLocation, LastRestockedAt, CreatedAt)
    VALUES (@Prod6, N'WH-KYIV-MAIN', 80, 10, 15, N'SPK-A-04', '2026-02-11T13:00:00', '2026-01-22T14:30:00');
END;

PRINT N'[INFO] Ідемпотентні тестові дані для Проєкту №2 успішно завантажені.';
GO
