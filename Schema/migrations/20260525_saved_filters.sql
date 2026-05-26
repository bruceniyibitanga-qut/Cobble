-- Adds the dynamic filter preset table to existing development databases.
-- Fresh databases already receive this table from schema.sql.

CREATE TABLE saved_filters (
    id                CHAR(36) PRIMARY KEY,
    user_id           CHAR(36) NOT NULL,
    entity            VARCHAR(100) NOT NULL,
    name              VARCHAR(255) NOT NULL,
    filters           JSON NOT NULL,
    created_at        DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),

    FOREIGN KEY (user_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE INDEX idx_saved_filters_user_entity ON saved_filters (user_id, entity);
