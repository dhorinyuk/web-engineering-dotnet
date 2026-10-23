# Лабораторна робота №1: Проєктування та розгортання баз даних розподіленої платформи

**Курс:** Технології .NET у веб-інженерії  
**Предметна область:** Інтернет-магазин автотоварів та інструментів (AutoParts & Tools E-Commerce Platform)  
**Архітектура:** Розподілена система з 3 обмеженими контекстами (Bounded Contexts) за принципами Domain-Driven Design (DDD) та поліглотного збереження даних (Polyglot Persistence).

---

## 1. Опис предметної області

Платформа призначена для роздрібної та дрібнооптової торгівлі автомобільними запчастинами, мастильними матеріалами, фільтрами, автоелектронікою, а також професійним слюсарним і спеціальним інструментом для станцій технічного обслуговування (СТО).

Система спроєктована з урахуванням високих навантажень (Read-Heavy каталог і сумісність за маркою/моделлю авто) та критичних фінансових транзакцій при оформленні замовлень і прийомі платежів.

---

## 2. Три обмежені контексти (Bounded Contexts)

### 2.1. Проєкт №1: Контекст замовлень та виконання (Orders & Fulfillment)
* **Технологія:** Microsoft SQL Server.
* **Призначення:** Забезпечення транзакційної надійності (ACID), фіксація історичних цін (Price Snapshotting), облік платежів, логістика відправок (Нова Пошта, Meest), аудит переходів станів замовлення.
* **Ключові особливості:**
  * **Жодних зовнішніх ключів (FK) на інші сервіси:** `ExternalUserId` та `ExternalProductId` зберігаються як скалярні значення.
  * Зв'язки: **1:1** (`Orders` <-> `OrderDeliveries`), **1:N** (`Customers` -> `Orders`, `Orders` -> `OrderItems`, `Orders` -> `PaymentTransactions`, `Orders` -> `OrderStatusHistory`), **M:N** (`Orders` <-> `Promotions` через `OrderPromotions`).
  * Поля аудиту: `CreatedAt`, `UpdatedAt`.
  * М'яке видалення: `IsDeleted` (BIT).
  * Оптимістичне блокування: `RowVersion` (ROWVERSION).
  * Транзакційні збережувані процедури, зокрема **`usp_ChangeOrderStatus`** з блокуванням рядка `WITH (UPDLOCK, ROWLOCK)` для запобігання race conditions при зміні статусів.

### 2.2. Проєкт №2: Каталог автотоварів та складський облік (Catalog & Inventory)
* **Технологія:** Microsoft SQL Server (оптимізовано для майбутнього Entity Framework Core).
* **Призначення:** Централізоване ведення номенклатури, брендів (Bosch, Brembo, Motul тощо), дерева категорій та фізичних залишків на складах.
* **Ключові особливості:**
  * 6 сутностей: `Brands`, `Categories`, `Products`, `ProductCategories`, `ProductDetails`, `InventoryStocks`.
  * Зв'язки: **1:1** (`Products` <-> `ProductDetails`), **1:N** (`Brands` -> `Products`, `Products` -> `InventoryStocks`, `Categories` -> `Categories`), **M:N** через проміжну таблицю `ProductCategories`.
  * Проміжна таблиця **`ProductCategories` містить власні поля: `IsPrimary` (BIT) та `AssignedAt` (DATETIME2)**.
  * Конвенції іменування сумісні з EF Core, присутні індекси та обмеження `CHECK`, `UNIQUE`.

### 2.3. Проєкт №3: Специфікації автотоварів, сумісність та відгуки (Vehicle Fitment, Specs & Reviews)
* **Технологія:** MongoDB (NoSQL).
* **Призначення:** Зберігання динамічних поліморфних технічних специфікацій (оливи мають в'язкість та допуски OEM, колодки — товщину та вісь, інструменти — робочий момент), каталогу сумісності з авто (TecDoc-подібна структура) та відгуків покупців.
* **Ключові особливості:**
  * Валідація документів на рівні схеми через **`$jsonSchema`**.
  * **Embedding (Вкладення):** вкладені характеристики `specifications`, масиви сертифікатів `certificates`, склад комплекту `package_contents`, критерії відгуків `ratings`, контекст авто `vehicle_context`, масив модифікацій автомобілів `compatible_vehicles`.
  * **Referencing (Посилання):** посилання на реляційні сутності: `catalog_product_id`, `product_id`, `customer_id`, `order_id`.
  * Спеціальні індекси: унікальні, складені (compound) та повнотекстові (text search).

---

## 3. Зведена таблиця дублювання даних між контекстами

| Поле / Атрибут | Джерело істини (Source) | Приймач (Target) | Таблиця / Колекція | Причина дублювання | Стратегія оновлення | Рівень узгодженості |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `ExternalProductId`, `ProductSKU`, `ProductName`, `UnitPrice` | **Проєкт №2 (Catalog)** `Products` | **Проєкт №1 (Orders)** | `OrderItems` | **Price Snapshotting:** Забезпечення незмінності фінансового чека замовлення при подальшій зміні цін або назв у каталозі. | Одноразовий знімок при оформленні замовлення. Далі запис стає незмінним (Immutable). | Strong під час покупки; далі не оновлюється. |
| `RecipientFullName`, `RecipientPhone`, `DeliveryAddress`, `City` | **Проєкт №1 (Customers)** | **Проєкт №1 (Orders)** | `OrderDeliveries` | **Address Snapshot:** Забезпечення доставки за початково вказаною адресою навіть при зміні адреси в профілі клієнта. | Фіксація під час оформлення замовлення. | Immutable після створення. |
| `catalog_product_id`, `sku`, `title`, `brand_code` | **Проєкт №2 (Catalog)** `Products`, `Brands` | **Проєкт №3 (MongoDB)** | `product_specifications` | **Швидкість пошуку:** Автономна видача специфікацій та повнотекстовий пошук без навантаження на реляційний каталог. | Асинхронні події `ProductCreatedEvent` / `ProductUpdatedEvent` через брокер повідомлень. | Eventual Consistency. |
| `product_id`, `customer_id`, `order_id` | **Проєкт №1 (Orders)** та **Проєкт №2 (Catalog)** | **Проєкт №3 (MongoDB)** | `customer_reviews` | **Verified Purchase:** Підтвердження факту реальної покупки для публікації достовірного відгуку без крос-базових JOIN. | Асинхронна подія `OrderDeliveredEvent`. | Eventual Consistency. |
| `product_id`, `oem_reference_numbers` | **Проєкт №2 (Catalog)** `Products` | **Проєкт №3 (MongoDB)** | `vehicle_fitments` | **Швидкий підбір деталей:** Миттєва фільтрація за OE кодами автовиробників (VAG, BMW, Toyota). | Асинхронна подія `ProductFitmentUpdatedEvent`. | Eventual Consistency. |

---

## 4. Структура файлів репозиторію

```text
.
├── README.md                   # Цей документ: опис домену, 3 контексти, дублювання, розгортання
├── docs/
│   ├── contexts.md             # Архітектурний опис трьох Bounded Contexts за DDD
│   ├── duplication-policy.md   # Детальна політика дублювання, денормалізації та синхронізації
│   ├── erd-p1.puml             # PlantUML ER-діаграма Проєкту №1 (Orders & Fulfillment)
│   ├── erd-p2.puml             # PlantUML ER-діаграма Проєкту №2 (Catalog & Inventory)
│   └── mongo-examples.json     # Зразкові JSON-документи MongoDB з embedding та referencing
└── db/
    ├── p1/
    │   ├── schema.sql          # SQL Server: Таблиці 1:1, 1:N, M:N, PK/FK/CHECK/UQ, аудит, IsDeleted, RowVersion
    │   ├── procedures.sql      # SQL Server: 5 збережуваних процедур, включаючи транзакційну usp_ChangeOrderStatus з UPDLOCK
    │   └── seed.sql            # SQL Server: Ідемпотентні дані з IF NOT EXISTS
    ├── p2/
    │   ├── schema.sql          # SQL Server (EF Core): 6 сутностей, 1:1, 1:N, M:N (ProductCategories з IsPrimary та AssignedAt)
    │   └── seed.sql            # SQL Server: Ідемпотентні дані для каталогу автотоварів
    └── p3/
        ├── collections.js      # MongoDB: Ініціалізація колекцій з $jsonSchema, індексами та валідацією
        └── seed.js             # MongoDB: Ідемпотентні документи з поліморфними специфікаціями та відгуками
```

---

## 5. Інструкція з розгортання

### 5.1. Попередні вимоги
Для локального запуску середовища потрібні:
* **Docker Desktop** (або встановлені окремо SQL Server 2019/2022 та MongoDB 6.0/7.0).
* Утиліти командного рядка: `sqlcmd` (або SSMS / Azure Data Studio / Rider Database Tools) та `mongosh`.

### 5.2. Швидкий запуск СУБД у Docker
Запустіть контейнери SQL Server та MongoDB однією командою:

```bash
# 1. Запуск Microsoft SQL Server
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd!" \
   -p 1433:1433 --name mssql-lab -d mcr.microsoft.com/mssql/server:2022-latest

# 2. Запуск MongoDB
docker run -p 27017:27017 --name mongo-lab -d mongo:latest
```

---

### 5.3. Розгортання Проєкту №1: Orders & Fulfillment (SQL Server)

Створіть базу даних `AutoParts_OrdersDb` та виконайте скрипти по черзі:

```powershell
# Створення бази даних
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'AutoParts_OrdersDb') CREATE DATABASE AutoParts_OrdersDb;"

# 1. Накатування схеми таблиць
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d AutoParts_OrdersDb -i db/p1/schema.sql

# 2. Створення збережуваних процедур (включаючи usp_ChangeOrderStatus)
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d AutoParts_OrdersDb -i db/p1/procedures.sql

# 3. Завантаження ідемпотентних тестових даних
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d AutoParts_OrdersDb -i db/p1/seed.sql
```

**Перевірка роботи процедури зі зміною статусу (з блокуванням `UPDLOCK`):**
```sql
USE AutoParts_OrdersDb;
-- Переведення замовлення #ORD-2026-0004 з 'Pending' у 'Paid'
EXEC dbo.usp_ChangeOrderStatus 
    @OrderId = 4, 
    @NewStatus = N'Paid', 
    @ChangedBy = N'OPERATOR_ADMIN', 
    @Reason = N'Оплата отримана на розрахунковий рахунок';

-- Перевірка результату в таблиці замовлень та журналі аудиту
SELECT OrderId, OrderNumber, OrderStatus, UpdatedAt FROM dbo.Orders WHERE OrderId = 4;
SELECT * FROM dbo.OrderStatusHistory WHERE OrderId = 4 ORDER BY ChangedAt DESC;
```

---

### 5.4. Розгортання Проєкту №2: Catalog & Inventory (SQL Server для EF Core)

Створіть окрему базу даних `AutoParts_CatalogDb` для забезпечення ізоляції контекстів:

```powershell
# Створення бази даних каталогу
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -Q "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'AutoParts_CatalogDb') CREATE DATABASE AutoParts_CatalogDb;"

# 1. Накатування схеми таблиць (з ProductCategories.IsPrimary та AssignedAt)
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d AutoParts_CatalogDb -i db/p2/schema.sql

# 2. Завантаження ідемпотентних тестових даних
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd!" -d AutoParts_CatalogDb -i db/p2/seed.sql
```

**Перевірка наявності полів IsPrimary та AssignedAt у M:N зв'язку:**
```sql
USE AutoParts_CatalogDb;
SELECT 
    p.SKU, 
    p.Name AS ProductName, 
    c.Name AS CategoryName, 
    pc.IsPrimary, 
    pc.AssignedAt
FROM dbo.ProductCategories pc
JOIN dbo.Products p ON pc.ProductId = p.Id
JOIN dbo.Categories c ON pc.CategoryId = c.Id;
```

---

### 5.5. Розгортання Проєкту №3: MongoDB (Specs, Fitment & Reviews)

Виконайте ініціалізацію колекцій з валідацією та заповнення тестовими документами:

```powershell
# 1. Створення колекцій, валідаторів $jsonSchema та індексів
mongosh "mongodb://localhost:27017" db/p3/collections.js

# 2. Завантаження ідемпотентних даних
mongosh "mongodb://localhost:27017" db/p3/seed.js
```

**Перевірка роботи запитів у MongoDB:**
```javascript
use autoparts_telemetry_reviews;

// 1. Пошук сумісних колодок для Volkswagen Passat B8
db.vehicle_fitments.find({
    "compatible_vehicles.make": "Volkswagen",
    "compatible_vehicles.model": "Passat"
}, { sku: 1, oem_reference_numbers: 1, "compatible_vehicles.$": 1 });

// 2. Вибірка відгуків з високим рейтингом та контекстом авто
db.customer_reviews.find(
    { "ratings.overall": 5 },
    { author_name: 1, headline: 1, "vehicle_context.model": 1, ratings: 1 }
);
```
