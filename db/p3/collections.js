// Reviews and customer feedback database initialization (MongoDB)

const targetDb = db.getSiblingDB("ReviewsServiceDb");

// Schema validator for reviews collection with embedding and referencing
const reviewsSchema = {
    $jsonSchema: {
        bsonType: "object",
        required: ["productId", "productName", "rating", "author"],
        properties: {
            productId: {
                bsonType: ["int", "long", "double"],
                description: "Cross-service reference to catalog product identifier"
            },
            productName: {
                bsonType: "string",
                description: "Denormalized product name snapshot for read performance"
            },
            rating: {
                bsonType: ["int", "long", "double"],
                minimum: 1,
                maximum: 5,
                description: "Customer satisfaction rating score"
            },
            headline: {
                bsonType: "string"
            },
            content: {
                bsonType: "string"
            },
            author: {
                bsonType: "object",
                required: ["name"],
                properties: {
                    userId: { bsonType: ["string", "null"] },
                    name: { bsonType: "string" },
                    email: { bsonType: "string" },
                    isVerifiedBuyer: { bsonType: "bool" }
                }
            },
            comments: {
                bsonType: "array",
                description: "Embedded collection of replies and customer remarks",
                items: {
                    bsonType: "object",
                    required: ["commentId", "authorName", "commentText", "createdAt"],
                    properties: {
                        commentId: { bsonType: "string" },
                        authorName: { bsonType: "string" },
                        commentText: { bsonType: "string" },
                        createdAt: { bsonType: ["date", "string"] }
                    }
                }
            },
            photos: {
                bsonType: "array",
                items: { bsonType: "string" }
            },
            discussionId: {
                bsonType: ["objectId", "null"],
                description: "Referenced identifier to discussions collection"
            },
            createdAt: {
                bsonType: ["date", "string"]
            },
            updatedAt: {
                bsonType: ["date", "string"]
            }
        }
    }
};

// Schema validator for discussions collection
const discussionsSchema = {
    $jsonSchema: {
        bsonType: "object",
        required: ["productId", "topic", "createdAt"],
        properties: {
            productId: {
                bsonType: ["int", "long", "double"]
            },
            reviewId: {
                bsonType: ["objectId", "null"]
            },
            topic: {
                bsonType: "string"
            },
            status: {
                enum: ["Open", "Closed", "Locked"]
            },
            messages: {
                bsonType: "array",
                items: {
                    bsonType: "object",
                    required: ["author", "messageText", "sentAt"],
                    properties: {
                        author: { bsonType: "string" },
                        messageText: { bsonType: "string" },
                        sentAt: { bsonType: ["date", "string"] }
                    }
                }
            },
            createdAt: {
                bsonType: ["date", "string"]
            }
        }
    }
};

// Schema validator for ratings summary collection
const ratingsSchema = {
    $jsonSchema: {
        bsonType: "object",
        required: ["productId", "averageRating", "totalRatings"],
        properties: {
            productId: {
                bsonType: ["int", "long", "double"]
            },
            averageRating: {
                bsonType: ["double", "int"]
            },
            totalRatings: {
                bsonType: ["int", "long", "double"]
            },
            breakdown: {
                bsonType: "object",
                properties: {
                    star1: { bsonType: ["int", "long", "double"] },
                    star2: { bsonType: ["int", "long", "double"] },
                    star3: { bsonType: ["int", "long", "double"] },
                    star4: { bsonType: ["int", "long", "double"] },
                    star5: { bsonType: ["int", "long", "double"] }
                }
            },
            updatedAt: {
                bsonType: ["date", "string"]
            }
        }
    }
};

const collections = targetDb.getCollectionNames();

// Initialize or update reviews collection
if (!collections.includes("reviews")) {
    targetDb.createCollection("reviews", { validator: reviewsSchema, validationLevel: "moderate" });
} else {
    targetDb.runCommand({ collMod: "reviews", validator: reviewsSchema, validationLevel: "moderate" });
}

// Initialize or update discussions collection
if (!collections.includes("discussions")) {
    targetDb.createCollection("discussions", { validator: discussionsSchema, validationLevel: "moderate" });
} else {
    targetDb.runCommand({ collMod: "discussions", validator: discussionsSchema, validationLevel: "moderate" });
}

// Initialize or update ratings collection
if (!collections.includes("ratings")) {
    targetDb.createCollection("ratings", { validator: ratingsSchema, validationLevel: "moderate" });
} else {
    targetDb.runCommand({ collMod: "ratings", validator: ratingsSchema, validationLevel: "moderate" });
}

// Optimization indexes
targetDb.reviews.createIndex({ productId: 1, createdAt: -1 }, { name: "ix_reviews_product_created" });
targetDb.reviews.createIndex({ rating: 1 }, { name: "ix_reviews_rating" });
targetDb.reviews.createIndex({ discussionId: 1 }, { name: "ix_reviews_discussion", sparse: true });

targetDb.discussions.createIndex({ productId: 1 }, { name: "ix_discussions_product" });
targetDb.discussions.createIndex({ reviewId: 1 }, { name: "ix_discussions_review", sparse: true });

targetDb.ratings.createIndex({ productId: 1 }, { unique: true, name: "uq_ratings_product" });
