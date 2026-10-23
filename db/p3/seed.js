/**
 * ============================================================================
 * Проєкт №3: Ідемпотентне наповнення тестовими даними (Seed Data)
 * СУБД: MongoDB
 * Призначення: Демонстрація гнучкої поліморфної схеми, вбудованих масивів
 *              (Embedding) та міжконтекстних посилань (Referencing).
 * ============================================================================
 */

const targetDb = db.getSiblingDB("autoparts_telemetry_reviews");

print("[INFO] Початок завантаження тестових даних у MongoDB...");

// ----------------------------------------------------------------------------
// 1. Поліморфні специфікації автотоварів (product_specifications)
// Демонструє різний склад характеристик для різних типів товарів.
// ----------------------------------------------------------------------------

const sampleSpecifications = [
    {
        catalog_product_id: 1,
        sku: "MOT-8100-5W40-5L",
        title: "Моторна олива Motul 8100 X-cess Gen2 5W-40 5л",
        product_type: "engine_oil",
        brand_code: "MOTUL",
        specifications: {
            viscosity_sae: "5W-40",
            oil_base_type: "Fully Synthetic (100% Synthetic)",
            volume_liters: 5.0,
            density_at_20c_g_cm3: 0.851,
            flash_point_celsius: 228,
            pour_point_celsius: -36,
            ace_standards: ["A3", "B4"],
            api_standards: ["SN", "CF"],
            oem_approvals: [
                "BMW LongLife-01",
                "MB-Approval 229.5",
                "Porsche A40",
                "VW 502 00 / 505 00",
                "Renault RN0710 / RN0700"
            ],
            search_tags: ["motul", "8100", "5w40", "синтетика", "моторне масло", "vw 502 00"]
        },
        certificates: [
            {
                standard: "ISO 9001:2015",
                certificate_number: "CERT-FR-2023-8841",
                issuer: "TÜV Rheinland",
                valid_until: "2027-12-31"
            }
        ],
        package_contents: [
            { item: "Пластикова каністра 5л з захисною пломбою", quantity: 1 }
        ],
        created_at: new Date("2026-01-15T09:30:00Z"),
        updated_at: new Date("2026-02-10T14:20:00Z")
    },
    {
        catalog_product_id: 2,
        sku: "BRM-P85020",
        title: "Комплект передніх гальмівних колодок Brembo P 85 020",
        product_type: "brake_pads",
        brand_code: "BREMBO",
        specifications: {
            axle_position: "Front",
            braking_system: "Lucas / TRW",
            thickness_mm: 19.8,
            width_mm: 146.0,
            height_mm: 54.7,
            wear_indicator_included: true,
            wear_indicator_type: "Acoustic / Electronic contact",
            wva_numbers: ["20961", "23587", "23588"],
            friction_material: "Low-Metallic Ceramic blend",
            search_tags: ["brembo", "p85020", "гальмівні колодки", "передні колодки", "vag"]
        },
        certificates: [
            {
                standard: "ECE R90",
                certificate_number: "E11-90R-01124/491",
                issuer: "VCA United Kingdom",
                valid_until: "2029-06-30"
            }
        ],
        package_contents: [
            { item: "Гальмівні колодки", quantity: 4 },
            { item: "Притискні пружинні скоби", quantity: 4 },
            { item: "Інструкція зі встановлення", quantity: 1 }
        ],
        created_at: new Date("2026-01-18T11:15:00Z"),
        updated_at: new Date("2026-01-20T10:00:00Z")
    },
    {
        catalog_product_id: 3,
        sku: "TOP-ANAM1621",
        title: "Ключ динамометричний Toptul 1/2\" 40-210 Нм (ANAM1621)",
        product_type: "hand_tool",
        brand_code: "TOPTUL",
        specifications: {
            tool_category: "torque_wrench",
            drive_square_inch: "1/2\"",
            torque_range_min_nm: 40.0,
            torque_range_max_nm: 210.0,
            scale_graduation_nm: 1.0,
            accuracy_percentage: 4.0,
            total_length_mm: 535,
            ratchet_teeth_count: 24,
            mechanism_material: "Chrome-Vanadium steel (Cr-V)",
            locking_mechanism: "Push-pull ring lock at base",
            search_tags: ["toptul", "динамометричний ключ", "anam1621", "інструмент для авто", "1/2 210nm"]
        },
        certificates: [
            {
                standard: "DIN EN ISO 6789",
                certificate_number: "CALIB-TPT-90422",
                issuer: "Factory Calibration Laboratory TOPTUL",
                valid_until: "2027-01-01"
            }
        ],
        package_contents: [
            { item: "Динамометричний ключ 1/2\"", quantity: 1 },
            { item: "Ударостійкий пластиковий футляр", quantity: 1 },
            { item: "Індивідуальний сертифікат заводського калібрування", quantity: 1 }
        ],
        created_at: new Date("2026-01-20T16:45:00Z"),
        updated_at: new Date("2026-02-01T12:00:00Z")
    },
    {
        catalog_product_id: 4,
        sku: "BOS-S4-60AH",
        title: "Акумулятор автомобільний Bosch S4 005 Silver 60Ah 540A",
        product_type: "battery",
        brand_code: "BOSCH",
        specifications: {
            battery_technology: "Lead-Acid / Calcium (Ca/Ca)",
            capacity_ah: 60,
            cold_cranking_amps_en: 540,
            voltage: 12,
            polarity: "Right Positive (0 / R+)",
            terminal_type: "Standard European (Type 1)",
            base_hold_down: "B13",
            dimensions_mm: { length: 242, width: 175, height: 190 },
            maintenance_free: true,
            search_tags: ["bosch", "s4", "акумулятор 60ah", "540a", "автомобільний акумулятор"]
        },
        certificates: [
            {
                standard: "EN 50342-1",
                certificate_number: "BOSCH-BAT-DE-8109",
                issuer: "VDA QMC",
                valid_until: "2028-12-31"
            }
        ],
        package_contents: [
            { item: "Акумуляторна батарея 60Ah", quantity: 1 },
            { item: "Захисні ковпачки для клем", quantity: 2 },
            { item: "Гарантійний талон з інструкцією", quantity: 1 }
        ],
        created_at: new Date("2026-01-22T08:00:00Z"),
        updated_at: new Date("2026-02-05T11:00:00Z")
    }
];

// Ідемпотентне оновлення/вставка специфікацій через upsert
sampleSpecifications.forEach((spec) => {
    targetDb.product_specifications.updateOne(
        { catalog_product_id: spec.catalog_product_id },
        { $set: spec },
        { upsert: true }
    );
});
print(`[SUCCESS] Оновлено/вставлено ${sampleSpecifications.length} специфікацій товарів.`);

// ----------------------------------------------------------------------------
// 2. Відгуки покупців (customer_reviews)
// Демонструє вкладені оцінки, контекст автомобіля та фотозвіти.
// ----------------------------------------------------------------------------

const sampleReviews = [
    {
        product_id: 1,
        customer_id: 1,
        order_id: 1,
        is_verified_purchase: true,
        author_name: "Олександр Коваленко",
        headline: "Чудове мастило для 2.0 TSI (VW Passat B8)",
        comment: "Заливаю це мастило вже третій сезон у свій Passat B8 2.0 TSI. Мотор працює помітно тихіше, ніж на дилерському мастилі. Жодного масляного голодування або чаду при поїздках автобаном на високих швидкостях. Взимку при -22°C двигун запустився без жодних проблем. Оригінал 100%, перевірив QR-код на кришці.",
        ratings: {
            overall: 5,
            quality: 5,
            price_value: 4,
            packaging: 5
        },
        vehicle_context: {
            make: "Volkswagen",
            model: "Passat",
            generation: "B8",
            year: 2018,
            engine: "2.0 TSI (CHHB 220 hp)",
            mileage_km: 115000
        },
        photos: [
            {
                url: "https://cdn.autoparts.ua/reviews/2026/02/motul_canister_qr_check.jpg",
                uploaded_at: new Date("2026-02-12T18:32:00Z")
            },
            {
                url: "https://cdn.autoparts.ua/reviews/2026/02/oil_change_service.jpg",
                uploaded_at: new Date("2026-02-12T18:33:00Z")
            }
        ],
        helpful_votes: {
            upvotes: 14,
            downvotes: 1
        },
        created_at: new Date("2026-02-12T18:35:00Z"),
        updated_at: new Date("2026-02-12T18:35:00Z")
    },
    {
        product_id: 2,
        customer_id: 2,
        order_id: 2,
        is_verified_purchase: true,
        author_name: "Ірина Шевченко",
        headline: "Колодки не скриплять, чудова якість Brembo",
        comment: "Поставили на автосервісі замість зношених рідних колодок на Skoda Octavia A7. Проїхала вже 3000 км — гальмування дуже плавне та прогнозоване, немає огидного писку навіть у вологу дощову погоду. Диски не затирають, чорного нальоту на литих дисках значно менше.",
        ratings: {
            overall: 5,
            quality: 5,
            price_value: 5,
            packaging: 5
        },
        vehicle_context: {
            make: "Skoda",
            model: "Octavia",
            generation: "A7 (5E3)",
            year: 2017,
            engine: "1.4 TSI (CZDA 150 hp)",
            mileage_km: 88000
        },
        photos: [],
        helpful_votes: {
            upvotes: 8,
            downvotes: 0
        },
        created_at: new Date("2026-02-14T10:15:00Z"),
        updated_at: new Date("2026-02-14T10:15:00Z")
    },
    {
        product_id: 3,
        customer_id: 3,
        order_id: 3,
        is_verified_purchase: true,
        author_name: "Максим Бондаренко",
        headline: "Незамінний інструмент для гаража та затяжки колісних болтів",
        comment: "Купив для особистого обслуговування авто (заміна коліс зима/літо, свічок та підвіски). Чітке та дзвінке клацання при досягненні заданого моменту. Шкала нанесена лазером, читається дуже легко. У комплекті був сертифікат з індивідуальним серійним номером.",
        ratings: {
            overall: 5,
            quality: 5,
            price_value: 5,
            packaging: 5
        },
        vehicle_context: {
            make: "Toyota",
            model: "RAV4",
            generation: "XA50",
            year: 2021,
            engine: "2.0 Dynamic Force (M20A-FKS)",
            mileage_km: 42000
        },
        photos: [
            {
                url: "https://cdn.autoparts.ua/reviews/2026/02/toptul_case_open.jpg",
                uploaded_at: new Date("2026-02-19T09:00:00Z")
            }
        ],
        helpful_votes: {
            upvotes: 5,
            downvotes: 0
        },
        created_at: new Date("2026-02-19T09:10:00Z"),
        updated_at: new Date("2026-02-19T09:10:00Z")
    }
];

sampleReviews.forEach((review) => {
    targetDb.customer_reviews.updateOne(
        { product_id: review.product_id, customer_id: review.customer_id },
        { $set: review },
        { upsert: true }
    );
});
print(`[SUCCESS] Оновлено/вставлено ${sampleReviews.length} відгуків покупців.`);

// ----------------------------------------------------------------------------
// 3. Каталог сумісності з авто (vehicle_fitments)
// Демонструє OE номери та розгорнуті модифікації підтримуваних авто.
// ----------------------------------------------------------------------------

const sampleFitments = [
    {
        product_id: 2,
        sku: "BRM-P85020",
        oem_reference_numbers: [
            "1K0698151",
            "1K0698151C",
            "1K0698151E",
            "3C0698151A",
            "5K0698151",
            "JZW698151B"
        ],
        compatible_vehicles: [
            {
                make: "Volkswagen",
                model: "Golf",
                generation: "VII (5G1, BQ1)",
                year_from: 2012,
                year_to: 2020,
                engine: "1.4 TSI",
                engine_codes: ["CZCA", "CPVA", "CHPA"],
                power_hp: 125,
                fuel_type: "Petrol",
                axle: "Front",
                notes: "Для автомобілів з гальмівними дисками PR-1ZA, PR-1ZE діаметром 288 мм"
            },
            {
                make: "Volkswagen",
                model: "Passat",
                generation: "B8 (3G2, CB2)",
                year_from: 2014,
                year_to: 2023,
                engine: "2.0 TDI",
                engine_codes: ["DFCA", "DDAA"],
                power_hp: 190,
                fuel_type: "Diesel",
                axle: "Front",
                notes: "Сумісні з передньою віссю TRW"
            },
            {
                make: "Skoda",
                model: "Octavia",
                generation: "III (5E3, NL3)",
                year_from: 2012,
                year_to: 2020,
                engine: "1.4 TSI",
                engine_codes: ["CHPA", "CZDA"],
                power_hp: 150,
                fuel_type: "Petrol",
                axle: "Front",
                notes: "Платформа MQB"
            },
            {
                make: "Audi",
                model: "A3",
                generation: "8V (8V1, 8VK)",
                year_from: 2012,
                year_to: 2020,
                engine: "2.0 TDI",
                engine_codes: ["CRBC", "CRLB"],
                power_hp: 150,
                fuel_type: "Diesel",
                axle: "Front",
                notes: "Передні вентильовані диски"
            }
        ],
        updated_at: new Date("2026-02-15T08:00:00Z")
    },
    {
        product_id: 5,
        sku: "FLT-MAN-W712",
        oem_reference_numbers: [
            "04E115561H",
            "04E115561D",
            "04E115561B",
            "04E115561"
        ],
        compatible_vehicles: [
            {
                make: "Volkswagen",
                model: "Golf",
                generation: "VII (5G1, BQ1)",
                year_from: 2012,
                year_to: 2020,
                engine: "1.2 TSI / 1.4 TSI",
                engine_codes: ["CJZA", "CZCA", "CHPA"],
                power_hp: 110,
                fuel_type: "Petrol",
                axle: "Engine Bay",
                notes: "Моторна група EA211"
            },
            {
                make: "Skoda",
                model: "Rapid",
                generation: "NH3",
                year_from: 2015,
                year_to: 2022,
                engine: "1.6 MPI",
                engine_codes: ["CWVA", "CWVB"],
                power_hp: 110,
                fuel_type: "Petrol",
                axle: "Engine Bay",
                notes: "Двигун 1.6 серії EA211"
            }
        ],
        updated_at: new Date("2026-02-16T12:00:00Z")
    }
];

sampleFitments.forEach((fitment) => {
    targetDb.vehicle_fitments.updateOne(
        { product_id: fitment.product_id },
        { $set: fitment },
        { upsert: true }
    );
});
print(`[SUCCESS] Оновлено/вставлено ${sampleFitments.length} записів сумісності авто.`);

print("[SUCCESS] Ідемпотентне наповнення MongoDB завершено успішно!");
