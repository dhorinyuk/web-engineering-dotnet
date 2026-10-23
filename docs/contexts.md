# Bounded Contexts Architecture

## Overview

The platform is designed around Domain-Driven Design (DDD) principles with decoupled bounded contexts, polyglot persistence, and asynchronous integration.

```
                    +-----------------------------+
                    |  Catalog Context (P2)       |
                    |  SQL Server (EF Core)       |
                    +-----------------------------+
                                   |
         ProductCreatedEvent       | ProductCreatedEvent
         ProductPriceChangedEvent  |
                                   v
+-----------------------------+   +-----------------------------+
|  Ordering Context (P1)      |   |  Reviews Context (P3)       |
|  SQL Server (OLTP)          |   |  MongoDB (Document Store)   |
+-----------------------------+   +-----------------------------+
```

---

## 1. Ordering Context (db/p1)

* **Purpose:** Handles customer order placement, transactional state machine progression, payment registration, and shipping execution.
* **Storage:** Microsoft SQL Server.
* **Aggregates and Entities:**
  * `Orders`: Aggregate root controlling order lifecycle and total amount calculations.
  * `OrderDetails`: 1:1 delivery metadata entity (recipient, shipping destination, tracking information).
  * `OrderItems`: M:N line items joining orders with local product projections.
  * `Customers`: Context-local customer profile referencing external identity through `ExternalId`.
  * `Products`: Local catalog cache maintained without cross-database foreign keys.
  * `OrderStatusHistory`: Immutable transition audit log.
* **Concurrency and Integrity:** Optimistic concurrency via `RowVersion`, exclusive state transitions via `SELECT ... WITH (UPDLOCK, ROWLOCK)`, soft delete (`IsDeleted`).

---

## 2. Catalog Context (db/p2)

* **Purpose:** Master repository for vehicle spare parts, manufacturer brands, taxonomy hierarchy, and product specifications.
* **Storage:** Microsoft SQL Server configured for Entity Framework Core.
* **Entities:**
  * `Brands`: Manufacturer metadata (Bosch, Brembo, Motul, Toptul).
  * `Categories`: Self-referencing hierarchical category graph.
  * `Products`: Core product entity with inventory availability and pricing.
  * `ProductDetails`: 1:1 technical specifications and warranty parameters.
  * `ProductCategories`: Explicit M:N join entity with payload attributes (`IsPrimary`, `AssignedAt`).
  * `ProductReviewsSummary`: Local read-model cache updated asynchronously from the reviews context.
* **Integration Role:** Single Source of Truth for product identification (`Sku`, `Price`, `Name`).

---

## 3. Reviews and Feedback Context (db/p3)

* **Purpose:** Customer feedback collection, multi-tier rating evaluations, and community discussion threads.
* **Storage:** MongoDB (`ReviewsServiceDb`).
* **Collections and Document Design:**
  * `reviews`: Root document storing denormalized product identifiers, structured rating attributes, embedded author information, embedded comment arrays, and referencing links to discussions.
  * `discussions`: Dedicated collection containing threaded Q&A and support discussions linked to reviews and products.
  * `ratings`: Analytical read-model storing rating score distributions and aggregated averages per product.
* **Modeling Paradigms:**
  * **Embedding:** Embedded author metadata, embedded review comment replies, embedded photo attachments.
  * **Referencing:** `productId` referencing catalog identity, `discussionId` referencing the discussion collection thread.
