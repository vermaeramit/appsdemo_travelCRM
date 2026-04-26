-- ============================================================================
-- Master DB schema (one-time, holds tenants, plans, super-admins, billing)
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE features (
    key           VARCHAR(100) PRIMARY KEY,
    display_name  VARCHAR(150) NOT NULL,
    category      VARCHAR(50)  NOT NULL,
    description   TEXT
);

CREATE TABLE subscription_plans (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name          VARCHAR(50)  NOT NULL UNIQUE,
    description   TEXT,
    price_monthly NUMERIC(12,2) NOT NULL DEFAULT 0,
    price_yearly  NUMERIC(12,2) NOT NULL DEFAULT 0,
    max_users     INT NOT NULL DEFAULT 5,
    max_branches  INT NOT NULL DEFAULT 1,
    trial_days    INT NOT NULL DEFAULT 14,
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    sort_order    INT NOT NULL DEFAULT 0
);

CREATE TABLE plan_features (
    id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    plan_id       UUID NOT NULL REFERENCES subscription_plans(id) ON DELETE CASCADE,
    feature_key   VARCHAR(100) NOT NULL REFERENCES features(key),
    feature_value VARCHAR(100) NOT NULL DEFAULT 'true',
    UNIQUE (plan_id, feature_key)
);

CREATE TABLE tenants (
    id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code                     VARCHAR(50)  NOT NULL UNIQUE,
    company_name             VARCHAR(200) NOT NULL,
    contact_person           VARCHAR(150),
    email                    VARCHAR(150) NOT NULL,
    phone                    VARCHAR(30),
    country                  VARCHAR(100) NOT NULL DEFAULT 'India',
    timezone                 VARCHAR(50)  NOT NULL DEFAULT 'Asia/Kolkata',
    currency_code            CHAR(3)      NOT NULL DEFAULT 'INR',
    db_name                  VARCHAR(100) NOT NULL UNIQUE,
    db_host                  VARCHAR(200) NOT NULL DEFAULT 'localhost',
    db_port                  INT          NOT NULL DEFAULT 5432,
    db_user                  VARCHAR(100) NOT NULL,
    db_password_encrypted    TEXT         NOT NULL,
    plan_id                  UUID NOT NULL REFERENCES subscription_plans(id),
    status                   VARCHAR(20)  NOT NULL DEFAULT 'Trial',
    trial_ends_on            DATE,
    subscription_starts_on   DATE,
    subscription_ends_on     DATE,
    max_users                INT NOT NULL DEFAULT 5,
    logo_path                VARCHAR(300),
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by               UUID
);
CREATE INDEX ix_tenants_status ON tenants(status);

CREATE TABLE global_users (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email               VARCHAR(150) NOT NULL UNIQUE,
    full_name           VARCHAR(150) NOT NULL,
    password_hash       TEXT NOT NULL,
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at       TIMESTAMPTZ,
    failed_login_count  INT NOT NULL DEFAULT 0,
    locked_until        TIMESTAMPTZ,
    two_factor_enabled  BOOLEAN NOT NULL DEFAULT FALSE,
    two_factor_secret   TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE tenant_subscriptions_history (
    id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    plan_id           UUID NOT NULL REFERENCES subscription_plans(id),
    starts_on         DATE NOT NULL,
    ends_on           DATE,
    price_paid        NUMERIC(12,2) NOT NULL DEFAULT 0,
    billing_cycle     VARCHAR(10) NOT NULL DEFAULT 'monthly',
    notes             TEXT,
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by        UUID
);

CREATE TABLE tenant_invoices (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    invoice_no      VARCHAR(40) NOT NULL UNIQUE,
    invoice_date    DATE NOT NULL,
    amount          NUMERIC(12,2) NOT NULL,
    tax_amount      NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_amount    NUMERIC(12,2) NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'Pending',
    paid_at         TIMESTAMPTZ,
    payment_ref     VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE audit_log_global (
    id           BIGSERIAL PRIMARY KEY,
    actor_id     UUID,
    actor_email  VARCHAR(150),
    action       VARCHAR(100) NOT NULL,
    entity       VARCHAR(100),
    entity_id    VARCHAR(100),
    payload      JSONB,
    ip           VARCHAR(45),
    user_agent   TEXT,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX ix_audit_log_global_created ON audit_log_global(created_at DESC);
