-- ============================================================================
-- Tenant DB: identity, access control, branches, audit
-- ============================================================================

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE branches (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(150) NOT NULL,
    code            VARCHAR(20)  NOT NULL UNIQUE,
    address         TEXT,
    city            VARCHAR(100),
    state           VARCHAR(100),
    country         VARCHAR(100) DEFAULT 'India',
    phone           VARCHAR(30),
    email           VARCHAR(150),
    gstin           VARCHAR(20),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    is_head_office  BOOLEAN NOT NULL DEFAULT FALSE,
    is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID,
    updated_at      TIMESTAMPTZ,
    updated_by      UUID
);

CREATE TABLE users (
    id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    email                 VARCHAR(150) NOT NULL UNIQUE,
    username              VARCHAR(50)  NOT NULL UNIQUE,
    full_name             VARCHAR(150) NOT NULL,
    password_hash         TEXT NOT NULL,
    phone                 VARCHAR(30),
    branch_id             UUID REFERENCES branches(id),
    reports_to_id         UUID REFERENCES users(id),
    is_active             BOOLEAN NOT NULL DEFAULT TRUE,
    must_change_password  BOOLEAN NOT NULL DEFAULT FALSE,
    last_login_at         TIMESTAMPTZ,
    last_login_ip         VARCHAR(45),
    failed_login_count    INT NOT NULL DEFAULT 0,
    locked_until          TIMESTAMPTZ,
    two_factor_enabled    BOOLEAN NOT NULL DEFAULT FALSE,
    two_factor_secret     TEXT,
    profile_image         VARCHAR(300),
    is_deleted            BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at            TIMESTAMPTZ,
    created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by            UUID,
    updated_at            TIMESTAMPTZ,
    updated_by            UUID
);
CREATE INDEX ix_users_branch ON users(branch_id);

CREATE TABLE roles (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name         VARCHAR(50) NOT NULL UNIQUE,
    description  TEXT,
    is_system    BOOLEAN NOT NULL DEFAULT FALSE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE permissions (
    key           VARCHAR(100) PRIMARY KEY,
    module        VARCHAR(50)  NOT NULL,
    action        VARCHAR(50)  NOT NULL,
    display_name  VARCHAR(150) NOT NULL,
    description   TEXT
);
CREATE INDEX ix_permissions_module ON permissions(module);

CREATE TABLE role_permissions (
    role_id        UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_key VARCHAR(100) NOT NULL REFERENCES permissions(key) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_key)
);

CREATE TABLE user_roles (
    user_id    UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id    UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE user_permissions_override (
    user_id        UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    permission_key VARCHAR(100) NOT NULL REFERENCES permissions(key) ON DELETE CASCADE,
    grant_type     VARCHAR(10) NOT NULL CHECK (grant_type IN ('grant','deny')),
    PRIMARY KEY (user_id, permission_key)
);

CREATE TABLE audit_log (
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
CREATE INDEX ix_audit_log_created ON audit_log(created_at DESC);
CREATE INDEX ix_audit_log_entity  ON audit_log(entity, entity_id);

CREATE TABLE login_history (
    id           BIGSERIAL PRIMARY KEY,
    user_id      UUID,
    email        VARCHAR(150),
    success      BOOLEAN NOT NULL,
    failure_reason VARCHAR(100),
    ip           VARCHAR(45),
    user_agent   TEXT,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX ix_login_history_user ON login_history(user_id, created_at DESC);

CREATE TABLE notifications (
    id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id      UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title        VARCHAR(200) NOT NULL,
    body         TEXT,
    link         VARCHAR(500),
    is_read      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX ix_notifications_user ON notifications(user_id, is_read, created_at DESC);

CREATE TABLE tenant_settings (
    key          VARCHAR(100) PRIMARY KEY,
    value        TEXT,
    updated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by   UUID
);

CREATE TABLE number_sequences (
    entity        VARCHAR(50) PRIMARY KEY,
    prefix        VARCHAR(20) NOT NULL DEFAULT '',
    current_no    BIGINT NOT NULL DEFAULT 0,
    padding       INT NOT NULL DEFAULT 4
);

INSERT INTO number_sequences (entity, prefix, current_no, padding) VALUES
    ('lead',     'LD',  0, 5),
    ('quote',    'QT',  0, 5),
    ('booking',  'BK',  0, 5),
    ('voucher',  'VC',  0, 5),
    ('invoice',  'INV', 0, 5),
    ('payment',  'PAY', 0, 5)
ON CONFLICT (entity) DO NOTHING;
