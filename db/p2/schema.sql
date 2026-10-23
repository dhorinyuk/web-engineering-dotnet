-- Catalog bounded context database schema for Entity Framework Core (Microsoft SQL Server)

IF OBJECT_ID(N'dbo.ProductReviewsSummary', N'U') IS NOT NULL DROP TABLE dbo.ProductReviewsSummary;
IF OBJECT_ID(N'dbo.ProductDetails', N'U') IS NOT NULL DROP TABLE dbo.ProductDetails;
IF OBJECT_ID(N'dbo.ProductCategories', N'U') IS NOT NULL DROP TABLE dbo.ProductCategories;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID(N'dbo.Brands', N'U') IS NOT NULL DROP TABLE dbo.Brands;
GO

-- Manufacturer brand catalog entity
CREATE TABLE dbo.Brands (
    Id          INT IDENTITY(1,1) NOT NULL,
    Name        NVARCHAR(100) NOT NULL,
    Code        NVARCHAR(50) NOT NULL,
    Country     NVARCHAR(60) NOT NULL,
    IsActive    BIT NOT NULL CONSTRAINT DF_Brands_IsActive DEFAULT (1),
    CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_Brands_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Brands PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Brands_Name UNIQUE NONCLUSTERED (Name),
    CONSTRAINT UQ_Brands_Code UNIQUE NONCLUSTERED (Code)
);
GO

-- Hierarchical category tree with self-referencing relationship
CREATE TABLE dbo.Categories (
    Id                  INT IDENTITY(1,1) NOT NULL,
    ParentCategoryId    INT NULL,
    Name                NVARCHAR(100) NOT NULL,
    Slug                NVARCHAR(120) NOT NULL,
    DisplayOrder        INT NOT NULL CONSTRAINT DF_Categories_DisplayOrder DEFAULT (0),
    IsActive            BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
    CreatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Categories_Slug UNIQUE NONCLUSTERED (Slug),
    CONSTRAINT FK_Categories_ParentCategory FOREIGN KEY (ParentCategoryId) REFERENCES dbo.Categories (Id)
);
GO

-- Product entity mapped to EF Core aggregate root with concurrency token
CREATE TABLE dbo.Products (
    Id          INT IDENTITY(1,1) NOT NULL,
    BrandId     INT NOT NULL,
    Sku         NVARCHAR(64) NOT NULL,
    Name        NVARCHAR(200) NOT NULL,
    Price       DECIMAL(18,2) NOT NULL,
    CostPrice   DECIMAL(18,2) NOT NULL,
    WeightKg    DECIMAL(8,3) NOT NULL,
    IsAvailable BIT NOT NULL CONSTRAINT DF_Products_IsAvailable DEFAULT (1),
    RowVersion  ROWVERSION NOT NULL,
    CreatedAt   DATETIME2(7) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt   DATETIME2(7) NULL,

    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Products_Sku UNIQUE NONCLUSTERED (Sku),
    CONSTRAINT FK_Products_Brands FOREIGN KEY (BrandId) REFERENCES dbo.Brands (Id),
    CONSTRAINT CK_Products_Price CHECK (Price >= 0.00),
    CONSTRAINT CK_Products_CostPrice CHECK (CostPrice >= 0.00),
    CONSTRAINT CK_Products_WeightKg CHECK (WeightKg >= 0.000)
);
GO

-- 1:1 technical specification details where PK is FK
CREATE TABLE dbo.ProductDetails (
    ProductId               INT NOT NULL,
    TechnicalDescription    NVARCHAR(MAX) NOT NULL,
    WarrantyMonths          INT NOT NULL CONSTRAINT DF_ProductDetails_WarrantyMonths DEFAULT (12),
    CountryOfOrigin         NVARCHAR(60) NOT NULL,
    Dimensions              NVARCHAR(100) NULL,
    InstallationGuideUrl    NVARCHAR(255) NULL,

    CONSTRAINT PK_ProductDetails PRIMARY KEY CLUSTERED (ProductId),
    CONSTRAINT FK_ProductDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT CK_ProductDetails_WarrantyMonths CHECK (WarrantyMonths >= 0)
);
GO

-- M:N join entity with payload attributes
CREATE TABLE dbo.ProductCategories (
    ProductId   INT NOT NULL,
    CategoryId  INT NOT NULL,
    IsPrimary   BIT NOT NULL CONSTRAINT DF_ProductCategories_IsPrimary DEFAULT (0),
    AssignedAt  DATETIME2(7) NOT NULL CONSTRAINT DF_ProductCategories_AssignedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_ProductCategories PRIMARY KEY CLUSTERED (ProductId, CategoryId),
    CONSTRAINT FK_ProductCategories_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProductCategories_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id) ON DELETE CASCADE
);
GO

-- Denormalized review analytics projection updated via integration events
CREATE TABLE dbo.ProductReviewsSummary (
    ProductId           INT NOT NULL,
    AverageRating       DECIMAL(3,2) NOT NULL CONSTRAINT DF_ReviewsSummary_AverageRating DEFAULT (0.00),
    TotalReviewsCount   INT NOT NULL CONSTRAINT DF_ReviewsSummary_TotalCount DEFAULT (0),
    LastReviewedAt      DATETIME2(7) NULL,
    UpdatedAt           DATETIME2(7) NOT NULL CONSTRAINT DF_ReviewsSummary_UpdatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_ProductReviewsSummary PRIMARY KEY CLUSTERED (ProductId),
    CONSTRAINT FK_ProductReviewsSummary_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT CK_ProductReviewsSummary_AverageRating CHECK (AverageRating >= 0.00 AND AverageRating <= 5.00),
    CONSTRAINT CK_ProductReviewsSummary_TotalReviewsCount CHECK (TotalReviewsCount >= 0)
);
GO

-- Query optimization index for catalog filtering by brand and price
CREATE NONCLUSTERED INDEX IX_Products_Brand_Price
ON dbo.Products (BrandId, Price);
GO

-- Lookup index for catalog item by SKU
CREATE NONCLUSTERED INDEX IX_Products_Sku
ON dbo.Products (Sku);
GO

-- Category navigation index
CREATE NONCLUSTERED INDEX IX_ProductCategories_CategoryId
ON dbo.ProductCategories (CategoryId, IsPrimary);
GO

-- Hierarchy lookup index
CREATE NONCLUSTERED INDEX IX_Categories_ParentCategoryId
ON dbo.Categories (ParentCategoryId)
WHERE ParentCategoryId IS NOT NULL;
GO
