# AutoParts & Tools E-Commerce Platform

Enterprise distributed e-commerce platform for auto parts and professional tools retail, designed with polyglot persistence (Microsoft SQL Server and MongoDB) and clean 3-tier architecture (.NET 8, ADO.NET, Dapper, and .NET Aspire).

---

## Architecture Overview

The system is separated into three bounded contexts following Domain-Driven Design (DDD):

1. **Ordering Context (`db/p1`, `Services/Orders`):** Microsoft SQL Server OLTP database and .NET 3-tier service handling order lifecycle, line items, state transitions with row locking, price snapshotting, and stock decrease.
2. **Catalog Context (`db/p2`):** Microsoft SQL Server database designed for Entity Framework Core managing products, brands, self-referencing categories, and payload-rich M:N category assignments.
3. **Reviews Context (`db/p3`):** MongoDB document store managing customer feedback, multi-tier rating breakdowns, threaded discussions, and flexible polymorphic review structures.

---

## Entity-Relationship Diagrams (ERD)

### Project 1: Ordering Service Database
- PlantUML: [docs/erd-p1.puml](docs/erd-p1.puml)

```plantuml
@startuml
!theme plain
skinparam linetype ortho
skinparam roundcorner 4

entity "Customers" as customers {
    * Id : BIGINT <<PK, identity>>
    --
    * ExternalId : UNIQUEIDENTIFIER <<UQ>>
    * FullName : NVARCHAR(100)
    * Email : NVARCHAR(255) <<UQ>>
    * Phone : NVARCHAR(30)
    * CreatedAt : DATETIME2
    * CreatedBy : NVARCHAR(100)
    UpdatedAt : DATETIME2
    UpdatedBy : NVARCHAR(100)
    * IsDeleted : BIT
    * RowVersion : ROWVERSION
}

entity "Orders" as orders {
    * Id : BIGINT <<PK, identity>>
    --
    * OrderNumber : NVARCHAR(32) <<UQ>>
    * CustomerId : BIGINT <<FK>>
    * Status : NVARCHAR(32)
    * TotalAmount : DECIMAL(18,2)
    * CreatedAt : DATETIME2
    * CreatedBy : NVARCHAR(100)
    UpdatedAt : DATETIME2
    UpdatedBy : NVARCHAR(100)
    * IsDeleted : BIT
    * RowVersion : ROWVERSION
}

entity "OrderDetails" as order_details {
    * OrderId : BIGINT <<PK, FK>>
    --
    * RecipientName : NVARCHAR(100)
    * RecipientPhone : NVARCHAR(30)
    * ShippingAddress : NVARCHAR(255)
    * City : NVARCHAR(100)
    PostalCode : NVARCHAR(20)
    * ShippingMethod : NVARCHAR(50)
    TrackingNumber : NVARCHAR(100)
    DeliveredAt : DATETIME2
    * CreatedAt : DATETIME2
    * CreatedBy : NVARCHAR(100)
    UpdatedAt : DATETIME2
    UpdatedBy : NVARCHAR(100)
    * IsDeleted : BIT
}

entity "Products" as products {
    * Id : BIGINT <<PK>>
    --
    * Sku : NVARCHAR(64) <<UQ>>
    * Name : NVARCHAR(200)
    * Price : DECIMAL(18,2)
    * IsAvailable : BIT
    * CreatedAt : DATETIME2
    * CreatedBy : NVARCHAR(100)
    UpdatedAt : DATETIME2
    UpdatedBy : NVARCHAR(100)
    * IsDeleted : BIT
}

entity "OrderItems" as order_items {
    * OrderId : BIGINT <<PK, FK>>
    * ProductId : BIGINT <<PK, FK>>
    --
    * ProductName : NVARCHAR(200)
    * UnitPrice : DECIMAL(18,2)
    * Units : INT
    * SubTotal : DECIMAL(18,2) <<computed>>
    * CreatedAt : DATETIME2
    * CreatedBy : NVARCHAR(100)
    UpdatedAt : DATETIME2
    UpdatedBy : NVARCHAR(100)
    * IsDeleted : BIT
}

entity "OrderStatusHistory" as order_status_history {
    * HistoryId : BIGINT <<PK, identity>>
    --
    * OrderId : BIGINT <<FK>>
    OldStatus : NVARCHAR(30)
    * NewStatus : NVARCHAR(30)
    * ChangedAt : DATETIME2
    Comment : NVARCHAR(500)
}

customers ||--o{ orders : "places (1:N)"
orders ||--|| order_details : "has delivery details (1:1)"
orders ||--|{ order_items : "contains (1:N)"
products ||--o{ order_items : "referenced by (1:N)"
orders ||--o{ order_status_history : "tracks state history (1:N)"
@enduml
```

### Project 2: Catalog Service Database
- PlantUML: [docs/erd-p2.puml](docs/erd-p2.puml)

```plantuml
@startuml
!theme plain
skinparam linetype ortho
skinparam roundcorner 4

entity "Brands" as brands {
    * Id : INT <<PK, identity>>
    --
    * Name : NVARCHAR(100) <<UQ>>
    * Code : NVARCHAR(50) <<UQ>>
    * Country : NVARCHAR(60)
    * IsActive : BIT
    * CreatedAt : DATETIME2
    UpdatedAt : DATETIME2
}

entity "Categories" as categories {
    * Id : INT <<PK, identity>>
    --
    ParentCategoryId : INT <<FK, self-ref>>
    * Name : NVARCHAR(100)
    * Slug : NVARCHAR(120) <<UQ>>
    * DisplayOrder : INT
    * IsActive : BIT
    * CreatedAt : DATETIME2
    UpdatedAt : DATETIME2
}

entity "Products" as products {
    * Id : INT <<PK, identity>>
    --
    * BrandId : INT <<FK>>
    * Sku : NVARCHAR(64) <<UQ>>
    * Slug : NVARCHAR(220) <<UQ>>
    * Name : NVARCHAR(200)
    * Price : DECIMAL(18,2)
    * CostPrice : DECIMAL(18,2)
    * WeightKg : DECIMAL(8,3)
    * Stock : INT
    * IsAvailable : BIT
    * RowVersion : ROWVERSION
    * CreatedAt : DATETIME2
    UpdatedAt : DATETIME2
}

entity "ProductDetails" as product_details {
    * ProductId : INT <<PK, FK>>
    --
    * TechnicalDescription : NVARCHAR(MAX)
    * WarrantyMonths : INT
    * CountryOfOrigin : NVARCHAR(60)
    Dimensions : NVARCHAR(100)
    InstallationGuideUrl : NVARCHAR(255)
    * CreatedAt : DATETIME2
    UpdatedAt : DATETIME2
}

entity "ProductCategories" as product_categories {
    * ProductId : INT <<PK, FK>>
    * CategoryId : INT <<PK, FK>>
    --
    * IsPrimary : BIT
    * AssignedAt : DATETIME2
}

entity "ProductReviewsSummary" as reviews_summary {
    * ProductId : INT <<PK, FK>>
    --
    * AverageRating : DECIMAL(3,2)
    * TotalReviewsCount : INT
    LastReviewedAt : DATETIME2
    * UpdatedAt : DATETIME2
}

brands ||--o{ products : "manufactures (1:N)"
categories ||--o{ categories : "subcategories (1:N self-ref)"
products ||--|{ product_categories : "categorized in (1:N)"
categories ||--|{ product_categories : "contains (1:N)"
products ||--|| product_details : "technical specifications (1:1)"
products ||--|| reviews_summary : "aggregated reviews cache (1:1)"
@enduml
```

---

## Data Consistency and Duplication Matrix

| Duplicated Field | Source of Truth | Destination Context | Storage Target | Pattern | Consistency Guarantee |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `ProductName`, `UnitPrice` | `CatalogDb.dbo.Products` | Ordering Context (P1) | `dbo.OrderItems` | Snapshot | Immutable snapshot upon order item insertion |
| `RecipientName`, `ShippingAddress` | Customer Profile | Ordering Context (P1) | `dbo.OrderDetails` | Snapshot | Immutable delivery snapshot |
| `productName` | `CatalogDb.dbo.Products` | Reviews Context (P3) | `reviews` collection | Denormalization | Eventual consistency via integration events |
| `AverageRating`, `TotalReviewsCount` | Reviews Context (P3) | Catalog Context (P2) | `dbo.ProductReviewsSummary` | Read Projection | Eventual consistency via async review events |

---

## Repository and Solution Structure

```text
.
├── Platform.sln                       # Classic solution file registering all projects
├── Platform.slnx                      # Modern XML-based solution file (.NET 9+)
├── Platform.AppHost/                  # Aspire Orchestration AppHost (.NET 8/9)
│   ├── Program.cs                     # Service discovery and container orchestration
│   ├── appsettings.json               # Connection strings and configuration
│   └── Platform.AppHost.csproj
├── Services/
│   ├── Catalog/                       # Lab 03: EF Core 3-tier Catalog Microservice
│   │   ├── Catalog.Domain/            # Rich Domain Entities, Encapsulated Invariants & Domain Exceptions
│   │   │   ├── Entities/              # Product, Brand, Category, ProductDetails, ProductCategory, ProductImage
│   │   │   └── Exceptions/            # DomainException, EntityNotFoundException, ConflictException
│   │   ├── Catalog.Dal/               # EF Core DbContext, Configurations, Repositories, UoW & Migrations
│   │   │   ├── Configurations/        # Product, Brand, Category, ProductDetails, ProductCategory, ProductImage
│   │   │   ├── Migrations/            # EF Core InitialCreate & Idempotent SQL script
│   │   │   ├── Repositories/          # Generic Repo, ProductRepo (Eager, Explicit, LINQ M:N), BrandRepo, CategoryRepo
│   │   │   ├── Specifications/        # ISpecification, BaseSpecification, SpecificationEvaluator
│   │   │   ├── CatalogDbContext.cs
│   │   │   ├── DbInitializer.cs       # Programmatic idempotent seeding
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── UnitOfWork.cs
│   │   ├── Catalog.Bll/               # Business Logic Layer (Services, DTOs, AutoMapper, FluentValidation, Specifications)
│   │   │   ├── Common/                # PagedResult, ProductQueryParams
│   │   │   ├── Dtos/                  # ProductDto, CreateProductDto, UpdateProductDto, BrandDto, CategoryDto
│   │   │   ├── Mapping/               # AutoMapper CatalogMappingProfile
│   │   │   ├── Services/              # ProductService, BrandService, CategoryService (via IUnitOfWork)
│   │   │   ├── Specifications/        # ProductFilterPaginatedSpec, ProductFilterCountSpec
│   │   │   └── Validators/            # FluentValidation Validators
│   │   └── Catalog.Api/               # ASP.NET Core Web API (Controllers, Swagger, Serilog, ProblemDetails)
│   │       ├── Controllers/           # ProductsController, BrandsController, CategoriesController
│   │       ├── Middleware/            # CatalogExceptionHandler (RFC 7807 ProblemDetails)
│   │       ├── Program.cs
│   │       └── appsettings.json
│   └── Orders/
│       ├── Orders.Domain/             # Pure domain models and domain exceptions
│       │   ├── Entities/              # Customer, Order, OrderDetails, Product, OrderItem, OrderStatusHistory
│       │   └── Exceptions/            # NotFoundException, BusinessConflictException, ValidationException
│       ├── Orders.Dal/                # Data Access Layer (ADO.NET + Dapper + Unit of Work)
│       │   ├── Repositories/          # Generic Repo, BaseDapperRepository, CustomerRepo (pure ADO.NET), OrderRepo, ProductRepo
│       │   ├── IUnitOfWork.cs
│       │   └── UnitOfWork.cs          # Single connection per scope, transaction coordinator
│       ├── Orders.Bll/                # Business Logic Layer (DTOs, AutoMapper, 2-level validation)
│       │   ├── DTOs/                  # CreateOrderDto, CreateOrderItemDto, OrderDto, OrderItemDto, CustomerDto
│       │   ├── Mapping/               # AutoMapper OrderProfile
│       │   └── Services/              # IOrderService, OrderService (transactional operations)
│       └── Orders.Api/                # Web API (thin controllers, Swagger, Serilog, ProblemDetails)
│           ├── Controllers/           # OrdersController (GET, POST, POST /confirm)
│           ├── Middleware/            # DomainExceptionHandler (IExceptionHandler -> RFC 7807 ProblemDetails)
│           ├── Program.cs
│           └── appsettings.json
├── docs/
│   ├── contexts.md
│   ├── duplication-policy.md
│   ├── erd-p1.puml
│   ├── erd-p2.puml
│   └── mongo-examples.json
└── db/
    ├── p1/
    │   ├── schema.sql
    │   ├── procedures.sql
    │   └── seed.sql
    ├── p2/
    │   ├── schema.sql
    │   └── seed.sql
    └── p3/
        ├── collections.js
        └── seed.js
```

---

## Laboratory Work #2: 3-Tier Architecture (.NET 8, ADO.NET, Dapper)

### 1. Architecture Design and Layer Responsibilities

The Orders Service is designed strictly following the 3-Tier Enterprise Architectural Pattern:

```
[ HTTP Client / Swagger / Aspire ]
                |
                v
       Orders.Api (Presentation)
  - Thin API Controller (OrdersController)
  - Attribute routing ([Route("api/orders")])
  - Global ProblemDetails Exception Handler (DomainExceptionHandler)
  - Serilog logging & Swagger OpenAPI
                |
                v
       Orders.Bll (Business Logic)
  - Two-level validation (DataAnnotations on DTO + BLL Business Rules)
  - DTO mapping via AutoMapper (OrderProfile)
  - Transactional workflows (OrderService.ConfirmOrderAsync)
  - Price Snapshotting upon Order creation
                |
                v
       Orders.Dal (Data Access)
  - Unit of Work pattern (IUnitOfWork, UnitOfWork) managing 1 SqlConnection
  - Pure ADO.NET (CustomerRepository: SqlCommand, SqlParameter, SqlDataReader)
  - Dapper Generic Repository (BaseDapperRepository<T>: parameterized SQL)
  - Dapper Multi-Mapping (OrderRepository.GetWithItemsAsync via QueryAsync)
  - Stored Procedure execution (OrderRepository.ConfirmAsync -> dbo.usp_ChangeOrderStatus)
                |
                v
       Orders.Domain (Core Entities)
  - Entity models matching database schema (Customer, Order, Product, etc.)
  - Domain exceptions (NotFoundException, BusinessConflictException, ValidationException)
```

### 2. How to Run the Application

#### Option A: Run via .NET Aspire AppHost (Recommended)

```powershell
# Run orchestrator
dotnet run --project Platform.AppHost
```
This automatically boots the Orders API service and launches the Aspire Developer Dashboard in your browser.

#### Option B: Run Orders.Api Directly

```powershell
# Run API directly
dotnet run --project Services/Orders/Orders.Api
```
The API starts on `http://localhost:5000` (or configured HTTPS port), and Swagger UI is accessible at `/swagger`.

---

### 3. API Verification with cURL

#### 1. Retrieve Order by ID (200 OK)
```bash
curl -X GET "http://localhost:5000/api/orders/1" \
     -H "Accept: application/json"
```

#### 2. Create a New Order (201 Created)
```bash
curl -X POST "http://localhost:5000/api/orders" \
     -H "Content-Type: application/json" \
     -d '{
       "customerId": 1,
       "items": [
         { "productId": 1, "units": 2 },
         { "productId": 2, "units": 1 }
       ],
       "shippingAddress": "вул. Хрещатик, 22, кв. 10",
       "city": "Київ",
       "postalCode": "01001"
     }'
```

#### 3. Confirm Order and Decrease Stock (204 No Content)
```bash
curl -X POST "http://localhost:5000/api/orders/1/confirm" \
     -H "Accept: application/json"
```

#### 4. Verification of Error Handling (ProblemDetails)
- **Non-existent Order:** `GET /api/orders/9999` returns `404 Not Found` with RFC 7807 JSON `ProblemDetails`.
- **Duplicate Confirmation:** Confirming an already confirmed order returns `409 Conflict`.
- **Validation Failure:** Negative `units` or missing items return `400 Bad Request`.

---

## Laboratory Work #3: 3-Tier Architecture on Entity Framework Core

### 1. Architecture Design and Layer Responsibilities

The Catalog Service is built on Entity Framework Core (.NET 8) strictly adhering to domain-driven principles and 14 automated verification criteria:

```
[ HTTP Client / Swagger / Aspire ]
                |
                v
       Catalog.Api (Presentation)
  - Thin API Controllers ([Route("api/[controller]")])
  - Async action methods (Task<ActionResult<T>>)
  - Standard HTTP statuses (200, 201 CreatedAtAction, 204, 400, 404, 409)
  - Global ProblemDetails Exception Handler (CatalogExceptionHandler: IExceptionHandler)
  - Serilog structured logging with EF command level override
  - Swagger OpenAPI interactive documentation
                |
                v
       Catalog.Bll (Business Logic)
  - Exclusively operates through IUnitOfWork (no direct DbContext)
  - Specification Pattern integration (ProductFilterPaginatedSpec, ProductFilterCountSpec)
  - SQL-level pagination, filtering and dynamic sorting
  - FluentValidation integration (AbstractValidator<T>) + DataAnnotations
  - AutoMapper profiles (CatalogMappingProfile)
  - Database-level rule validations throwing EntityNotFoundException & ConflictException
                |
                v
       Catalog.Dal (Data Access)
  - CatalogDbContext applying IEntityTypeConfiguration<T> from assembly
  - Explicit relationships: 1:1 (PK=FK), 1:N (Cascade), M:N (Junction with AddedAt payload)
  - Check constraints (CK_Products_Price, CK_Products_Stock, etc.) & Unique indexes
  - Async Generic Repository (IGenericRepository<T>, GenericRepository<T>)
  - Unit of Work (IUnitOfWork, UnitOfWork) controlling SaveChangesAsync boundary
  - 3 Specific Repositories:
      1. Eager Loading (.Include + .ThenInclude single query)
      2. Explicit Loading (.Entry().Collection().Query().LoadAsync())
      3. LINQ to Entities M:N query (filtered & sorted by junction AddedAt)
  - Static seeding (HasData) & Programmatic seeding (DbInitializer.SeedAsync)
  - InitialCreate migration & Idempotent SQL migration script (InitialCreate.sql)
                |
                v
       Catalog.Domain (Domain Entities)
  - Encapsulated business rules: private setters, private parameterless constructors
  - Public constructors with invariant checks throwing DomainException
  - State mutations strictly through domain methods (UpdatePrice, AddStock, RemoveStock)
  - Domain exceptions: DomainException, EntityNotFoundException, ConflictException
```

### 2. How to Run Catalog Service

#### Option A: Run via .NET Aspire AppHost

```powershell
dotnet run --project Platform.AppHost
```

#### Option B: Run Catalog.Api Directly

```powershell
dotnet run --project Services/Catalog/Catalog.Api
```

Swagger UI will be available at: `http://localhost:5000/swagger` (or configured HTTPS port).

---

### 3. API Verification with cURL

#### 1. SQL-Level Paginated, Filtered & Sorted Query (200 OK)
```bash
curl -X GET "http://localhost:5000/api/products?page=1&pageSize=10&sortBy=price&sortDescending=false" \
     -H "Accept: application/json"
```

#### 2. Eager Loading Full Aggregate (200 OK)
```bash
curl -X GET "http://localhost:5000/api/products/1" \
     -H "Accept: application/json"
```

#### 3. Explicit Loading Demonstration (200 OK)
```bash
curl -X GET "http://localhost:5000/api/products/1/explicit" \
     -H "Accept: application/json"
```

#### 4. LINQ M:N Junction Query by AddedAt (200 OK)
```bash
curl -X GET "http://localhost:5000/api/products/categories/2/recent?days=30" \
     -H "Accept: application/json"
```

#### 5. Create Product (201 Created with Location Header)
```bash
curl -X POST "http://localhost:5000/api/products" \
     -H "Content-Type: application/json" \
     -d '{
       "brandId": 1,
       "sku": "MOT-300V-15W50-2L",
       "slug": "motul-300v-competition-15w50-2l",
       "name": "Моторна олива Motul 300V 15W-50 2л",
       "price": 1450.00,
       "costPrice": 980.00,
       "weightKg": 1.950,
       "stock": 25,
       "isAvailable": true,
       "categoryIds": [1, 2],
       "primaryCategoryId": 2,
       "details": {
         "technicalDescription": "100% синтетична спортивна олива з технологією ESTER Core.",
         "warrantyMonths": 60,
         "countryOfOrigin": "France"
       }
     }'
```

#### 6. Update Price via Domain Method (200 OK)
```bash
curl -X PATCH "http://localhost:5000/api/products/1/price" \
     -H "Content-Type: application/json" \
     -d '{
       "price": 1950.00,
       "costPrice": 1400.00
     }'
```

#### 7. Update Stock via Domain Method (200 OK)
```bash
curl -X PATCH "http://localhost:5000/api/products/1/stock" \
     -H "Content-Type: application/json" \
     -d '{
       "quantity": 10,
       "operation": "add"
     }'
```

#### 8. Verification of Error Handling (RFC 7807 ProblemDetails)
- **404 Not Found:** `GET /api/products/9999` returns `404 Not Found` with `EntityNotFoundException` mapped to ProblemDetails.
- **409 Conflict:** Creating a product with duplicate SKU returns `409 Conflict` with `ConflictException`.
- **400 Validation Problem:** Passing negative price or empty name returns `400 Bad Request` with `ValidationProblemDetails` containing field-level errors.

---

## Deployment and Verification Guide for Databases

### 1. Database Containers Setup

```bash
# Start Microsoft SQL Server 2022
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd!" \
   -p 1433:1433 --name mssql-ecommerce -d mcr.microsoft.com/mssql/server:2022-latest

# Start MongoDB
docker run -p 27017:27017 --name mongo-ecommerce -d mongo:latest
```

---

### 2. Project 1 Deployment (Ordering DB - SQL Server)

```powershell
# Create Ordering database
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'OrderingDb') CREATE DATABASE OrderingDb;"

# Apply schema, stored procedures, and seed data
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d OrderingDb -i db/p1/schema.sql
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d OrderingDb -i db/p1/procedures.sql
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d OrderingDb -i db/p1/seed.sql
```

**Verification Queries:**
```sql
USE OrderingDb;

-- Test status transition with row locking
EXEC dbo.usp_ChangeOrderStatus 
    @OrderId = 1, 
    @NewStatus = N'Paid', 
    @Comment = N'Payment confirmed via LiqPay';

-- Inspect order status and audit history
SELECT Id, OrderNumber, Status, TotalAmount, RowVersion FROM dbo.Orders WHERE Id = 1;
SELECT * FROM dbo.OrderStatusHistory WHERE OrderId = 1 ORDER BY ChangedAt ASC;
```

---

### 3. Project 2 Deployment (Catalog DB - SQL Server for EF Core)

```powershell
# Create Catalog database
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'CatalogDb') CREATE DATABASE CatalogDb;"

# Apply schema and seed data
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d CatalogDb -i db/p2/schema.sql
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d CatalogDb -i db/p2/seed.sql
```

**Verification Queries:**
```sql
USE CatalogDb;

-- Verify M:N payload relationship with IsPrimary and AssignedAt
SELECT 
    p.Sku,
    p.Name AS ProductName,
    c.Name AS CategoryName,
    pc.IsPrimary,
    pc.AssignedAt
FROM dbo.ProductCategories pc
INNER JOIN dbo.Products p ON pc.ProductId = p.Id
INNER JOIN dbo.Categories c ON pc.CategoryId = c.Id;
```

---

### 4. Project 3 Deployment (Reviews DB - MongoDB)

```powershell
# Initialize collections, validation schemas, and indexes
mongosh "mongodb://localhost:27017" db/p3/collections.js

# Load idempotent seed documents
mongosh "mongodb://localhost:27017" db/p3/seed.js
```

**Verification Queries:**
```mongodb
use ReviewsServiceDb;

// Query polymorphic review document with embedded comments and referenced discussion
db.reviews.find({ productId: 1 }).pretty();

// Query minimal review document
db.reviews.find({ productId: 2 }).pretty();
```
