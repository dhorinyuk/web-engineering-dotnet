/**
 * ============================================================================
 * Проєкт №3: Специфікації автотоварів, сумісність та відгуки
 * СУБД: MongoDB
 * Призначення: Ініціалізація колекцій з валідацією $jsonSchema, індексами,
 *              демонстрацією підходів Embedding та Referencing.
 * ============================================================================
 */

const targetDb = db.getSiblingDB("autoparts_telemetry_reviews");

print("[INFO] Початок налаштування колекцій у базі autoparts_telemetry_reviews...");

// ============================================================================
// 1. Колекція: product_specifications
// Демонструє поліморфну гнучку схему (Flexible Schema) для специфікацій олив,
// колодок, інструментів, акумуляторів.
// Referencing: catalog_product_id -> посилання на Product.Id з SQL Server (P2).
// Embedding: specifications (вбудований документ параметрів),
//            certificates (вбудований масив), package_contents (вбудований масив).
// ============================================================================

const productSpecsSchema = {
    $jsonSchema: {
        bsonType: "object",
        required: ["catalog_product_id", "sku", "title", "product_type", "brand_code", "specifications"],
        properties: {
            catalog_product_id: {
                bsonType: ["int", "long", "double"],
                description: "Посилання на Product.Id у SQL Server Каталогу (P2) - Referencing"
            },
            sku: {
                bsonType: "string",
                description: "Унікальний артикул товару"
            },
            title: {
                bsonType: "string",
                description: "Повна назва товару для пошуку"
            },
            product_type: {
                enum: ["engine_oil", "brake_pads", "hand_tool", "battery", "filter", "spark_plug"],
                description: "Тип номенклатури для поліморфної валідації"
            },
            brand_code: {
                bsonType: "string",
                description: "Код бренду (BOSCH, BREMBO, MOTUL тощо)"
            },
            specifications: {
                bsonType: "object",
                description: "Гнучкий вбудований документ характеристик (Embedding)"
            },
            certificates: {
                bsonType: "array",
                description: "Масив сертифікатів відповідності (Embedding)",
                items: {
                    bsonType: "object",
                    required: ["standard", "certificate_number"],
                    properties: {
                        standard: { bsonType: "string" },
                        certificate_number: { bsonType: "string" },
                        issuer: { bsonType: "string" },
                        valid_until: { bsonType: "string" }
                    }
                }
            },
            package_contents: {
                bsonType: "array",
                description: "Склад заводського комплекту (Embedding)",
                items: {
                    bsonType: "object",
                    required: ["item", "quantity"],
                    properties: {
                        item: { bsonType: "string" },
                        quantity: { bsonType: ["int", "long", "double"] }
                    }
                }
            },
            created_at: {
                bsonType: ["date", "string"]
            },
            updated_at: {
                bsonType: ["date", "string"]
            }
        }
    }
};

// Створення або оновлення валідатора колекції product_specifications
const existingCols = targetDb.getCollectionNames();

if (!existingCols.includes("product_specifications")) {
    targetDb.createCollection("product_specifications", {
        validator: productSpecsSchema,
        validationLevel: "moderate",
        validationAction: "error"
    });
    print("[SUCCESS] Колекцію product_specifications створено.");
} else {
    targetDb.runCommand({
        collMod: "product_specifications",
        validator: productSpecsSchema,
        validationLevel: "moderate",
        validationAction: "error"
    });
    print("[INFO] Валідатор для product_specifications оновлено.");
}

// Індекси для product_specifications
targetDb.product_specifications.createIndex({ catalog_product_id: 1 }, { unique: true, name: "uq_catalog_product_id" });
targetDb.product_specifications.createIndex({ sku: 1 }, { unique: true, name: "uq_sku" });
targetDb.product_specifications.createIndex({ product_type: 1 }, { name: "ix_product_type" });
targetDb.product_specifications.createIndex(
    { 
        title: "text", 
        "specifications.search_tags": "text", 
        "specifications.oem_approvals": "text" 
    }, 
    { name: "text_search_title_tags", default_language: "russian" }
);

// ============================================================================
// 2. Колекція: customer_reviews
// Демонструє збір детальних відгуків покупців автотоварів.
// Referencing: product_id (P2), customer_id (P1), order_id (P1).
// Embedding: ratings (об'єкт критеріїв оцінки: якість, ціна, пакування),
//            vehicle_context (автомобіль, на який встановлювалась деталь),
//            photos (масив завантажених фотозвітів), helpful_votes.
// ============================================================================

const customerReviewsSchema = {
    $jsonSchema: {
        bsonType: "object",
        required: ["product_id", "customer_id", "ratings", "headline", "comment"],
        properties: {
            product_id: {
                bsonType: ["int", "long", "double"],
                description: "Посилання на ID товару (Referencing)"
            },
            customer_id: {
                bsonType: ["int", "long", "double"],
                description: "Посилання на CustomerId з P1 (Referencing)"
            },
            order_id: {
                bsonType: ["int", "long", "double", "null"],
                description: "Посилання на OrderId з P1 для Verified Purchase"
            },
            is_verified_purchase: {
                bsonType: "bool"
            },
            author_name: {
                bsonType: "string"
            },
            headline: {
                bsonType: "string"
            },
            comment: {
                bsonType: "string"
            },
            ratings: {
                bsonType: "object",
                required: ["overall"],
                properties: {
                    overall: { bsonType: ["int", "long", "double"], minimum: 1, maximum: 5 },
                    quality: { bsonType: ["int", "long", "double"], minimum: 1, maximum: 5 },
                    price_value: { bsonType: ["int", "long", "double"], minimum: 1, maximum: 5 },
                    packaging: { bsonType: ["int", "long", "double"], minimum: 1, maximum: 5 }
                }
            },
            vehicle_context: {
                bsonType: "object",
                properties: {
                    make: { bsonType: "string" },
                    model: { bsonType: "string" },
                    generation: { bsonType: "string" },
                    year: { bsonType: ["int", "long", "double"] },
                    engine: { bsonType: "string" },
                    mileage_km: { bsonType: ["int", "long", "double"] }
                }
            },
            photos: {
                bsonType: "array",
                items: {
                    bsonType: "object",
                    required: ["url"],
                    properties: {
                        url: { bsonType: "string" },
                        uploaded_at: { bsonType: ["date", "string"] }
                    }
                }
            },
            helpful_votes: {
                bsonType: "object",
                properties: {
                    upvotes: { bsonType: ["int", "long", "double"] },
                    downvotes: { bsonType: ["int", "long", "double"] }
                }
            },
            created_at: {
                bsonType: ["date", "string"]
            },
            updated_at: {
                bsonType: ["date", "string"]
            }
        }
    }
};

if (!existingCols.includes("customer_reviews")) {
    targetDb.createCollection("customer_reviews", {
        validator: customerReviewsSchema,
        validationLevel: "moderate",
        validationAction: "error"
    });
    print("[SUCCESS] Колекцію customer_reviews створено.");
} else {
    targetDb.runCommand({
        collMod: "customer_reviews",
        validator: customerReviewsSchema,
        validationLevel: "moderate",
        validationAction: "error"
    });
    print("[INFO] Валідатор для customer_reviews оновлено.");
}

// Індекси для customer_reviews
targetDb.customer_reviews.createIndex({ product_id: 1, created_at: -1 }, { name: "ix_product_reviews_date" });
targetDb.customer_reviews.createIndex({ customer_id: 1 }, { name: "ix_customer_id" });
targetDb.customer_reviews.createIndex({ "ratings.overall": -1 }, { name: "ix_ratings_overall" });

// ============================================================================
// 3. Колекція: vehicle_fitments
// Демонструє каталог автосумісності запчастин (Fitment / TecDoc model).
// Referencing: product_id (P2), sku.
// Embedding: oem_reference_numbers (масив оригінальних номерів OE),
//            compatible_vehicles (вбудований масив модифікацій автомобілів).
// ============================================================================

const vehicleFitmentsSchema = {
    $jsonSchema: {
        bsonType: "object",
        required: ["product_id", "sku", "compatible_vehicles"],
        properties: {
            product_id: {
                bsonType: ["int", "long", "double"],
                description: "Посилання на Product.Id (Referencing)"
            },
            sku: {
                bsonType: "string"
            },
            oem_reference_numbers: {
                bsonType: "array",
                items: { bsonType: "string" },
                description: "Масив оригінальних OE/OEM номерів запчастини"
            },
            compatible_vehicles: {
                bsonType: "array",
                description: "Вбудований масив підтримуваних автомобілів (Embedding)",
                items: {
                    bsonType: "object",
                    required: ["make", "model", "year_from"],
                    properties: {
                        make: { bsonType: "string" },
                        model: { bsonType: "string" },
                        generation: { bsonType: "string" },
                        year_from: { bsonType: ["int", "long", "double"] },
                        year_to: { bsonType: ["int", "long", "double", "null"] },
                        engine: { bsonType: "string" },
                        engine_codes: {
                            bsonType: "array",
                            items: { bsonType: "string" }
                        },
                        power_hp: { bsonType: ["int", "long", "double"] },
                        fuel_type: { bsonType: "string" },
                        axle: { bsonType: "string" },
                        notes: { bsonType: "string" }
                    }
                }
            },
            updated_at: {
                bsonType: ["date", "string"]
            }
        }
    }
};

if (!existingCols.includes("vehicle_fitments")) {
    targetDb.createCollection("vehicle_fitments", {
        validator: vehicleFitmentsSchema,
        validationLevel: "moderate",
        validationAction: "error"
    });
    print("[SUCCESS] Колекцію vehicle_fitments створено.");
} else {
    targetDb.runCommand({
        collMod: "vehicle_fitments",
        validator: vehicleFitmentsSchema,
        validationLevel: "moderate",
        validationAction: "error"
    });
    print("[INFO] Валідатор для vehicle_fitments оновлено.");
}

// Індекси для vehicle_fitments
targetDb.vehicle_fitments.createIndex({ product_id: 1 }, { unique: true, name: "uq_fitment_product_id" });
targetDb.vehicle_fitments.createIndex({ sku: 1 }, { name: "ix_fitment_sku" });
targetDb.vehicle_fitments.createIndex({ oem_reference_numbers: 1 }, { name: "ix_oem_numbers" });
targetDb.vehicle_fitments.createIndex(
    { 
        "compatible_vehicles.make": 1, 
        "compatible_vehicles.model": 1, 
        "compatible_vehicles.year_from": 1 
    }, 
    { name: "ix_vehicle_search" }
);

print("[SUCCESS] Ініціалізація структури MongoDB завершена успішно.");
