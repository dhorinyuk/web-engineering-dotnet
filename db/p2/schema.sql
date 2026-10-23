-- ============================================================================
-- Проєкт №2: Каталог автотоварів та складський облік (Catalog & Inventory)
-- СУБД: Microsoft SQL Server
-- Призначення: Реляційна схема, спроєктована під майбутній Entity Framework Core
-- Вимоги: 4-6 сутностей, зв'язки 1:1, 1:N, M:N через ProductCategories з
--         власними полями IsPrimary та AssignedAt, індекси, обмеження цілісності.
-- ============================================================================

-- Безпечне очищення перед накатуванням
IF OBJECT_ID(N'dbo.InventoryStocks', N'U') IS NOT NULL DROP TABLE dbo.InventoryStocks;
IF OBJECT_ID(N'dbo.ProductDetails', N'U') IS NOT NULL DROP TABLE dbo.ProductDetails;
IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NOT NULL DROP TABLE dbo.ProductCategories;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Brands', N'U') IS NOT NULL DROP TABLE dbo.Brands;
GO

-- ============================================================================
-- 1. Таблиця брендів / виробників (Brands)
-- Сутність 1:N з товарами (Products).
-- ============================================================================
CREATE TABLE dbo.Brands (
    Id          INT IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(100) NOT NULL,
    Code        NVARCHAR(50) NOT NULL,
    Country     NVARCHAR(60) NOT NULL,
    WebsiteUrl  NVARCHAR(255) NULL,
    IsActive    BIT NOT NULL CONSTRAINT DF_Brands_IsActive DEFAULT (1),
    CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_Brands_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt   DATETIME2(7) NULL,

    CONSTRAINT PK_Brands PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Brands_Name UNIQUE NONCLUSTERED (Name),
    CONSTRAINT UQ_Brands_Code UNIQUE NONCLUSTERED (Code)
);
GO

-- ============================================================================
-- 2. Таблиця категорій каталогу (Categories)
-- Підтримує ієрархічні підкатегорії через self-referencing FK.
-- ============================================================================
CREATE TABLE dbo.Categories (
    Id                  INT IDENTITY(1,1) NOT NULL,
    ParentCategoryId    INT NULL,
    Name                NVARCHAR(100) NOT NULL,
    Slug                NVARCHAR(120) NOT NULL,
    Description         NVARCHAR(500) NULL,
    DisplayOrder        INT NOT NULL CONSTRAINT DF_Categories_DisplayOrder DEFAULT (0),
    IsActive            BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
    CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Categories_Slug UNIQUE NONCLUSTERED (Slug),
    CONSTRAINT FK_Categories_ParentCategory FOREIGN KEY (ParentCategoryId) 
        REFERENCES dbo.Categories (Id) ON DELETE NO_ACTION
);
GO

-- ============================================================================
-- 3. Головна таблиця товарів (Products)
-- Зв'язок 1:N з Brands, M:N з Categories, 1:1 з ProductDetails, 1:N з InventoryStocks.
-- ============================================================================
CREATE TABLE dbo.Products (
    Id              INT IDENTITY(1,1) NOT NULL,
    BrandId         INT NOT NULL,
    SKU             NVARCHAR(60) NOT NULL,
    Name            NVARCHAR(200) NOT NULL,
    Slug            NVARCHAR(220) NOT NULL,
    Price           DECIMAL(18,2) NOT NULL,
    CostPrice       DECIMAL(18,2) NOT NULL,
    WeightKg        DECIMAL(8,3) NOT NULL,
    IsAvailable     BIT NOT NULL CONSTRAINT DF_Products_IsAvailable DEFAULT (1),
    CreatedAt       DATETIME2(7) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(7) NULL,

    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Products_SKU UNIQUE NONCLUSTERED (SKU),
    CONSTRAINT UQ_Products_Slug UNIQUE NONCLUSTERED (Slug),
    CONSTRAINT FK_Products_Brands FOREIGN KEY (BrandId) 
        REFERENCES dbo.Brands (Id) ON DELETE NO_ACTION,
    CONSTRAINT CK_Products_Price CHECK (Price >= 0.00),
    CONSTRAINT CK_Products_CostPrice CHECK (CostPrice >= 0.00),
    CONSTRAINT CK_Products_WeightKg CHECK (WeightKg >= 0.000)
);
GO

-- ============================================================================
-- 4. Проміжна таблиця M:N зв'язку (ProductCategories)
-- Реалізує зв'язок M:N між Products та Categories з ВЛАСНИМИ ПОЛЯМИ:
--   - IsPrimary: прапорець головної категорії для хлібних крихт і навігації
--   - AssignedAt: мітка часу прив'язки товару до категорії
-- ============================================================================
CREATE TABLE dbo.ProductCategories (
    ProductId       INT NOT NULL,
    CategoryId      INT NOT NULL,
    IsPrimary       BIT NOT NULL CONSTRAINT DF_ProductCategories_IsPrimary DEFAULT (0),
    AssignedAt      DATETIME2(7) NOT NULL CONSTRAINT DF_ProductCategories_AssignedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_ProductCategories PRIMARY KEY CLUSTERED (ProductId, CategoryId),
    CONSTRAINT FK_ProductCategories_Products FOREIGN KEY (ProductId) 
        REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProductCategories_Categories FOREIGN KEY (CategoryId) 
        REFERENCES dbo.Categories (Id) ON DELETE CASCADE
);
GO

-- ============================================================================
-- 5. Таблиця розширених характеристик (ProductDetails) - Зв'язок 1:1 з Products
-- Спільний первинний ключ ProductId (PK + FK) реалізує чисте відношення 1:1 у EF Core.
-- ============================================================================
CREATE TABLE dbo.ProductDetails (
    ProductId               INT NOT NULL,
    TechnicalDescription    NVARCHAR(MAX) NOT NULL,
    WarrantyMonths          INT NOT NULL CONSTRAINT DF_ProductDetails_Warranty DEFAULT (12),
    CountryOfOrigin         NVARCHAR(60) NOT NULL,
    PackageDimensions       NVARCHAR(100) NULL,
    EAN13                   NVARCHAR(13) NULL,
    InstallationGuideUrl    NVARCHAR(255) NULL,
    SafetyInstructions      NVARCHAR(MAX) NULL,
    CreatedAt               DATETIME2(7) NOT NULL CONSTRAINT DF_ProductDetails_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt               DATETIME2(7) NULL,

    CONSTRAINT PK_ProductDetails PRIMARY KEY CLUSTERED (ProductId),
    CONSTRAINT FK_ProductDetails_Products FOREIGN KEY (ProductId) 
        REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT CK_ProductDetails_Warranty CHECK (WarrantyMonths >= 0),
    CONSTRAINT CK_ProductDetails_EAN13 CHECK (EAN13 IS NULL OR LEN(EAN13) = 13)
);
GO

-- ============================================================================
-- 6. Таблиця залишків на складах (InventoryStocks) - Зв'язок 1:N з Products
-- Фіксує фізичну наявність товару на різних складах компанії.
-- ============================================================================
CREATE TABLE dbo.InventoryStocks (
    Id                  INT IDENTITY(1,1) NOT NULL,
    ProductId           INT NOT NULL,
    WarehouseCode       NVARCHAR(30) NOT NULL,
    QuantityOnHand      INT NOT NULL CONSTRAINT DF_InventoryStocks_Quantity DEFAULT (0),
    ReservedQuantity    INT NOT NULL CONSTRAINT DF_InventoryStocks_Reserved DEFAULT (0),
    ReorderThreshold    INT NOT NULL CONSTRAINT DF_InventoryStocks_Threshold DEFAULT (5),
    BinLocation         NVARCHAR(30) NULL,
    LastRestockedAt     DATETIME2(7) NULL,
    CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_InventoryStocks_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(7) NULL,

    CONSTRAINT PK_InventoryStocks PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_InventoryStocks_Product_Warehouse UNIQUE NONCLUSTERED (ProductId, WarehouseCode),
    CONSTRAINT FK_InventoryStocks_Products FOREIGN KEY (ProductId) 
        REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT CK_InventoryStocks_QuantityOnHand CHECK (QuantityOnHand >= 0),
    CONSTRAINT CK_InventoryStocks_ReservedQuantity CHECK (ReservedQuantity >= 0),
    CONSTRAINT CK_InventoryStocks_ReorderThreshold CHECK (ReorderThreshold >= 0)
);
GO

-- ============================================================================
-- ІНДЕКСИ ДЛЯ ОПТИМІЗАЦІЇ ENTITY FRAMEWORK CORE ТА БІЗНЕС-ЗАПИТІВ
-- ============================================================================

-- 1. Індекс для пошуку товарів за брендом (1:N)
CREATE NONCLUSTERED INDEX IX_Products_BrandId_IsAvailable
ON dbo.Products (BrandId, IsAvailable)
INCLUDE (SKU, Name, Price);
GO

-- 2. Індекс для вибірки товарів за категорією у ProductCategories (M:N)
CREATE NONCLUSTERED INDEX IX_ProductCategories_CategoryId_IsPrimary
ON dbo.ProductCategories (CategoryId, IsPrimary)
INCLUDE (ProductId, AssignedAt);
GO

-- 3. Індекс для підкатегорій (Self-reference)
CREATE NONCLUSTERED INDEX IX_Categories_ParentCategoryId
ON dbo.Categories (ParentCategoryId)
WHERE ParentCategoryId IS NOT NULL;
GO

-- 4. Індекс для швидкої перевірки залишків товару на складі
CREATE NONCLUSTERED INDEX IX_InventoryStocks_ProductId_WarehouseCode
ON dbo.InventoryStocks (ProductId, WarehouseCode)
INCLUDE (QuantityOnHand, ReservedQuantity);
GO
