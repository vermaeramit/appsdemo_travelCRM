-- ============================================================================
-- Tenant DB: master data (destinations, hotels, suppliers, services, etc.)
-- ============================================================================

CREATE TABLE destinations (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name          VARCHAR(150) NOT NULL,
    parent_id     UUID REFERENCES destinations(id),
    type          VARCHAR(20) NOT NULL DEFAULT 'City', -- Country/State/City/Region
    country       VARCHAR(100),
    state         VARCHAR(100),
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted    BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at    TIMESTAMPTZ,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by    UUID,
    updated_at    TIMESTAMPTZ,
    updated_by    UUID
);
CREATE INDEX ix_destinations_parent ON destinations(parent_id);

CREATE TABLE suppliers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200) NOT NULL,
    type            VARCHAR(50) NOT NULL DEFAULT 'Hotel', -- Hotel/Transport/Activity/DMC/Other
    contact_person  VARCHAR(150),
    email           VARCHAR(150),
    phone           VARCHAR(30),
    address         TEXT,
    gstin           VARCHAR(20),
    pan             VARCHAR(20),
    payment_terms   VARCHAR(100),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID
);

CREATE TABLE hotels (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200) NOT NULL,
    destination_id  UUID REFERENCES destinations(id),
    supplier_id     UUID REFERENCES suppliers(id),
    star_rating     INT,
    address         TEXT,
    phone           VARCHAR(30),
    email           VARCHAR(150),
    gstin           VARCHAR(20),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID
);
CREATE INDEX ix_hotels_destination ON hotels(destination_id);

CREATE TABLE room_types (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    hotel_id        UUID NOT NULL REFERENCES hotels(id) ON DELETE CASCADE,
    name            VARCHAR(100) NOT NULL,
    max_occupancy   INT NOT NULL DEFAULT 2,
    base_rate       NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE sightseeing (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200) NOT NULL,
    destination_id  UUID REFERENCES destinations(id),
    supplier_id     UUID REFERENCES suppliers(id),
    duration_hours  NUMERIC(4,1),
    base_cost       NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE transport_services (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200) NOT NULL,
    vehicle_type    VARCHAR(50),
    capacity        INT,
    supplier_id     UUID REFERENCES suppliers(id),
    base_rate       NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE services (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200) NOT NULL,
    category        VARCHAR(50),
    description     TEXT,
    default_rate    NUMERIC(12,2) NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE tax_rates (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name        VARCHAR(50) NOT NULL,
    percent     NUMERIC(5,2) NOT NULL,
    type        VARCHAR(20) NOT NULL DEFAULT 'GST',
    is_active   BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE currencies (
    code            CHAR(3) PRIMARY KEY,
    name            VARCHAR(50) NOT NULL,
    symbol          VARCHAR(5),
    exchange_rate   NUMERIC(12,4) NOT NULL DEFAULT 1
);
INSERT INTO currencies (code, name, symbol, exchange_rate) VALUES
    ('INR', 'Indian Rupee',     '₹', 1),
    ('USD', 'US Dollar',        '$', 83),
    ('EUR', 'Euro',             '€', 90),
    ('GBP', 'Pound Sterling',   '£', 105),
    ('AED', 'UAE Dirham',       'د.إ', 22)
ON CONFLICT (code) DO NOTHING;
