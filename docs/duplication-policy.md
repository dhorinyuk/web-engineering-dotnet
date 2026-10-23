# Data Duplication and Consistency Policy

## Architectural Rationale

In distributed microservices, maintaining database-level foreign keys across service boundaries creates runtime dependencies, limits horizontal scalability, and prevents independent schema migrations. Data duplication is applied deliberately to achieve operational autonomy, historical immutability, and low-latency queries.

---

## Duplication Matrix

| Attribute | Source of Truth | Target Context | Target Table / Document | Strategy | Consistency Guarantee |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `ProductName`, `UnitPrice` | `CatalogDb.dbo.Products` | `OrderingDb` (P1) | `dbo.OrderItems` | **Price Snapshot:** Captured once upon order item creation via `usp_AddOrderItem`. Preserves historical legal and fiscal accuracy regardless of future catalog price changes. | Strong at creation, immutable thereafter |
| `RecipientName`, `RecipientPhone`, `ShippingAddress` | Identity / Customer Profile | `OrderingDb` (P1) | `dbo.OrderDetails` | **Delivery Snapshot:** Captured when an order is submitted to guarantee delivery to the specified location even if the customer changes their profile address later. | Strong at creation, immutable thereafter |
| `productName` | `CatalogDb.dbo.Products` | `ReviewsServiceDb` (P3) | `reviews` collection | **Read Denormalization:** Included in review documents to avoid cross-service joins during catalog and review feed browsing. | Eventual consistency via `ProductUpdatedEvent` |
| `AverageRating`, `TotalReviewsCount` | `ReviewsServiceDb` (P3) | `CatalogDb` (P2) | `dbo.ProductReviewsSummary` | **Aggregate Projection:** Summary scores calculated in MongoDB and pushed to SQL Server catalog cache for sorting products by rating. | Eventual consistency via `ReviewCreatedEvent` |

---

## Consistency Models

### 1. Immutable Snapshots (Ordering Context)
* Target fields: `OrderItems.ProductName`, `OrderItems.UnitPrice`, `OrderDetails.ShippingAddress`.
* Lifecycle: Write-once upon order creation. Subsequent modifications to catalog prices or user profiles never propagate to submitted orders.

### 2. Eventual Consistency (Catalog & Reviews Contexts)
* Target fields: `reviews.productName`, `ProductReviewsSummary.AverageRating`.
* Delivery Mechanism: Transactional outbox on the producer side, message broker publication (RabbitMQ/Kafka), and idempotent consumer processing on the subscriber side.
* Recovery: In case of transient messaging downtime, compensating reconciliation jobs synchronize delta changes using high-watermark timestamps.
